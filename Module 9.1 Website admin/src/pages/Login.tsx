import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { API_BASE_URL } from "@/config/api";

function Login() {
    const navigate = useNavigate();

    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");

    async function handleLogin() {
        setError("");

        try {
            const response = await fetch(`${API_BASE_URL}/auth/login`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    username,
                    password,
                }),
            });

            const data = await response.json();

            if (!response.ok) {
                setError(data.message);
                return;
            }

            localStorage.setItem("token", data.token);

            navigate("/live");
        } catch (error) {
            console.error(error);
            setError("Kan geen verbinding maken met de server.");
        }
    }

    return (
        <div className="flex min-h-screen items-center justify-center">
            <div className="w-full max-w-sm space-y-6">
                <div className="text-center">
                    <h1 className="text-3xl font-bold">Admin Login</h1>
                    <p className="text-muted-foreground">
                        Log in om de adminomgeving te openen.
                    </p>
                </div>

                <div className="space-y-4">
                    <div>
                        <label htmlFor="username">Gebruikersnaam</label>
                        <Input
                            id="username"
                            value={username}
                            onChange={(event) => setUsername(event.target.value)}
                            placeholder="Gebruikersnaam"
                        />
                    </div>

                    <div>
                        <label htmlFor="password">Wachtwoord</label>
                        <Input
                            id="password"
                            type="password"
                            value={password}
                            onChange={(event) => setPassword(event.target.value)}
                            placeholder="Wachtwoord"
                        />
                    </div>

                    {error && (
                        <p className="text-sm text-red-500">
                            {error}
                        </p>
                    )}

                    <Button
                        className="w-full"
                        onClick={handleLogin}
                    >
                        Inloggen
                    </Button>
                </div>
            </div>
        </div>
    );
}

export default Login;
