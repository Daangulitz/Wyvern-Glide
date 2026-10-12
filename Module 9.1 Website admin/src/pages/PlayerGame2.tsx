import PlayerAccess from "@/components/PlayerAccess";
import gameImage from "@/assets/Temp.webp";

function PlayerGame2() {
    return (
        <PlayerAccess
            gameKey="game2"
            gameName="Race 'n Rally"
            gameId={2}
            clientId={2}
            leaderboardNumber={1}
            gameImage={gameImage}
        />
    );
}

export default PlayerGame2;