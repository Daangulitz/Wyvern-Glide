-- DropForeignKey
ALTER TABLE "GameSession" DROP CONSTRAINT "GameSession_leaderboardId_fkey";

-- AddForeignKey
ALTER TABLE "GameSession" ADD CONSTRAINT "GameSession_leaderboardId_fkey" FOREIGN KEY ("leaderboardId") REFERENCES "Leaderboard"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
