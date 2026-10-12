import { Router } from "express";
import bcrypt from "bcrypt";
import jwt from "jsonwebtoken";
import prisma from "./prisma.js";

const router = Router();

router.post("/login", async (request, response) => {
    try {
        const { username, password } = request.body;

        if (!username || !password) {
            response.status(400).json({
                message: "Gebruikersnaam en wachtwoord zijn verplicht.",
            });
            return;
        }

        const admin = await prisma.admin.findUnique({
            where: {
                username,
            },
        });

        if (!admin) {
            response.status(401).json({
                message: "Ongeldige gebruikersnaam of wachtwoord.",
            });
            return;
        }

        const passwordCorrect = await bcrypt.compare(
            password,
            admin.passwordHash
        );

        if (!passwordCorrect) {
            response.status(401).json({
                message: "Ongeldige gebruikersnaam of wachtwoord.",
            });
            return;
        }
        const token = jwt.sign(
            {
                adminId: admin.id,
                clientId: admin.clientId,
            },
                process.env.JWT_SECRET!,
            {
                expiresIn: "8h",
            }
        );

        response.json({
            message: "Login succesvol.",
            token,
            admin: {
                id: admin.id,
                username: admin.username,
            },
            client: {
                id: admin.clientId,
            },
        });
    } catch (error) {
        console.error(error);

        response.status(500).json({
            message: "Inloggen mislukt.",
        });
    }
});

export default router;