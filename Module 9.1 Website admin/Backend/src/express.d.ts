import "express";

declare global {
    namespace Express {
        interface Request {
            playerClientId: number;
            playerGameId: number;
        }
    }
}

export {};
