import { BrowserRouter, Routes, Route } from "react-router-dom";

import Login from "./pages/Login";
import Live from "./pages/Live";
import Database from "./pages/Database";
import AdminLayout from "./components/AdminLayout";
import PlayerGame1 from "./pages/PlayerGame1";
import PlayerGame2 from "./pages/PlayerGame2";

function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/login" element={<Login />} />
                <Route path="/player/game1" element={<PlayerGame1 />} />
                <Route path="/player/game2" element={<PlayerGame2 />} />

                <Route element={<AdminLayout />}>
                    <Route path="/live" element={<Live />} />
                    <Route path="/database" element={<Database />} />
                </Route>
            </Routes>
        </BrowserRouter>
    );
}

export default App;