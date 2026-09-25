using Circuit.Models;
using Microsoft.EntityFrameworkCore;

namespace Circuit.Data;

public static class EditorRules
{
    public static async Task<string?> CheckTeamAsync(CircuitDbContext db, TeamInput input, int? currentId = null)
    {
        if (!await db.Tournaments.AnyAsync(t => t.Id == input.TournamentId)) return "Турнір не знайдено.";
        var shortName = input.ShortName.Trim().ToUpperInvariant();
        if (await db.Teams.AnyAsync(t => t.TournamentId == input.TournamentId && t.ShortName == shortName && t.Id != currentId))
            return "Таке скорочення вже є в турнірі.";
        return null;
    }

    public static async Task<string?> CheckTournamentAsync(CircuitDbContext db, TournamentInput input, int? currentId = null)
    {
        var slug = input.Slug.Trim();
        if (await db.Tournaments.AnyAsync(t => t.Slug == slug && t.Id != currentId))
            return "Така адреса турніру вже зайнята.";
        return null;
    }

    public static async Task<string?> CheckMatchAsync(CircuitDbContext db, MatchInput input, int? currentId = null)
    {
        if (!await db.Tournaments.AnyAsync(t => t.Id == input.TournamentId)) return "Турнір не знайдено.";
        var code = input.Code.Trim().ToUpperInvariant();
        if (await db.Matches.AnyAsync(m => m.TournamentId == input.TournamentId && m.Code == code && m.Id != currentId))
            return "Такий код матчу вже є в турнірі.";
        var ids = new[] { input.TeamAId, input.TeamBId }.Where(id => id.HasValue).Select(id => id!.Value).ToArray();
        if (ids.Length != await db.Teams.CountAsync(t => t.TournamentId == input.TournamentId && ids.Contains(t.Id)))
            return "Команди повинні належати цьому турніру.";
        return null;
    }

    public static void Apply(Tournament target, TournamentInput input)
    {
        target.Slug = input.Slug.Trim(); target.Name = input.Name.Trim(); target.Season = input.Season;
        target.City = input.City.Trim(); target.StartsOn = input.StartsOn; target.EndsOn = input.EndsOn;
        target.IsComplete = input.IsComplete; target.SourceUrl = input.SourceUrl.Trim(); target.SnapshotOn = input.SnapshotOn;
    }

    public static void Apply(Team target, TeamInput input)
    {
        target.TournamentId = input.TournamentId; target.Name = input.Name.Trim(); target.ShortName = input.ShortName.Trim().ToUpperInvariant();
        target.Region = input.Region.Trim(); target.AccentColor = input.AccentColor; target.MarkUrl = string.IsNullOrWhiteSpace(input.MarkUrl) ? null : input.MarkUrl.Trim();
        target.Placement = input.Placement;
    }

    public static void Apply(Match target, MatchInput input)
    {
        target.TournamentId = input.TournamentId; target.Code = input.Code.Trim().ToUpperInvariant(); target.Lane = input.Lane;
        target.Round = input.Round; target.Slot = input.Slot; target.Label = input.Label.Trim();
        target.TeamAId = input.TeamAId; target.TeamBId = input.TeamBId; target.ScoreA = input.ScoreA; target.ScoreB = input.ScoreB;
        target.BestOf = input.BestOf; target.MatchDate = input.MatchDate;
        target.NextWinCode = input.NextWinCode; target.NextLossCode = input.NextLossCode;
    }

    public static TournamentInput ToInput(Tournament t) => new()
    {
        Slug = t.Slug, Name = t.Name, Season = t.Season, City = t.City, StartsOn = t.StartsOn, EndsOn = t.EndsOn,
        IsComplete = t.IsComplete, SourceUrl = t.SourceUrl, SnapshotOn = t.SnapshotOn
    };
    public static TeamInput ToInput(Team t) => new()
    {
        TournamentId = t.TournamentId, Name = t.Name, ShortName = t.ShortName, Region = t.Region,
        AccentColor = t.AccentColor, MarkUrl = t.MarkUrl, Placement = t.Placement
    };
    public static MatchInput ToInput(Match m) => new()
    {
        TournamentId = m.TournamentId, Code = m.Code, Lane = m.Lane, Round = m.Round, Slot = m.Slot, Label = m.Label,
        TeamAId = m.TeamAId, TeamBId = m.TeamBId, ScoreA = m.ScoreA, ScoreB = m.ScoreB, BestOf = m.BestOf,
        MatchDate = m.MatchDate, NextWinCode = m.NextWinCode, NextLossCode = m.NextLossCode
    };
}
