import { Router } from "express";
import prisma from "./prisma.js";
import { GameSessionStatus } from "@prisma/client";

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
		message: "Game resultat opgeslagen",
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

export default router;
			
