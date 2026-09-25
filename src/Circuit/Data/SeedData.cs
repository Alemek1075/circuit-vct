using Circuit.Models;
using Microsoft.EntityFrameworkCore;

namespace Circuit.Data;

public static class SeedData
{
    public static async Task ApplyAsync(CircuitDbContext db)
    {
        await db.Database.MigrateAsync();
        if (await db.Tournaments.AnyAsync()) return;

        var paris = new Tournament
        {
            Slug = "paris-2025", Name = "Champions Paris", Season = 2025, City = "Париж",
            StartsOn = new DateOnly(2025, 9, 12), EndsOn = new DateOnly(2025, 10, 5), IsComplete = true,
            SourceUrl = "https://www.vlr.gg/event/2283/", SnapshotOn = new DateOnly(2026, 9, 24)
        };
        var shanghai = new Tournament
        {
            Slug = "shanghai-2026", Name = "Champions Shanghai", Season = 2026, City = "Шанхай",
            StartsOn = new DateOnly(2026, 9, 24), EndsOn = new DateOnly(2026, 10, 18), IsComplete = false,
            SourceUrl = "https://valorantesports.com/en-US/tournament/115576361459045501/overview", SnapshotOn = new DateOnly(2026, 9, 24)
        };
        db.Tournaments.AddRange(paris, shanghai);

        Team Add(Tournament tournament, string name, string shortName, string region, string color, int? placement = null)
        {
            var team = new Team
            {
                Tournament = tournament, Name = name, ShortName = shortName, Region = region,
                AccentColor = color, MarkUrl = $"/img/marks/{shortName.ToLowerInvariant()}.svg", Placement = placement
            };
            db.Teams.Add(team);
            return team;
        }

        var fnc = Add(paris, "FNATIC", "FNC", "EMEA", "#E8A24B", 2);
        var drx = Add(paris, "KIWOOM DRX", "DRX", "Pacific", "#86A9D4", 3);
        var prx = Add(paris, "Paper Rex", "PRX", "Pacific", "#D98F9D", 4);
        var g2 = Add(paris, "G2 Esports", "G2", "Americas", "#C8C9D0", 7);
        var th = Add(paris, "Team Heretics", "TH", "EMEA", "#E7B160", 5);
        var mibr = Add(paris, "MIBR", "MIBR", "Americas", "#B2C7D1", 5);
        var nrg = Add(paris, "NRG", "NRG", "Americas", "#93C9A0", 1);
        var gx = Add(paris, "GIANTX", "GX", "EMEA", "#D4AD8A", 7);

        void Result(string code, string lane, int round, int slot, string label, Team a, Team b,
            int scoreA, int scoreB, int bestOf = 3, string? nextWin = null, string? nextLoss = null)
        {
            db.Matches.Add(new Match
            {
                Tournament = paris, Code = code, Lane = lane, Round = round, Slot = slot, Label = label,
                TeamA = a, TeamB = b, ScoreA = scoreA, ScoreB = scoreB, BestOf = bestOf,
                NextWinCode = nextWin, NextLossCode = nextLoss
            });
        }

        Result("U1", "upper", 1, 1, "Верхня сітка · 1/4", fnc, drx, 2, 1, nextWin: "U5", nextLoss: "L1");
        Result("U2", "upper", 1, 2, "Верхня сітка · 1/4", prx, g2, 2, 1, nextWin: "U5", nextLoss: "L1");
        Result("U3", "upper", 1, 3, "Верхня сітка · 1/4", th, mibr, 0, 2, nextWin: "U6", nextLoss: "L2");
        Result("U4", "upper", 1, 4, "Верхня сітка · 1/4", nrg, gx, 2, 0, nextWin: "U6", nextLoss: "L2");
        Result("U5", "upper", 2, 1, "Верхня сітка · 1/2", fnc, prx, 2, 1, nextWin: "U7", nextLoss: "L4");
        Result("U6", "upper", 2, 2, "Верхня сітка · 1/2", mibr, nrg, 1, 2, nextWin: "U7", nextLoss: "L3");
        Result("U7", "upper", 3, 1, "Фінал верхньої сітки", fnc, nrg, 0, 2, nextWin: "GF", nextLoss: "L6");
        Result("L1", "lower", 1, 1, "Нижня сітка · раунд 1", drx, g2, 2, 1, nextWin: "L3");
        Result("L2", "lower", 1, 2, "Нижня сітка · раунд 1", th, gx, 2, 1, nextWin: "L4");
        Result("L3", "lower", 2, 1, "Нижня сітка · раунд 2", mibr, drx, 1, 2, nextWin: "L5");
        Result("L4", "lower", 2, 2, "Нижня сітка · раунд 2", prx, th, 2, 1, nextWin: "L5");
        Result("L5", "lower", 3, 1, "Нижня сітка · раунд 3", drx, prx, 2, 0, nextWin: "L6");
        Result("L6", "lower", 4, 1, "Фінал нижньої сітки", fnc, drx, 3, 1, 5, nextWin: "GF");
        Result("GF", "final", 4, 1, "Гранд-фінал", nrg, fnc, 3, 2, 5);

        var shanghaiTeams = new[]
        {
            ("NRG", "NRG", "Americas", "#93C9A0"), ("G2 Esports", "G2", "Americas", "#C8C9D0"),
            ("LOUD", "LOUD", "Americas", "#93BFA1"), ("100 Thieves", "100T", "Americas", "#D98C87"),
            ("Team Vitality", "VIT", "EMEA", "#E9B959"), ("FUT Esports", "FUT", "EMEA", "#A8BCD2"),
            ("Team Liquid", "TL", "EMEA", "#A7C1D2"), ("Karmine Corp", "KC", "EMEA", "#8CB3DA"),
            ("Paper Rex", "PRX", "Pacific", "#D98F9D"), ("NONGSHIM REDFORCE", "NS", "Pacific", "#D78C80"),
            ("GLOBAL ESPORTS", "GE", "Pacific", "#B9A9D9"), ("T1", "T1", "Pacific", "#D5A0A0"),
            ("JD GAMING", "JDG", "China", "#D9A2A6"), ("Xi Lai Gaming", "XLG", "China", "#BA9ED1"),
            ("TYLOO GAMING", "TYL", "China", "#DBA17E"), ("EDWARD Gaming", "EDG", "China", "#B5C5D0")
        };
        var roster = shanghaiTeams.ToDictionary(t => t.Item2, t => Add(shanghai, t.Item1, t.Item2, t.Item3, t.Item4));

        void Opening(string code, int slot, string teamA, string teamB, int day)
        {
            db.Matches.Add(new Match
            {
                Tournament = shanghai, Code = code, Lane = "groups", Round = 1, Slot = slot,
                Label = "Груповий етап · стартовий матч", TeamA = roster[teamA], TeamB = roster[teamB],
                MatchDate = new DateOnly(2026, 9, day), BestOf = 3
            });
        }
        Opening("G1", 1, "TL", "PRX", 24);
        Opening("G2", 2, "TYL", "G2", 24);
        Opening("G3", 3, "NS", "NRG", 25);
        Opening("G4", 4, "KC", "XLG", 25);
        Opening("G5", 5, "GE", "VIT", 26);
        Opening("G6", 6, "LOUD", "EDG", 26);
        Opening("G7", 7, "100T", "T1", 27);
        Opening("G8", 8, "JDG", "FUT", 27);

        await db.SaveChangesAsync();
    }
}
