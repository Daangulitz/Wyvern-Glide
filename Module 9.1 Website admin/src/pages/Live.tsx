import { useEffect, useState } from "react";
import { API_BASE_URL } from "@/config/api";
import { Badge } from "@/components/ui/badge";
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table";

type ActivePlayer = {
    sessionId: number;
    playerId: number | null;
    gameId: number;
    name: string;
    game: {
        id: number;
        name: string;
        usesScore: boolean;
    };
};

type RankingResult = {
    rank: number;
    sessionId: number;
    playerId: number;
    name: string;
    score: number | null;
    time: number | null;
};

type RankingData = {
    gameId: number;
    gameName: string;
    leaderboardNumber: number;
    usesScore: boolean;
    results: RankingResult[];
};

type LiveData = {
    activePlayer: ActivePlayer | null;
    ranking: RankingData;
};

function formatTime(time: number | null) {
    if (time === null) {
        return "-";
    }

    const totalMilliseconds = Math.round(time * 1000);
    const minutes = Math.floor(totalMilliseconds / 60000);
    const seconds = Math.floor((totalMilliseconds % 60000) / 1000);
    const milliseconds = totalMilliseconds % 1000;

    return (
        `${String(minutes).padStart(2, "0")}:` +
        `${String(seconds).padStart(2, "0")},` +
        `${String(milliseconds).padStart(3, "0")}`
    );
}

function Live() {
    const [activePlayer, setActivePlayer] = useState<ActivePlayer | null>(null);
    const [ranking, setRanking] = useState<RankingData | null>(null);
    const [error, setError] = useState("");
    const [lastUpdate, setLastUpdate] = useState<Date | null>(null);

    useEffect(() => {
        let isMounted = true;

        async function loadData() {
            const token = sessionStorage.getItem("token");

            if (!token) {
                if (isMounted) {
                    setError("Je bent niet ingelogd. Log opnieuw in.");
                }
                return;
            }

            try {
                const response = await fetch(`${API_BASE_URL}/game/live`, {
                    headers: {
                        Authorization: `Bearer ${token}`,
                    },
                });

                const data = await response.json();

                if (!response.ok) {
                    throw new Error(
                        data.message ?? "Livegegevens konden niet worden opgehaald."
                    );
                }

                const liveData: LiveData = data;

                if (!isMounted) return;

                setActivePlayer(liveData.activePlayer);
                setRanking(liveData.ranking);
                setError("");
                setLastUpdate(new Date());
            } catch (error) {
                if (!isMounted) return;

                setError(
                    error instanceof Error
                        ? error.message
                        : "Er is iets misgegaan."
                );
            }
        }

        loadData();

        const intervalId = window.setInterval(loadData, 5000);

        return () => {
            isMounted = false;
            window.clearInterval(intervalId);
        };
    }, []);

    return (
        <div className="space-y-6">
            <div>
                <h1 className="text-3xl font-bold">Live Ranking</h1>
                <p className="text-muted-foreground">
                    Bekijk de actieve speler en de huidige ranking.
                </p>
            </div>

            <div className="space-y-3 rounded-md border p-4">
                <div className="flex items-center justify-between gap-3">
                    <h2 className="text-xl font-semibold">Actieve speler</h2>

                    <Badge variant={activePlayer ? "default" : "secondary"}>
                        {activePlayer ? "ACTIVE" : "GEEN SPELER"}
                    </Badge>
                </div>

                {activePlayer ? (
                    <div>
                        <p className="text-2xl font-bold">
                            {activePlayer.name}
                        </p>
                        {activePlayer.playerId !== null && (
                            <p className="text-sm text-muted-foreground">
                                Speler ID: {activePlayer.playerId}
                            </p>
                        )}
                        <p className="text-sm text-muted-foreground">
                            {activePlayer.game.name} · Sessie #
                            {activePlayer.sessionId}
                        </p>
                    </div>
                ) : (
                    <p className="text-muted-foreground">
                        Er is momenteel geen actieve speler.
                    </p>
                )}
            </div>

            <div className="space-y-2">
                <div>
                    <h2 className="text-xl font-semibold">
                        {ranking
                            ? `${ranking.gameName} — Leaderboard ${ranking.leaderboardNumber}`
                            : "Huidige leaderboard"}
                    </h2>
                    <p className="text-sm text-muted-foreground">
                        {ranking?.usesScore
                            ? "Gesorteerd op hoogste score, daarna op tijd."
                            : "Gesorteerd op snelste tijd."}
                    </p>
                </div>

                <div className="rounded-md border">
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead className="w-16">#</TableHead>
                                <TableHead>Speler</TableHead>
                                {ranking?.usesScore && (
                                    <TableHead className="text-right">
                                        Score
                                    </TableHead>
                                )}
                                <TableHead className="text-right">
                                    Tijd
                                </TableHead>
                            </TableRow>
                        </TableHeader>

                        <TableBody>
                            {ranking?.results.length ? (
                                ranking.results.map((player) => (
                                    <TableRow key={player.sessionId}>
                                        <TableCell className="font-medium">
                                            {player.rank}
                                        </TableCell>
                                        <TableCell>{player.name}</TableCell>
                                        {ranking.usesScore && (
                                            <TableCell className="text-right font-semibold">
                                                {player.score ?? "-"}
                                            </TableCell>
                                        )}
                                        <TableCell className="text-right">
                                            {formatTime(player.time)}
                                        </TableCell>
                                    </TableRow>
                                ))
                            ) : (
                                <TableRow>
                                    <TableCell
                                        colSpan={ranking?.usesScore ? 4 : 3}
                                        className="py-8 text-center text-muted-foreground"
                                    >
                                        {ranking
                                            ? "Er zijn nog geen resultaten."
                                            : "Ranking laden..."}
                                    </TableCell>
                                </TableRow>
                            )}
                        </TableBody>
                    </Table>
                </div>
            </div>

            {error && (
                <p className="text-sm text-red-500" role="alert">
                    {error}
                </p>
            )}

            <p className="text-sm text-muted-foreground">
                {lastUpdate
                    ? `Laatste update: ${lastUpdate.toLocaleTimeString("nl-NL")}`
                    : "Gegevens worden opgehaald..."}
                {" · "}Vernieuwen iedere 5 seconden
            </p>
        </div>
    );
}

export default Live;
