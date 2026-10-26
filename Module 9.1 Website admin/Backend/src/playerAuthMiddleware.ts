import jwt from "jsonwebtoken";
import { Request, Response, NextFunction } from "express";

interface PlayerToken {
    type: string;
    clientId: number;
    gameId: number;
}

function playerAuthMiddleware(
    request: Request,
    response: Response,
    next: NextFunction
) {
    const authorization =
        request.headers.authorization;

    if (!authorization) {
        response.status(401).json({
            message: "Geen toegang.",
        });
        return;
    }

    const token = authorization.split(" ")[1];

    if (!token) {
        response.status(401).json({
            message: "Geen token.",
        });
        return;
    }

    try {
        const decoded =
            jwt.verify(
                token,
                process.env.JWT_SECRET!
            ) as PlayerToken;

        if (decoded.type !== "player") {
            response.status(403).json({
                message: "Ongeldig token.",
            });
            return;
        }

        request.playerClientId = decoded.clientId;
        request.playerGameId = decoded.gameId;

        next();
    } catch {
        response.status(401).json({
            message: "Ongeldig of verlopen token.",
        });
    }
}

export default playerAuthMiddleware;
