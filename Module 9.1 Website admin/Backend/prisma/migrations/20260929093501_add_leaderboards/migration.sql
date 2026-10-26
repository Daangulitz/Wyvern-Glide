CREATE TABLE "Leaderboard" (
    "id" SERIAL NOT NULL,
    "number" INTEGER NOT NULL,
    "gameId" INTEGER NOT NULL,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "Leaderboard_pkey" PRIMARY KEY ("id")
);

ALTER TABLE "Leaderboard"
ADD CONSTRAINT "Leaderboard_gameId_fkey"
FOREIGN KEY ("gameId")
REFERENCES "Game"("id")
ON DELETE RESTRICT
ON UPDATE CASCADE;

INSERT INTO "Leaderboard" ("number", "gameId")
SELECT 1, "id"
FROM "Game";

ALTER TABLE "GameSession"
ADD COLUMN "leaderboardId" INTEGER;

UPDATE "GameSession"
SET "leaderboardId" = (
    SELECT "Leaderboard"."id"
    FROM "Leaderboard"
    WHERE "Leaderboard"."number" = 1
    AND "Leaderboard"."gameId" = "GameSession"."gameId"
);

ALTER TABLE "GameSession"
ALTER COLUMN "leaderboardId" SET NOT NULL;

ALTER TABLE "GameSession"
ADD CONSTRAINT "GameSession_leaderboardId_fkey"
FOREIGN KEY ("leaderboardId")
REFERENCES "Leaderboard"("id")
ON DELETE RESTRICT
