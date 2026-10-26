import express from "express";
import cors from "cors";
import prisma from "./prisma.js";
import clientRoutes from "./clientRoutes.js";
import authRoutes from "./authRoutes.js";
import authMiddleware from "./authMiddleware.js";
import databaseRoutes from "./databaseRoutes.js";
import playerRoutes from "./playerRoutes.js";
import gameRoutes from "./gameRoutes.js";

const app = express();
const port = 3000;
app.use(cors());
app.use(express.json());

app.use(express.json());
app.use("/clients", clientRoutes);
app.use("/auth", authRoutes);
app.use("/database", databaseRoutes);
app.use("/player", playerRoutes);
app.use("/game", gameRoutes);

app.get("/", (_request, response) => {
    response.json({
        message: "Module 9.1 API werkt!",
    });
});

app.get("/test/database", async (_request, response) => {
    const players = await prisma.player.findMany();

    response.json(players);
});

app.get("/test/secure", authMiddleware, (request, response) => {
    response.json({
        message: "Beveiligde route werkt!",
        adminId: request.adminId,
        clientId: request.clientId,
    });
});

app.use("/clients", clientRoutes);
app.use("/auth", authRoutes);

app.listen(port, "0.0.0.0",  () => {
    console.log(`API gestart op poort ${port}`);
});
