import { Router } from "express";
import prisma from "./prisma.js";
import { GameSessionStatus } from "@prisma/client";
import authMiddleware from "./authMiddleware.js";

const router = Router();

router.post("/result", async (request, response) => {
    try {
        const { sessionId, score, time } = request.body;

        if (!sessionId || time === undefined) {
            response.status(400).json({
                message: "sessionId en time zijn verplicht.",
            });
            return;
        }

        const gameSession = await prisma.gameSession.findUnique({
            where: {
                id: sessionId,
            },
            include: {
                game: true,
                player: true,
            },
        });

        if (!gameSession) {
            response.status(404).json({
                message: "Game session niet gevonden.",
            });
            return;
        }
        
        if (gameSession.playerId === null) {
            await prisma.gameSession.update({
                where: {
                    id: sessionId,
                },
                data: {
                    status: GameSessionStatus.FINISHED,
                    endedAt: new Date(),
                },
            });

            response.status(200).json({
                message: "Gastsessie afgerond. Geen resultaat opgeslagen.",
                isGuest: true,
            });
            return;
        }

        if (!gameSession.player) {
            response.status(409).json({
                message: "Deze sessie heeft geen geldige speler.",
            });
            return;
        }


        if (gameSession.status !== GameSessionStatus.ACTIVE) {
            response.status(409).json({
                message: "Deze gamesessie is al afgerond.",
            });
            return;
        }

        const gameResult = await prisma.gameResult.create({
            data: {
                sessionId,
                score: gameSession.game.usesScore ? score : null,
                time,
            },
        });

        await prisma.gameSession.update({
            where: {
                id: sessionId,
            },
            data: {
                status: GameSessionStatus.FINISHED,
                endedAt: new Date(),
            },
        });

        response.status(201).json({
            message: "Game resultaat opgeslagen.",
            player: gameSession.player.name,
            result: gameResult,
        });
    } catch (error) {
        console.error(error);

        response.status(500).json({
            message: "Game resultaat kon niet worden opgeslagen.",
        });
    }
});

router.get("/ranking", async (request, response) => {
    try {
        const gameId = Number(request.query.gameId);
        const leaderboardNumber = Number(request.query.leaderboardNumber);

        if (
            !Number.isInteger(gameId) ||
            gameId <= 0 ||
            !Number.isInteger(leaderboardNumber) ||
            leaderboardNumber <= 0
        ) {
            response.status(400).json({
                message: "Een geldige gameId en leaderboardNumber zijn verplicht.",
            });
            return;
        }

        const leaderboard = await prisma.leaderboard.findFirst({
            where: {
                gameId,
                number: leaderboardNumber,
            },
            include: {
                game: true,
            },
        });

        if (!leaderboard) {
            response.status(404).json({
                message: "Leaderboard niet gevonden.",
            });
            return;
        }

        const sessions = await prisma.gameSession.findMany({
            where: {
                leaderboardId: leaderboard.id,
                status: GameSessionStatus.FINISHED,
                result: {
                    isNot: null,
                },
            },
            include: {
                player: true,
                result: true,
            },
        });

        sessions.sort((a, b) => {
            if (leaderboard.game.usesScore) {
                const scoreDifference =
                    (b.result?.score ?? 0) - (a.result?.score ?? 0);

                if (scoreDifference !== 0) {
                    return scoreDifference;
                }
            }

            return (a.result?.time ?? Infinity) -
                (b.result?.time ?? Infinity);
        });

        response.json({
            gameId,
            gameName: leaderboard.game.name,
            leaderboardNumber,
            usesScore: leaderboard.game.usesScore,
            results: sessions.map((session, index) => ({
                rank: index + 1,
                sessionId: session.id,
                playerId: session.player?.id ?? 0,
                name: session.player?.name ?? "Gast",
                score: session.result?.score ?? null,
                time: session.result?.time ?? null,
            })),
        });
    } catch (error) {
        console.error(error);

        response.status(500).json({
            message: "Ranking kon niet worden opgehaald.",
        });
    }
});

router.get("/live", authMiddleware, async (request, response) => {
    try {
        const clientId = request.clientId;

        if (!clientId) {
            response.status(401).json({
                message: "Client niet gevonden in het authenticatietoken.",
            });
            return;
        }

        const game = await prisma.game.findFirst({
            where: { clientId },
        });

        if (!game) {
            response.status(404).json({
                message: "Er is geen game gevonden voor deze client.",
            });
            return;
        }

        const leaderboardNumber = 1;

        const leaderboard = await prisma.leaderboard.findFirst({
            where: {
                gameId: game.id,
                number: leaderboardNumber,
            },
            include: {
                game: true,
            },
        });

        if (!leaderboard) {
            response.status(404).json({
                message: "Leaderboard niet gevonden voor deze game.",
            });
            return;
        }

        const activeSession = await prisma.gameSession.findFirst({
            where: {
                clientId,
                gameId: game.id,
                status: GameSessionStatus.ACTIVE,
            },
            include: {
                player: true,
                game: true,
            },
            orderBy: {
                id: "desc",
            },
        });

        const sessions = await prisma.gameSession.findMany({
            where: {
                clientId,
                gameId: game.id,
                leaderboardId: leaderboard.id,
                status: GameSessionStatus.FINISHED,
                result: {
                    isNot: null,
                },
                playerId: {
                    not: null,
                },
            },
            include: {
                player: true,
                result: true,
            },
        });

        sessions.sort((a, b) => {
            if (game.usesScore) {
                const scoreDifference =
                    (b.result?.score ?? 0) - (a.result?.score ?? 0);

                if (scoreDifference !== 0) {
                    return scoreDifference;
                }
            }

            return (
                (a.result?.time ?? Infinity) -
                (b.result?.time ?? Infinity)
            );
        });

        response.json({
            activePlayer: activeSession
                ? {
                      sessionId: activeSession.id,
                      playerId: activeSession.playerId,
                      gameId: activeSession.gameId,
                      name: activeSession.player?.name ?? "Gast",
                      game: {
                          id: activeSession.game.id,
                          name: activeSession.game.name,
                          usesScore: activeSession.game.usesScore,
                      },
                  }
                : null,
            ranking: {
                gameId: game.id,
                gameName: game.name,
                leaderboardNumber,
                usesScore: game.usesScore,
                results: sessions.map((session, index) => ({
                    rank: index + 1,
                    sessionId: session.id,
                    playerId: session.player?.id ?? 0,
                    name: session.player?.name ?? "Gast",
                    score: session.result?.score ?? null,
                    time: session.result?.time ?? null,
                })),
            },
        });
    } catch (error) {
        console.error(error);

        response.status(500).json({
            message: "Livegegevens konden niet worden opgehaald.",
        });
    }
});



export default router;

			
