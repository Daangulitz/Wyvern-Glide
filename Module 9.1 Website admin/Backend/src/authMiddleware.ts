import { Request, Response, NextFunction } from "express";
import jwt from "jsonwebtoken";

declare global {
    namespace Express {
        interface Request {
            adminId?: number;
            clientId?: number;
        }
    }
}

function authMiddleware(
    request: Request,
    response: Response,
    next: NextFunction
) {
    const authorization = request.headers.authorization;

    if (!authorization) {
        response.status(401).json({
            message: "Geen authenticatie token.",
        });
        return;
    }

    const token = authorization.split(" ")[1];

    if (!token) {
        response.status(401).json({
            message: "Ongeldig authenticatie token.",
        });
        return;
    }

    try {
        const decoded = jwt.verify(
            token,
            process.env.JWT_SECRET!
        ) as {
            adminId: number;
            clientId: number;
        };

        request.adminId = decoded.adminId;
        request.clientId = decoded.clientId;

        next();
    } catch (error) {
        response.status(401).json({
            message: "Ongeldig of verlopen authenticatie token.",
        });
    }
}

export default authMiddleware;