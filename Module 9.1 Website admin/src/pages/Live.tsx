import { Badge } from "@/components/ui/badge";
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table";

const players = [
    {
        rank: 1,
        name: "Marijn",
        score: 1520,
        tijd: 12.122,
    },
    {
        rank: 2,
        name: "Lisa",
        score: 1370,
        tijd: 15.456,
    },
    {
        rank: 3,
        name: "Thomas",
        score: 1210,
        tijd: 18.789,
    },
    {
        rank: 4,
        name: "Kevin",
        score: 980,
        tijd: 22.345,
    },
];

function Live() {
    return (
        <div className="space-y-6">
            <div>
                <h1 className="text-3xl font-bold">Live Ranking</h1>
                <p className="text-muted-foreground">
                    Bekijk de huidige ranking van de actieve game.
                </p>
            </div>

            <div className="flex items-center gap-3">
                <h2 className="text-xl font-semibold">
                    Session #12
                </h2>

                <Badge>ACTIVE</Badge>
            </div>

            <div className="rounded-md border">
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHead className="w-20">
                                #
                            </TableHead>
                            <TableHead>
                                Speler
                            </TableHead>
                            <TableHead className="text-right">
                                Score
                            </TableHead>
                            <TableHead className="text-right">
                                Tijd
                            </TableHead>
                        </TableRow>
                    </TableHeader>

                    <TableBody>
                        {players.map((player) => (
                            <TableRow key={player.rank}>
                                <TableCell className="font-medium">
                                    {player.rank}
                                </TableCell>

                                <TableCell>
                                    {player.name}
                                </TableCell>

                                <TableCell className="text-right font-semibold">
                                    {player.score}
                                </TableCell>

                                <TableCell className="text-right">
                                    {player.tijd}
                                </TableCell>
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </div>

            <p className="text-sm text-muted-foreground">
                Laatste update: zojuist
            </p>
        </div>
    );
}

export default Live;
