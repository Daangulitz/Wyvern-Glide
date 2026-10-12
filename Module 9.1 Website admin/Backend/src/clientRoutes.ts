import { Router } from "express";
import bcrypt from "bcrypt";
import prisma from "./prisma.js";
import authMiddleware from "./authMiddleware.js";

const router = Router();

console.log("clientRoutes geladen");

router.post("/", async (request, response) => {
    console.log("POST /clients bereikt");

    try {
        const { name, username, password } = request.body;

        if (!name || !username || !password) {
            response.status(400).json({
                message: "Naam, gebruikersnaam en wachtwoord zijn verplicht.",
            });
            return;
        }

        const passwordHash = await bcrypt.hash(password, 10);

        const client = await prisma.client.create({
            data: {
                name,
                admins: {
                    create: {
                        username,
                        passwordHash,
                    },
                },
            },
            include: {
                admins: true,
            },
        });

        response.status(201).json({
            id: client.id,
            name: client.name,
            admin: {
                id: client.admins[0].id,
                username: client.admins[0].username,
            },
        });
        } catch (error) {
            console.error("FOUT BIJ CLIENT AANMAKEN:");
            console.error(error);

            response.status(500).json({
                message: "Client kon niet worden aangemaakt.",
            });
        }
});

router.get("/me", authMiddleware, async (request, response) => {
    try {
        const client = await prisma.client.findUnique({
            where: {
                id: request.clientId,
            },
        });

        if (!client) {
            response.status(404).json({
                message: "Client niet gevonden.",
            });
            return;
        }

        response.json({
            id: client.id,
            name: client.name,
        });
    } catch (error) {
        console.error(error);

        response.status(500).json({
            message: "Client kon niet worden opgehaald.",
        });
    }
});

export default router;