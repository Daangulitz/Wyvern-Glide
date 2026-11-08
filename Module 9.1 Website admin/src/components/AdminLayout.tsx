import { useEffect, useState } from "react";
import { Outlet, useLocation, useNavigate } from "react-router-dom";
import {
    SidebarProvider,
    Sidebar,
    SidebarHeader,
    SidebarContent,
    SidebarGroup,
    SidebarGroupContent,
    SidebarGroupLabel,
    SidebarMenu,
    SidebarMenuItem,
    SidebarMenuButton,
    SidebarTrigger,
    useSidebar,
} from "@/components/ui/sidebar";
import {
    Radio,
    Database,
    RotateCcw,
    UserX,
} from "lucide-react";
import { API_BASE_URL } from "@/config/api";

function AdminSidebarContent() {
    const { state } = useSidebar();
    const isCollapsed = state === "collapsed";

    const location = useLocation();
    const navigate = useNavigate();

    const [leaderboardName, setLeaderboardName] = useState("");
    const [nextResetAt, setNextResetAt] = useState<string | null>(null);
    const [remainingSeconds, setRemainingSeconds] = useState(0);
    const [message, setMessage] = useState("");
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);

    async function refreshResetTime() {
        const token = sessionStorage.getItem("token");
        if (!token) return;

        try {
            const response = await fetch(`${API_BASE_URL}/game/live`, {
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            });

            if (!response.ok) return;

            const data = await response.json();
            setNextResetAt(data.reset?.nextResetAt ?? null);
        } catch (error) {
            console.error("Resettijd ophalen mislukt:", error);
        }
    }

    useEffect(() => {
        refreshResetTime();

        const refreshInterval = window.setInterval(
            refreshResetTime,
            15000
        );

        return () => window.clearInterval(refreshInterval);
    }, []);

    useEffect(() => {
        function updateCountdown() {
            if (!nextResetAt) {
                setRemainingSeconds(0);
                return;
            }

            const remaining = Math.max(
                0,
                Math.ceil(
                    (new Date(nextResetAt).getTime() - Date.now()) / 1000
                )
            );

            setRemainingSeconds(remaining);
        }

        updateCountdown();

        const interval = window.setInterval(updateCountdown, 1000);

        return () => window.clearInterval(interval);
    }, [nextResetAt]);

    function formatCountdown(seconds: number) {
        const minutes = Math.floor(seconds / 60);
        const remainingSeconds = seconds % 60;

        return `${String(minutes).padStart(2, "0")}:${String(
            remainingSeconds
        ).padStart(2, "0")}`;
    }

    async function handleResetLeaderboard() {
        const name = leaderboardName.trim();

        if (!name) {
            setError("Vul eerst een naam voor de ranglijst in.");
            return;
        }

        const confirmed = window.confirm(
            `Weet je zeker dat je de huidige ranglijst wilt opslaan als "${name}" en een nieuwe ranglijst wilt starten?`
        );

        if (!confirmed) return;

        const token = sessionStorage.getItem("token");

        if (!token) {
            setError("Je bent niet ingelogd.");
            return;
        }

        setLoading(true);
        setError("");
        setMessage("");

        try {
            const response = await fetch(
                `${API_BASE_URL}/game/reset-leaderboard`,
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        Authorization: `Bearer ${token}`,
                    },
                    body: JSON.stringify({ name }),
                }
            );

            const data = await response.json();

            if (!response.ok) {
                throw new Error(data.message ?? "Ranglijst resetten mislukt.");
            }

            setLeaderboardName("");
            setNextResetAt(data.nextResetAt);
            setMessage(data.message);
        } catch (error) {
            setError(
                error instanceof Error
                    ? error.message
                    : "Er is iets misgegaan."
            );
        } finally {
            setLoading(false);
        }
    }

    async function handleCancelActivePlayer() {
        const confirmed = window.confirm(
            "Weet je zeker dat je de huidige actieve speler wilt verwijderen?"
        );

        if (!confirmed) return;

        const token = sessionStorage.getItem("token");

        if (!token) {
            setError("Je bent niet ingelogd.");
            return;
        }

        setLoading(true);
        setError("");
        setMessage("");

        try {
            const response = await fetch(
                `${API_BASE_URL}/game/cancel-active`,
                {
                    method: "POST",
                    headers: {
                        Authorization: `Bearer ${token}`,
                    },
                }
            );

            const data = await response.json();

            if (!response.ok) {
                throw new Error(data.message ?? "Speler verwijderen mislukt.");
            }

            setMessage(data.message);
        } catch (error) {
            setError(
                error instanceof Error
                    ? error.message
                    : "Er is iets misgegaan."
            );
        } finally {
            setLoading(false);
        }
    }

    return (
        <>
            <Sidebar>
                <SidebarHeader className="flex flex-row items-center justify-between p-3">
                    {!isCollapsed && (
                        <span className="font-semibold">Admin</span>
                    )}
                    <SidebarTrigger />
                </SidebarHeader>

                <SidebarContent>
                    <SidebarGroup>
                        {!isCollapsed && (
                            <SidebarGroupLabel>Navigatie</SidebarGroupLabel>
                        )}

                        <SidebarGroupContent>
                            <SidebarMenu>
                                <SidebarMenuItem>
                                    <SidebarMenuButton
                                        isActive={location.pathname === "/live"}
                                        tooltip="Live"
                                        onClick={() => navigate("/live")}
                                    >
                                        <Radio />
                                        <span>Live</span>
                                    </SidebarMenuButton>
                                </SidebarMenuItem>

                                <SidebarMenuItem>
                                    <SidebarMenuButton
                                        isActive={location.pathname === "/database"}
                                        tooltip="Database"
                                        onClick={() => navigate("/database")}
                                    >
                                        <Database />
                                        <span>Database</span>
                                    </SidebarMenuButton>
                                </SidebarMenuItem>
                            </SidebarMenu>
                        </SidebarGroupContent>
                    </SidebarGroup>

                    {!isCollapsed && (
                        <SidebarGroup>
                            <SidebarGroupLabel>
                                Ranglijstbeheer
                            </SidebarGroupLabel>

                            <SidebarGroupContent className="space-y-3 px-2">
                                <div className="space-y-2">
                                    <label
                                        htmlFor="leaderboardName"
                                        className="text-sm font-medium"
                                    >
                                        Huidige ranglijst opslaan
                                    </label>

                                    <input
                                        id="leaderboardName"
                                        value={leaderboardName}
                                        onChange={(event) =>
                                            setLeaderboardName(event.target.value)
                                        }
                                        placeholder="Naam ranglijst..."
                                        maxLength={80}
                                        className="w-full rounded-md border bg-background px-3 py-2 text-sm"
                                    />

                                    <button
                                        type="button"
                                        onClick={handleResetLeaderboard}
                                        disabled={loading || remainingSeconds > 0}
                                        className="flex w-full items-center justify-center gap-2 rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
                                    >
                                        <RotateCcw className="size-4" />
                                        {loading
                                            ? "Bezig..."
                                            : remainingSeconds > 0
                                              ? `Wacht ${formatCountdown(remainingSeconds)}`
                                              : "Opslaan en nieuwe maken"}
                                    </button>
                                </div>

                                <div className="border-t pt-3">
                                    <button
                                        type="button"
                                        onClick={handleCancelActivePlayer}
                                        disabled={loading}
                                        className="flex w-full items-center justify-center gap-2 rounded-md border border-destructive px-3 py-2 text-sm font-medium text-destructive hover:bg-destructive/10 disabled:opacity-50"
                                    >
                                        <UserX className="size-4" />
                                        Speler verwijderen
                                    </button>
                                </div>

                                {error && (
                                    <p className="text-xs text-destructive" role="alert">
                                        {error}
                                    </p>
                                )}

                                {message && (
                                    <p className="text-xs text-green-600" role="status">
                                        {message}
                                    </p>
                                )}
                            </SidebarGroupContent>
                        </SidebarGroup>
                    )}
                </SidebarContent>
            </Sidebar>

            <main className="min-w-0 flex-1 p-6">
                <Outlet />
            </main>
        </>
    );
}

function AdminLayout() {
    return (
        <SidebarProvider>
            <AdminSidebarContent />
        </SidebarProvider>
    );
}

export default AdminLayout;

