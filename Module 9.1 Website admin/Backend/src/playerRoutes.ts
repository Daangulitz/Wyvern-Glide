import { Router } from "express";
import prisma from "./prisma.js";

const router = Router();

router.post("/start", async (request, response) => {
    try {
        const { name, gameId, clientId } = request.body;

        if (!name || !gameId || !clientId) {
            response.status(400).json({
                message: "Naam, gameId en clientId zijn verplicht.",
            });
            return;
        }

        const game = await prisma.game.findFirst({
            where: {
                id: gameId,
                clientId,
            },
        });

        if (!game) {
            response.status(404).json({
                message: "Game niet gevonden.",
            });
            return;
        }

        if (!game.isOnline) {
            response.status(409).json({
                message: "Deze game is momenteel offline.",
            });
            return;
        }

        const activeSession = await prisma.gameSession.findFirst({
            where: {
                clientId,
                status: "ACTIVE",
            },
        });

        if (activeSession) {
            response.status(409).json({
                message: "Er is al een speler actief. Wacht tot de huidige speler klaar is.",
            });
            return;
        }

        const player = await prisma.player.create({
            data: {
                name,
                clientId,
            },
        });

        const gameSession = await prisma.gameSession.create({
            data: {
                clientId,
                playerId: player.id,
                gameId: game.id,
            },
        });

        response.status(201).json({
            message: "Speler gestart.",
            player,
            game,
            gameSession,
        });
    } catch (error) {
        console.error(error);

        response.status(500).json({
            message: "Speler kon niet worden gestart.",
        });
    }
});

router.get("/active", async (request, response) => {
    try {
        const clientId = Number(request.query.clientId);

        if (!clientId) {
            response.status(400).json({
                message: "clientId is verplicht.",
            });
            return;
        }

        const activeSession = await prisma.gameSession.findFirst({
            where: {
                clientId,
                status: "ACTIVE",
            },
            include: {
                player: true,
                game: true,
            },
        });

        if (!activeSession) {
            response.status(404).json({
                message: "Er is momenteel geen actieve speler.",
            });
            return;
        }

        response.json({
            sessionId: activeSession.id,
            playerId: activeSession.playerId,
            gameId: activeSession.gameId,
            name: activeSession.player.name,
            game: {
                id: activeSession.game.id,
                name: activeSession.game.name,
                usesScore: activeSession.game.usesScore,
            },
        });
    } catch (error) {
        console.error(error);

        response.status(500).json({
            message: "Actieve speler kon niet worden opgehaald.",
        });
    }
});

export default router;