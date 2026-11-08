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

        const game = await prisma.game.findFirst({
            where: { clientId },
        });

        if (!game) {
            response.status(404).json({ error: "GAME_NOT_FOUND" });
            return;
        }

        // Haal de nieuwste ranglijst op
        const leaderboard = await prisma.leaderboard.findFirst({
            where: { gameId: game.id },
            orderBy: [
                { number: "desc" },
                { id: "desc" },
            ],
        });

        if (!leaderboard) {
            response.status(404).json({ error: "LEADERBOARD_NOT_FOUND" });
            return;
        }

        // Haal de actieve speler op
        const activeSession = await prisma.gameSession.findFirst({
            where: {
                gameId: game.id,
                status: GameSessionStatus.ACTIVE,
            },
            include: {
                player: true,
            },
        });

        // Haal alle afgeronde resultaten van deze ranglijst op
        const sessions = await prisma.gameSession.findMany({
            where: {
                gameId: game.id,
                leaderboardId: leaderboard.id,
                status: GameSessionStatus.FINISHED,
                playerId: { not: null },
                result: { isNot: null },
            },
            include: {
                player: true,
                result: true,
            },
        });

        // Sorteer de resultaten
        sessions.sort((a, b) => {
            if (game.usesScore) {
                const scoreDifference =
                    (b.result?.score ?? 0) - (a.result?.score ?? 0);

                if (scoreDifference !== 0) {
                    return scoreDifference;
                }
            }

            return (a.result?.time ?? 0) - (b.result?.time ?? 0);
        });

        // Bereken wanneer de volgende reset mogelijk is
        const nextResetAt = game.lastLeaderboardResetAt
            ? new Date(game.lastLeaderboardResetAt.getTime() + 10 * 60 * 1000)
            : null;

        response.json({
            activePlayer: activeSession
                ? {
                    sessionId: activeSession.id,
                    playerId: activeSession.playerId,
                    gameId: activeSession.gameId,
                    name: activeSession.player?.name ?? "Gast",
                    isGuest: activeSession.playerId === null,
                    game: {
                        id: game.id,
                        name: game.name,
                        usesScore: game.usesScore,
                    },
                }
                : null,

            ranking: {
                gameId: game.id,
                gameName: game.name,
                leaderboardId: leaderboard.id,
                leaderboardNumber: leaderboard.number,
                leaderboardName: leaderboard.name,
                usesScore: game.usesScore,

                results: sessions.map((session, index) => ({
                    rank: index + 1,
                    sessionId: session.id,
                    playerId: session.playerId,
                    name: session.player?.name,
                    score: session.result?.score ?? null,
                    time: session.result?.time ?? null,
                })),
            },

            reset: {
                nextResetAt,
                canReset: !nextResetAt || nextResetAt.getTime() <= Date.now(),
            },
        });
    } catch (error) {
        console.error("Fout bij ophalen live ranking:", error);
        response.status(500).json({ error: "INTERNAL_SERVER_ERROR" });
    }
});

router.post("/reset-leaderboard", authMiddleware, async (request, response) => {
    const clientId = request.clientId;
    const name =
        typeof request.body.name === "string"
            ? request.body.name.trim()
            : "";

    if (!clientId) {
        response.status(401).json({
            message: "Je bent niet ingelogd.",
        });
        return;
    }

    if (!name || name.length > 80) {
        response.status(400).json({
            message: "Vul een naam in van maximaal 80 tekens.",
        });
        return;
    }

    const now = new Date();
    const cooldownMs = 10 * 60 * 1000;
    const cooldownThreshold = new Date(now.getTime() - cooldownMs);

    try {
        const result = await prisma.$transaction(async (transaction) => {
            const game = await transaction.game.findFirst({
                where: { clientId },
            });

            if (!game) {
                throw new Error("GAME_NOT_FOUND");
            }

            const activeSession = await transaction.gameSession.findFirst({
                where: {
                    clientId,
                    gameId: game.id,
                    status: GameSessionStatus.ACTIVE,
                },
            });

            if (activeSession) {
                throw new Error("ACTIVE_SESSION");
            }

            const currentLeaderboard = await transaction.leaderboard.findFirst({
                where: { gameId: game.id },
                orderBy: [
                    { number: "desc" },
                    { id: "desc" },
                ],
            });

            if (!currentLeaderboard) {
                throw new Error("LEADERBOARD_NOT_FOUND");
            }

            const cooldownUpdate = await transaction.game.updateMany({
                where: {
                    id: game.id,
                    clientId,
                    OR: [
                        { lastLeaderboardResetAt: null },
                        { lastLeaderboardResetAt: { lte: cooldownThreshold } },
                    ],
                },
                data: {
                    lastLeaderboardResetAt: now,
                },
            });

            if (cooldownUpdate.count === 0) {
                throw new Error("COOLDOWN");
            }

            const archivedLeaderboard = await transaction.leaderboard.update({
                where: { id: currentLeaderboard.id },
                data: { name },
            });

            const newLeaderboard = await transaction.leaderboard.create({
                data: {
                    gameId: game.id,
                    number: currentLeaderboard.number + 1,
                    name: "Ranglijst",
                },
            });

            return {
                archivedLeaderboard,
                newLeaderboard,
                nextResetAt: new Date(now.getTime() + cooldownMs),
            };
        });

        response.status(201).json({
            message: "De huidige ranglijst is opgeslagen en een nieuwe ranglijst is aangemaakt.",
            archivedLeaderboard: result.archivedLeaderboard,
            newLeaderboard: result.newLeaderboard,
            nextResetAt: result.nextResetAt,
        });
    } catch (error) {
        if (error instanceof Error) {
            if (error.message === "GAME_NOT_FOUND") {
                response.status(404).json({
                    message: "Game niet gevonden.",
                });
                return;
            }

            if (error.message === "ACTIVE_SESSION") {
                response.status(409).json({
                    message: "Er is nog een actieve speler. Rond deze eerst af of verwijder deze.",
                });
                return;
            }

            if (error.message === "LEADERBOARD_NOT_FOUND") {
                response.status(404).json({
                    message: "Er is geen huidige ranglijst gevonden.",
                });
                return;
            }

            if (error.message === "COOLDOWN") {
                response.status(429).json({
                    message: "Je kunt pas na de cooldown opnieuw een ranglijst opslaan.",
                });
                return;
            }
        }

        console.error("Fout bij resetten ranglijst:", error);

        response.status(500).json({
            message: "De ranglijst kon niet worden opgeslagen.",
        });
    }
});

router.post("/cancel-active", authMiddleware, async (request, response) => {
    const clientId = request.clientId;

    if (!clientId) {
        response.status(401).json({
            message: "Je bent niet ingelogd.",
        });
        return;
    }

    try {
        await prisma.$transaction(async (transaction) => {
            const game = await transaction.game.findFirst({
                where: { clientId },
            });

            if (!game) {
                throw new Error("GAME_NOT_FOUND");
            }

            const activeSession = await transaction.gameSession.findFirst({
                where: {
                    clientId,
                    gameId: game.id,
                    status: GameSessionStatus.ACTIVE,
                },
            });

            if (!activeSession) {
                throw new Error("NO_ACTIVE_SESSION");
            }

            await transaction.gameSession.delete({
                where: { id: activeSession.id },
            });

            if (activeSession.playerId !== null) {
                const remainingSessions = await transaction.gameSession.count({
                    where: { playerId: activeSession.playerId },
                });

                if (remainingSessions === 0) {
                    await transaction.player.delete({
                        where: { id: activeSession.playerId },
                    });
                }
            }
        });

        response.status(200).json({
            message: "De huidige speler is verwijderd.",
        });
    } catch (error) {
        if (error instanceof Error) {
            if (error.message === "GAME_NOT_FOUND") {
                response.status(404).json({
                    message: "Game niet gevonden.",
                });
                return;
            }

            if (error.message === "NO_ACTIVE_SESSION") {
                response.status(404).json({
                    message: "Er is momenteel geen actieve speler.",
                });
                return;
            }
        }

        console.error("Fout bij verwijderen actieve speler:", error);

        response.status(500).json({
            message: "De actieve speler kon niet worden verwijderd.",
        });
    }
});

export default router;
