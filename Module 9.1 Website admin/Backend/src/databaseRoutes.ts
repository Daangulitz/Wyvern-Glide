import { Router } from "express";
import prisma from "./prisma.js";
import authMiddleware from "./authMiddleware.js";

const router = Router();

router.get("/", authMiddleware, async (request, response) => {
    try {
        const gameSessions = await prisma.gameSession.findMany({
            where: {
                clientId: request.clientId,
            },
            include: {
                player: true,
                result: true,
		game: true,
		leaderboard: true,
            },
        });

        response.json(gameSessions);
    } catch (error) {
        console.error(error);

        response.status(500).json({
            message: "Databasegegevens konden niet worden opgehaald.",
        });
    }
});

router.post("/test", authMiddleware, async (request, response) => {
    try {
        const clientId = request.clientId;

        if (!clientId) {
            response.status(401).json({
                message: "Client niet gevonden.",
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

        const leaderboard = await prisma.leaderboard.findFirst({
            where: { gameId: game.id },
        });

        if (!leaderboard) {
            response.status(404).json({
                message: "Er is geen leaderboard gevonden voor deze game.",
            });
            return;
        }

        const player = await prisma.player.create({
            data: {
                name: "Test Player",
                clientId,
            },
        });

        const gameSession = await prisma.gameSession.create({
            data: {
                clientId,
                playerId: player.id,
                gameId: game.id,
                leaderboardId: leaderboard.id,
            },
        });

        response.status(201).json({
            message: "Test game session aangemaakt.",
            player,
            gameSession,
        });
    } catch (error) {
        console.error(error);

        response.status(500).json({
            message: "Test game session kon niet worden aangemaakt.",
        });
    }
});

export default router;
