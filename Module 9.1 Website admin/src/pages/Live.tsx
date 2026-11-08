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
import {
    Radio,
    Trophy,
    Clock3,
    Users,
    Medal,
} from "lucide-react";

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
    leaderboardId: number;
    leaderboardNumber: number;
    leaderboardName: string;
    usesScore: boolean;
    results: RankingResult[];
};

type LiveData = {
    activePlayer: ActivePlayer | null;
    ranking: RankingData;
};

function formatTime(time: number | null) {
    if (time === null) return "-";

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
                        data.message ??
                            "Livegegevens konden niet worden opgehaald."
                    );
                }

                if (!isMounted) return;

                const liveData: LiveData = data;

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

    const topThree = ranking?.results.slice(0, 3) ?? [];

    return (
        <div className="min-h-full space-y-8">
            <div className="flex flex-wrap items-center justify-between gap-4">
                <div>
                    <div className="mb-2 flex items-center gap-2">
                        <span className="relative flex size-2">
                            <span className="absolute inline-flex size-full animate-ping rounded-full bg-emerald-400 opacity-75" />
                            <span className="relative inline-flex size-2 rounded-full bg-emerald-500" />
                        </span>
                        <span className="text-xs font-semibold uppercase tracking-[0.2em] text-emerald-500">
                            Live dashboard
                        </span>
                    </div>

                    <h1 className="text-3xl font-bold tracking-tight">
                        Live Ranking
                    </h1>

                    <p className="mt-1 text-muted-foreground">
                        {ranking
                            ? `${ranking.gameName} · ${ranking.leaderboardName} · #${ranking.leaderboardNumber}`
                            : "De huidige ranking wordt geladen."}
                    </p>
                </div>

                <Badge
                    variant="outline"
                    className="border-sky-500/40 bg-sky-500/10 px-3 py-1.5 text-sky-500"
                >
                    <Radio className="mr-2 size-3.5" />
                    LIVE
                </Badge>
            </div>

            <div className="grid gap-4 md:grid-cols-3">
                <div className="rounded-xl border bg-gradient-to-br from-sky-500/15 to-transparent p-5">
                    <div className="flex items-center justify-between">
                        <p className="text-sm font-medium text-muted-foreground">
                            Actieve speler
                        </p>
                        <Users className="size-5 text-sky-500" />
                    </div>

                    <p className="mt-3 truncate text-2xl font-bold">
                        {activePlayer?.name ?? "Geen speler"}
                    </p>

                    <p className="mt-1 text-sm text-muted-foreground">
                        {activePlayer
                            ? `Speler ID: ${activePlayer.playerId ?? "Gast"}`
                            : "Wacht op de volgende speler"}
                    </p>

                    <Badge
                        className="mt-3"
                        variant={activePlayer ? "default" : "secondary"}
                    >
                        {activePlayer ? "Aan het spelen" : "Inactief"}
                    </Badge>
                </div>

                <div className="rounded-xl border bg-gradient-to-br from-violet-500/15 to-transparent p-5">
                    <div className="flex items-center justify-between">
                        <p className="text-sm font-medium text-muted-foreground">
                            Deelnemers
                        </p>
                        <Users className="size-5 text-violet-500" />
                    </div>

                    <p className="mt-3 text-3xl font-bold tabular-nums">
                        {ranking?.results.length ?? "—"}
                    </p>

                    <p className="mt-1 text-sm text-muted-foreground">
                        Afgeronde resultaten
                    </p>
                </div>

                <div className="rounded-xl border bg-gradient-to-br from-amber-500/15 to-transparent p-5">
                    <div className="flex items-center justify-between">
                        <p className="text-sm font-medium text-muted-foreground">
                            Beste prestatie
                        </p>
                        <Trophy className="size-5 text-amber-500" />
                    </div>

                    <p className="mt-3 truncate text-2xl font-bold">
                        {ranking?.results[0]?.name ?? "Nog geen resultaat"}
                    </p>

                    <p className="mt-1 text-sm text-muted-foreground">
                        {ranking?.results[0]
                            ? ranking.usesScore
                                ? `Score: ${ranking.results[0].score ?? "-"}`
                                : `Tijd: ${formatTime(ranking.results[0].time)}`
                            : "De eerste speler is nog niet klaar"}
                    </p>
                </div>
            </div>

            {topThree.length > 0 && (
                <div className="space-y-3">
                    <div className="flex items-center gap-2">
                        <Medal className="size-5 text-amber-500" />
                        <h2 className="text-xl font-semibold">
                            Top 3 spelers
                        </h2>
                    </div>

                    <div className="grid gap-3 md:grid-cols-3">
                        {topThree.map((player) => {
                            const styles = {
                                1: "border-amber-500/50 bg-amber-500/10",
                                2: "border-slate-400/50 bg-slate-400/10",
                                3: "border-orange-700/50 bg-orange-700/10",
                            };

                            const icons = {
                                1: "text-amber-500",
                                2: "text-slate-400",
                                3: "text-orange-600",
                            };

                            return (
                                <div
                                    key={player.sessionId}
                                    className={`rounded-xl border p-4 ${
                                        styles[player.rank as 1 | 2 | 3]
                                    }`}
                                >
                                    <div className="flex items-center justify-between">
                                        <span className="text-sm font-semibold text-muted-foreground">
                                            #{player.rank}
                                        </span>
                                        <Trophy
                                            className={`size-5 ${
                                                icons[player.rank as 1 | 2 | 3]
                                            }`}
                                        />
                                    </div>

                                    <p className="mt-3 truncate text-xl font-bold">
                                        {player.name}
                                    </p>

                                    <p className="mt-1 text-sm text-muted-foreground">
                                        {ranking?.usesScore
                                            ? `Score: ${player.score ?? "-"}`
                                            : `Tijd: ${formatTime(player.time)}`}
                                    </p>
                                </div>
                            );
                        })}
                    </div>
                </div>
            )}

            <div className="space-y-3">
                <div>
                    <h2 className="text-xl font-semibold">
                        Volledige ranglijst
                    </h2>
                    <p className="text-sm text-muted-foreground">
                        {ranking?.usesScore
                            ? "Gesorteerd op hoogste score, daarna op snelste tijd."
                            : "Gesorteerd op snelste tijd."}
                    </p>
                </div>

                <div className="overflow-hidden rounded-xl border">
                    <Table>
                        <TableHeader>
                            <TableRow className="bg-muted/50 hover:bg-muted/50">
                                <TableHead className="w-20">Positie</TableHead>
                                <TableHead>Speler</TableHead>
                                {ranking?.usesScore && (
                                    <TableHead className="text-right">
                                        Score
                                    </TableHead>
                                )}
                                <TableHead className="text-right">
                                    <span className="inline-flex items-center justify-end gap-2">
                                        <Clock3 className="size-4" />
                                        Tijd
                                    </span>
                                </TableHead>
                            </TableRow>
                        </TableHeader>

                        <TableBody>
                            {ranking?.results.length ? (
                                ranking.results.map((player) => (
                                    <TableRow
                                        key={player.sessionId}
                                        className={
                                            player.rank === 1
                                                ? "bg-amber-500/5"
                                                : player.rank === 2
                                                  ? "bg-slate-400/5"
                                                  : player.rank === 3
                                                    ? "bg-orange-700/5"
                                                    : ""
                                        }
                                    >
                                        <TableCell>
                                            <span
                                                className={`inline-flex size-8 items-center justify-center rounded-lg text-sm font-bold ${
                                                    player.rank === 1
                                                        ? "bg-amber-500/20 text-amber-500"
                                                        : player.rank === 2
                                                          ? "bg-slate-400/20 text-slate-500"
                                                          : player.rank === 3
                                                            ? "bg-orange-700/20 text-orange-600"
                                                            : "bg-muted text-muted-foreground"
                                                }`}
                                            >
                                                {player.rank}
                                            </span>
                                        </TableCell>

                                        <TableCell className="font-medium">
                                            {player.name}
                                        </TableCell>

                                        {ranking.usesScore && (
                                            <TableCell className="text-right font-semibold tabular-nums">
                                                {player.score ?? "-"}
                                            </TableCell>
                                        )}

                                        <TableCell className="text-right font-mono tabular-nums">
                                            {formatTime(player.time)}
                                        </TableCell>
                                    </TableRow>
                                ))
                            ) : (
                                <TableRow>
                                    <TableCell
                                        colSpan={ranking?.usesScore ? 4 : 3}
                                        className="py-12 text-center text-muted-foreground"
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
                <p
                    className="rounded-lg border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive"
                    role="alert"
                >
                    {error}
                </p>
            )}

            <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-muted-foreground">
                <span>
                    {lastUpdate
                        ? `Laatste update: ${lastUpdate.toLocaleTimeString("nl-NL")}`
                        : "Gegevens worden opgehaald..."}
                </span>
                <span>Automatisch vernieuwen · iedere 5 seconden</span>
            </div>
        </div>
    );
}

export default Live;
