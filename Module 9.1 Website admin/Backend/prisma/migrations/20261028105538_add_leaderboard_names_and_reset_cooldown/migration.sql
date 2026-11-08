-- AlterTable
ALTER TABLE "Game" ADD COLUMN     "lastLeaderboardResetAt" TIMESTAMP(3);

-- AlterTable
ALTER TABLE "Leaderboard" ADD COLUMN     "name" TEXT NOT NULL DEFAULT 'Ranglijst';
