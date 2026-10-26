-- DropForeignKey
ALTER TABLE "GameSession" DROP CONSTRAINT "GameSession_playerId_fkey";

-- AlterTable
ALTER TABLE "GameSession" ALTER COLUMN "playerId" DROP NOT NULL;

-- AddForeignKey
ALTER TABLE "GameSession" ADD CONSTRAINT "GameSession_playerId_fkey" FOREIGN KEY ("playerId") REFERENCES "Player"("id") ON DELETE SET NULL ON UPDATE CASCADE;
