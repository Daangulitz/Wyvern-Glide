import PlayerAccess from "@/components/PlayerAccess";
import gameImage from "@/assets/Temp.webp";

function PlayerGame1() {
    return (
        <PlayerAccess
            gameKey="game1"
            gameName="Wyvern Glide"
            gameId={1}
            clientId={1}
            leaderboardNumber={1}
            gameImage={gameImage}
        />
    );
}

export default PlayerGame1;