import { Router } from "express";
import prisma from "./prisma.js";
import jwt from "jsonwebtoken";
import playerAuthMiddleware from "./playerAuthMiddleware.js";

const router = Router();

router.post("/access", async (request, response) => {
    try {
        const { gameKey, password } = request.body;

        if (!gameKey || !password) {
            response.status(400).json({
                message: "Game en wachtwoord zijn verplicht.",
            });
            return;
        }

        let correctPassword: string | undefined;
        let clientId: number;
        let gameId: number;

        if (gameKey === "game1") {
            correctPassword = process.env.PLAYER_GAME1_PASSWORD;
            clientId = 1;
            gameId = 1;
        } else if (gameKey === "game2") {
            correctPassword = process.env.PLAYER_GAME2_PASSWORD;
            clientId = 2;
            gameId = 2;
        } else {
            response.status(404).json({
                message: "Game niet gevonden.",
            });
            return;
        }

        if (password !== correctPassword) {
            response.status(401).json({
                message: "Ongeldig wachtwoord.",
            });
            return;
        }

        const token = jwt.sign(
            {
                type: "player",
                clientId,
                gameId,
            },
            process.env.JWT_SECRET!,
            {
                expiresIn: "8h",
            }
        );

        response.json({
            token,
        });
    } catch (error) {
        console.error(error);

        response.status(500).json({
            message: "Toegang kon niet worden gecontroleerd.",
        });
    }
});

router.post("/start-guest", playerAuthMiddleware, async (request, response) => {
    try {
        const clientId = request.playerClientId;
        const gameId = request.playerGameId;
        const { leaderboardNumber } = request.body;

        if (!gameId || !clientId || !leaderboardNumber) {
            response.status(400).json({
                message: "Game en leaderboard zijn verplicht.",
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

        const leaderboard = await prisma.leaderboard.findFirst({
            where: {
                gameId: game.id,
                number: leaderboardNumber,
            },
        });

        if (!leaderboard) {
            response.status(404).json({
                message: "Leaderboard niet gevonden.",
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
                message: "Er is al iemand aan het spelen.",
            });
            return;
        }

        const gameSession = await prisma.gameSession.create({
            data: {
                clientId,
                playerId: null,
                gameId: game.id,
                leaderboardId: leaderboard.id,
            },
        });

        response.status(201).json({
            message: "Gastsessie gestart.",
            gameSession,
            game,
            isGuest: true,
        });
    } catch (error) {
        console.error(error);

        response.status(500).json({
            message: "Gastsessie kon niet worden gestart.",
        });
    }
});

router.post("/start", playerAuthMiddleware, async (request, response) => {
    try {
        const { 
		name, 
		leaderboardNumber, 
	} = request.body;

	const clientId = request.playerClientId;
	const gameId = request.playerGameId;

        if (!name || !gameId || !clientId || !leaderboardNumber) {
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

	const leaderboard = await prisma.leaderboard.findFirst({
   		 where: {
       			 gameId: game.id,
       			 number: leaderboardNumber,
   		 },
	});

	if (!leaderboard) {
    		response.status(404).json({
        	message: "Leaderboard niet gevonden.",
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

        console.log("Actieve sessiecontrole:", {
            clientId,
            activeSession,
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
		leaderboardId: leaderboard.id,
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

router.get("/verify-access", playerAuthMiddleware, (request, response) => {
    response.status(200).json({
        valid: true,
    });
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
            playerId: activeSession.playerId ?? 0,
            name: activeSession.player?.name ?? "Gast",
            isGuest: activeSession.playerId === null,
            gameId: activeSession.gameId,
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
