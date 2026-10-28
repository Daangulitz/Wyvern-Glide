import { useEffect, useState } from "react";
import { API_BASE_URL } from "@/config/api";

import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table";

interface Player {
    id: number;
    name: string;
}

interface GameResult {
    id: number;
    sessionId: number;
    score: number | null;
    time: number;
    createdAt: string;
}

interface Game {
    id: number;
    name: string;
    usesScore: boolean;
}

interface Leaderboard {
    id: number;
    number: number;
    gameId: number;
    createdAt: string;
}

interface GameSession {
    id: number;
    clientId: number;
    playerId: number;
    gameId: number;
    leaderboardId: number;
    startedAt: string;
    endedAt: string | null;
    status: string;
    player: Player;
    game: Game;
    result: GameResult | null;
    leaderboard: Leaderboard;
}

function Database() {
    const [gameSessions, setGameSessions] = useState<GameSession[]>([]);
    const [selectedGameId, setSelectedGameId] = useState<number | null>(null);
    const [selectedLeaderboardId, setSelectedLeaderboardId] = useState<number | null>(null);

    useEffect(() => {
        fetchDatabase();
    }, []);

    async function fetchDatabase() {
        const token = sessionStorage.getItem("token");

        const response = await fetch(`${API_BASE_URL}/database`, {
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });

        const data = await response.json();

        console.log("Database data:", data);

        setGameSessions(data);
    }

    const games = Array.from(
    new Map(
        gameSessions.map((gameSession) => [
            gameSession.game.id,
            gameSession.game,
        ])
    ).values()
    );

    const leaderboards = Array.from(
        new Map(
            gameSessions
                .filter(
                    (gameSession) =>
                        gameSession.game.id === selectedGameId
                )
                .map((gameSession) => [
                    gameSession.leaderboard.id,
                    gameSession.leaderboard,
                ])
        ).values()
    );

    const selectedGameSessions = gameSessions
        .filter(
            (gameSession) =>
                gameSession.game.id === selectedGameId &&
                gameSession.leaderboard.id === selectedLeaderboardId
        )
        .sort((a, b) => {
            if (a.game.usesScore) {
                const scoreA = a.result?.score ?? 0;
                const scoreB = b.result?.score ?? 0;

                if (scoreA !== scoreB) {
                    return scoreB - scoreA;
                }
            }

            const timeA = a.result?.time ?? Infinity;
            const timeB = b.result?.time ?? Infinity;

            return timeA - timeB;
        });
    return (
        <div>
            <h1 className="text-3xl font-bold">
                Database
            </h1>

            <div className="mt-6">
                <label className="block mb-2 font-medium">
                    Game
                </label>

                <select
                    className="rounded-md border px-3 py-2"
                    value={selectedGameId ?? ""}
                    onChange={(event) => {
                        setSelectedGameId(Number(event.target.value));
                        setSelectedLeaderboardId(null);
                    }}
                >
                    <option value="" disabled>
                        Kies een game
                    </option>

                    {games.map((game) => (
                        <option
                            key={game.id}
                            value={game.id}
                        >
                            {game.name}
                        </option>
                    ))}
                </select>
            </div>

            {selectedGameId !== null && (
                <div className="mt-6">
                    <label className="block mb-2 font-medium">
                        Ranklijst
                    </label>

                    <select
                        className="rounded-md border px-3 py-2"
                        value={selectedLeaderboardId ?? ""}
                        onChange={(event) =>
                            setSelectedLeaderboardId(
                                Number(event.target.value)
                            )
                        }
                    >
                        <option value="" disabled>
                            Kies een ranklijst
                        </option>

                        {leaderboards.map((leaderboard) => (
                            <option
                                key={leaderboard.id}
                                value={leaderboard.id}
                            >
                                Ranklijst {leaderboard.number}
                            </option>
                        ))}
                    </select>
                </div>
            )}

            {selectedLeaderboardId !== null && (
                <h2 className="mt-8 text-xl font-semibold">
                    Ranklijst{" "}
                    {
                        leaderboards.find(
                            (leaderboard) =>
                                leaderboard.id === selectedLeaderboardId
                        )?.number
                    }
                </h2>
            )}

            {selectedLeaderboardId !== null && (
                <>
                    <p className="mt-4 mb-6">
                        Aantal resultaten: {selectedGameSessions.length}
                    </p>

                    <div className="overflow-hidden rounded-lg border">
                        <Table className="w-full">
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Rank</TableHead>
                                    <TableHead>Player ID</TableHead>
                                    <TableHead>Speler</TableHead>

                                    {selectedGameSessions[0]?.game.usesScore && (
                                        <TableHead>Score</TableHead>
                                    )}

                                    <TableHead>Tijd</TableHead>
                                </TableRow>
                            </TableHeader>

                                <TableBody>
                                    {selectedGameSessions.map((gameSession, index) => (
                                        <TableRow key={gameSession.id}>
                                            <TableCell>{index + 1}</TableCell>
                                            <TableCell>{gameSession.player?.id ?? "-"}</TableCell>
                                            <TableCell>{gameSession.player?.name ?? "Gast"}</TableCell>

                                            {gameSession.game.usesScore && (
                                                <TableCell>
                                                    {gameSession.result?.score ?? "-"}
                                                </TableCell>
                                            )}

                                            <TableCell>
                                                {gameSession.result
                                                    ? gameSession.result.time
                                                        .toFixed(3)
                                                        .replace(".", ",")
                                                    : "-"}
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>

                        </Table>
                    </div>
                </>
            )}
        </div>
    );
}

export default Database;
