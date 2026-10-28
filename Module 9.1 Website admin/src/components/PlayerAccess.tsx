import { useEffect, useState } from "react";
import { API_BASE_URL } from "@/config/api";

interface PlayerAccessProps {
    gameKey: string;
    gameName: string;
    gameId: number;
    clientId: number;
    leaderboardNumber: number;
    gameImage: string;
}

interface ActiveSession {
    name?: string;
    isGuest?: boolean;
}

type ActiveStatus = "checking" | "free" | "occupied" | "error";

function PlayerAccess({
    gameKey,
    gameName,
    gameId,
    clientId,
    leaderboardNumber,
    gameImage,
}: PlayerAccessProps) {
    const [password, setPassword] = useState("");
    const [name, setName] = useState("");

    const [hasAccess, setHasAccess] = useState(false);
    const [loading, setLoading] = useState(false);
    const [message, setMessage] = useState("");

    const [activeStatus, setActiveStatus] =
        useState<ActiveStatus>("checking");
    const [activePlayerName, setActivePlayerName] = useState("");

    const isGame1 = gameKey === "game1";

    const pageStyle = isGame1
        ? {
              background: "#20262d",
              accent: "text-blue-300",
              accentBorder: "border-blue-400/30",
              button: "bg-blue-700 hover:bg-blue-600",
              focus: "focus:border-blue-400 focus:ring-blue-400/20",
          }
        : {
              background: "#292827",
              accent: "text-red-300",
              accentBorder: "border-red-400/30",
              button: "bg-red-700 hover:bg-red-600",
              focus: "focus:border-red-400 focus:ring-red-400/20",
          };

useEffect(() => {
    let cancelled = false;

    async function verifyAccess() {
        const token = localStorage.getItem(`playerToken-${gameKey}`);

        if (!token) {
            setHasAccess(false);
            return;
        }

        try {
            const response = await fetch(
                `${API_BASE_URL}/player/verify-access`,
                {
                    headers: {
                        Authorization: `Bearer ${token}`,
                    },
                }
            );

            if (cancelled) {
                return;
            }

            if (response.ok) {
                setHasAccess(true);
                return;
            }

            if (response.status === 401) {
                localStorage.removeItem(`playerToken-${gameKey}`);
                setHasAccess(false);
                setMessage("Je toegang is verlopen. Voer het wachtwoord opnieuw in.");
                return;
            }

            setHasAccess(false);
            setMessage("De toegang kon niet worden gecontroleerd. Probeer het opnieuw.");
        } catch (error) {
            console.error("Toegang controleren mislukt:", error);

            if (!cancelled) {
                setHasAccess(false);
                setMessage("Geen verbinding met de server. Controleer je verbinding en vernieuw de pagina.");
            }
        }
    }

    verifyAccess();

    return () => {
        cancelled = true;
    };
}, [gameKey]);

    useEffect(() => {
        if (!hasAccess) {
            return;
        }

        let cancelled = false;

        async function checkActiveSession() {
            try {
                const response = await fetch(
                    `${API_BASE_URL}/player/active?clientId=${clientId}`
                );

                if (cancelled) {
                    return;
                }

                if (response.status === 404) {
                    setActiveStatus("free");
                    setActivePlayerName("");
                    return;
                }

                if (!response.ok) {
                    setActiveStatus("error");
                    return;
                }

                const data: ActiveSession = await response.json();

                if (cancelled) {
                    return;
                }

                setActiveStatus("occupied");
                setActivePlayerName(data.name || "Onbekende speler");
            } catch (error) {
                console.error("Actieve sessie controleren mislukt:", error);

                if (!cancelled) {
                    setActiveStatus("error");
                }
            }
        }
        
        function handleExpiredToken() {
            localStorage.removeItem(`playerToken-${gameKey}`);
            setHasAccess(false);
            setMessage("Je toegang is verlopen. Voer het wachtwoord opnieuw in.");
        }


        checkActiveSession();

        const intervalId = window.setInterval(
            checkActiveSession,
            2000
        );

        return () => {
            cancelled = true;
            window.clearInterval(intervalId);
        };
    }, [hasAccess, clientId]);

    async function handleAccess() {
        setLoading(true);
        setMessage("");

        try {
            const response = await fetch(
                `${API_BASE_URL}/player/access`,
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify({
                        gameKey,
                        password,
                    }),
                }
            );

            const data = await response.json();
            
            if (response.status === 401) {
                handleExpiredToken();
                return;
            }


            if (!response.ok) {
                setMessage(data.message);
                return;
            }

            localStorage.setItem(
                `playerToken-${gameKey}`,
                data.token
            );

            setHasAccess(true);
        } catch (error) {
            console.error(error);

            setMessage(
                "Er kon geen verbinding worden gemaakt met de server."
            );
        } finally {
            setLoading(false);
        }
    }

    async function handleStart() {
        if (activeStatus !== "free") {
            setMessage("De game is momenteel niet beschikbaar.");
            return;
        }

        if (!name.trim()) {
            setMessage("Vul eerst je naam in.");
            return;
        }

        const token = localStorage.getItem(`playerToken-${gameKey}`);

        if (!token) {
            setMessage("Geen toegang.");
            return;
        }

        setLoading(true);
        setMessage("");

        try {
            const response = await fetch(
                `${API_BASE_URL}/player/start`,
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        Authorization: `Bearer ${token}`,
                    },
                    body: JSON.stringify({
                        name: name.trim(),
                        gameId,
                        clientId,
                        leaderboardNumber,
                    }),
                }
            );

            const data = await response.json();

            if (!response.ok) {
                setMessage(data.message);
                return;
            }

            setMessage(
                `Succes! ${data.player.name} heeft Player ID ${data.player.id}.`
            );

            setActivePlayerName(data.player.name);
            setActiveStatus("occupied");
            setName("");
        } catch (error) {
            console.error(error);

            setMessage(
                "Er kon geen verbinding worden gemaakt met de server."
            );
        } finally {
            setLoading(false);
        }
    }

    async function handleSkip() {
        if (activeStatus !== "free") {
            setMessage("De game is momenteel niet beschikbaar.");
            return;
        }

        const token = localStorage.getItem(`playerToken-${gameKey}`);

        if (!token) {
            setMessage("Geen toegang.");
            return;
        }

        setLoading(true);
        setMessage("");

        try {
            const response = await fetch(
                `${API_BASE_URL}/player/start-guest`,
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        Authorization: `Bearer ${token}`,
                    },
                    body: JSON.stringify({
                        leaderboardNumber,
                    }),
                }
            );

            const data = await response.json();

            if (!response.ok) {
                setMessage(data.message);
                return;
            }

            setMessage(
                "Gastsessie gestart! Je kunt nu gaan spelen."
            );

            setActivePlayerName("Gast");
            setActiveStatus("occupied");
            setName("");
        } catch (error) {
            console.error(error);

            setMessage(
                "Er kon geen verbinding worden gemaakt met de server."
            );
        } finally {
            setLoading(false);
        }
    }

    if (!hasAccess) {
        return (
            <div
                className="min-h-screen px-6 py-10 text-white"
                style={{ backgroundColor: pageStyle.background }}
            >
                <div className="relative mx-auto max-w-3xl pt-8">
                    <div className="overflow-hidden rounded-2xl border border-white/10 bg-[#30343a] shadow-xl">
                        <div className="aspect-video w-full bg-[#181b1f]">
                            <img
                                src={gameImage}
                                alt={`${gameName} screenshot`}
                                className="h-full w-full object-cover"
                            />
                        </div>

                        <div className="p-8 text-center sm:p-12">
                            <p
                                className={`text-sm font-bold uppercase tracking-[0.3em] ${pageStyle.accent}`}
                            >
                                Welkom bij
                            </p>

                            <h1 className="mt-3 text-5xl font-black tracking-tight sm:text-6xl">
                                {gameName}
                            </h1>

                            <p className="mx-auto mt-5 max-w-lg text-white/55">
                                Welkom bij {gameName}. Voer het
                                toegangswachtwoord in om verder te gaan.
                            </p>

                            <div className="mx-auto mt-9 max-w-md">
                                <input
                                    type="password"
                                    className={`h-14 w-full rounded-xl border-2 border-white/10 bg-[#24282d] px-5 text-center text-lg text-white outline-none transition placeholder:text-white/30 ${pageStyle.focus}`}
                                    placeholder="Toegangswachtwoord"
                                    value={password}
                                    onChange={(event) =>
                                        setPassword(event.target.value)
                                    }
                                    onKeyDown={(event) => {
                                        if (event.key === "Enter") {
                                            handleAccess();
                                        }
                                    }}
                                />

                                <button
                                    className={`mt-4 h-14 w-full rounded-xl px-6 text-lg font-bold text-white transition hover:brightness-110 disabled:opacity-50 ${pageStyle.button}`}
                                    onClick={handleAccess}
                                    disabled={loading}
                                >
                                    {loading
                                        ? "Controleren..."
                                        : "Toegang tot de game"}
                                </button>
                            </div>

                            {message && (
                                <p className="mt-5 text-sm font-medium text-red-300">
                                    {message}
                                </p>
                            )}
                        </div>
                    </div>

                    <Credits />
                </div>
            </div>
        );
    }

    return (
        <div
            className="min-h-screen px-6 py-10 text-white"
            style={{ backgroundColor: pageStyle.background }}
        >
            <div className="relative mx-auto max-w-3xl pt-8">
                <div className="overflow-hidden rounded-2xl border border-white/10 bg-[#30343a] shadow-xl">
                    <div className="aspect-video w-full bg-[#181b1f]">
                        <img
                            src={gameImage}
                            alt={`${gameName} screenshot`}
                            className="h-full w-full object-cover"
                        />
                    </div>

                    <div className="p-8 text-center sm:p-12">
                        <p
                            className={`text-sm font-bold uppercase tracking-[0.3em] ${pageStyle.accent}`}
                        >
                            Welkom bij
                        </p>

                        <h1 className="mt-3 text-5xl font-black tracking-tight sm:text-6xl">
                            {gameName}
                        </h1>

                        <p className="mt-5 text-lg text-white/55">
                            Voer je naam in of sla deze stap over om als
                            gast te spelen.
                        </p>

                        {activeStatus === "checking" && (
                            <div className="mx-auto mt-8 max-w-xl rounded-xl border border-white/10 bg-[#24282d] p-4">
                                <p className="text-white/70">
                                    Beschikbaarheid controleren...
                                </p>
                            </div>
                        )}

                        {activeStatus === "occupied" && (
                            <div className="mx-auto mt-8 max-w-xl rounded-xl border border-amber-400/30 bg-amber-400/10 p-5">
                                <p className="font-bold text-amber-200">
                                    De game is momenteel bezet
                                </p>
                                <p className="mt-2 text-white/70">
                                    {activePlayerName} is aan het spelen.
                                    Wacht totdat de sessie is afgelopen.
                                </p>
                            </div>
                        )}

                        {activeStatus === "error" && (
                            <div className="mx-auto mt-8 max-w-xl rounded-xl border border-red-400/30 bg-red-400/10 p-5">
                                <p className="font-bold text-red-200">
                                    Beschikbaarheid niet gecontroleerd
                                </p>
                                <p className="mt-2 text-white/70">
                                    De serverstatus kon niet worden
                                    gecontroleerd. Probeer het opnieuw
                                    zodra de verbinding werkt.
                                </p>
                            </div>
                        )}

                        <div className="mx-auto mt-10 max-w-xl">
                            <label
                                htmlFor="playerName"
                                className="mb-3 block text-left text-sm font-bold uppercase tracking-wider text-white/65"
                            >
                                Jouw naam
                            </label>

                            <input
                                id="playerName"
                                type="text"
                                className={`h-16 w-full rounded-xl border-2 border-white/10 bg-[#24282d] px-6 text-xl font-semibold text-white outline-none transition placeholder:text-white/30 ${pageStyle.focus}`}
                                placeholder="Vul hier je naam in..."
                                value={name}
                                onChange={(event) =>
                                    setName(event.target.value)
                                }
                                onKeyDown={(event) => {
                                    if (event.key === "Enter") {
                                        handleStart();
                                    }
                                }}
                                autoFocus
                                disabled={
                                    loading || activeStatus !== "free"
                                }
                            />

                            <button
                                className={`mt-5 h-16 w-full rounded-xl px-8 text-xl font-black uppercase tracking-wide text-white transition hover:brightness-110 active:scale-[0.99] disabled:cursor-not-allowed disabled:opacity-50 ${pageStyle.button}`}
                                onClick={handleStart}
                                disabled={
                                    loading || activeStatus !== "free"
                                }
                            >
                                {loading
                                    ? "Even geduld..."
                                    : "Start game"}
                            </button>

                            <div className="my-6 flex items-center gap-4">
                                <div className="h-px flex-1 bg-white/10" />
                                <span className="text-sm font-bold uppercase tracking-widest text-white/35">
                                    Of
                                </span>
                                <div className="h-px flex-1 bg-white/10" />
                            </div>

                            <button
                                className="h-14 w-full rounded-xl border border-white/20 bg-white/5 px-6 text-lg font-bold text-white transition hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-50"
                                onClick={handleSkip}
                                disabled={
                                    loading || activeStatus !== "free"
                                }
                            >
                                {loading
                                    ? "Even geduld..."
                                    : "Skip — speel als gast"}
                            </button>
                        </div>

                        {message && (
                            <div className="mx-auto mt-6 max-w-xl rounded-xl border border-white/10 bg-[#24282d] p-4">
                                <p className="font-medium">{message}</p>
                            </div>
                        )}
                    </div>
                </div>

                <Credits />
            </div>
        </div>
    );
}

function Credits() {
    return (
        <div className="absolute -left-5 top-32 hidden w-44 rounded-lg border border-white/10 bg-[#292d32] p-4 text-white shadow-lg sm:block">
            <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-white/40">
                Credits
            </p>

            <p className="mt-2 text-sm font-semibold">Made by</p>

            <p className="mt-1 text-sm text-white/55">
                - Marijn van Veggel
                <br />
                - Jens Monné
                <br />
                - Daan Gulitz
                <br />
                3CSD-XR1 2026
            </p>
        </div>
    );
}

export default PlayerAccess;
