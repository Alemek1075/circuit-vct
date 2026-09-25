using Circuit.Models;

namespace Circuit.ViewModels;

public sealed record TournamentPageModel(
    Tournament Tournament,
    IReadOnlyList<Tournament> Tournaments,
    IReadOnlyList<Team> Teams,
    IReadOnlyList<Match> Matches)
{
    public IReadOnlyList<Match> Upper => Matches.Where(m => m.Lane is "upper" or "final")
        .OrderBy(m => m.Round).ThenBy(m => m.Slot).ToList();
    public IReadOnlyList<Match> Lower => Matches.Where(m => m.Lane == "lower")
        .OrderBy(m => m.Round).ThenBy(m => m.Slot).ToList();
    public IReadOnlyList<Match> Groups => Matches.Where(m => m.Lane == "groups")
        .OrderBy(m => m.MatchDate).ThenBy(m => m.Slot).ToList();
    public Match? GrandFinal => Matches.FirstOrDefault(m => m.Lane == "final");
}

public sealed record MatchPageModel(Match Match, IReadOnlyList<Match> Related);
public sealed record TeamPageModel(Team Team, IReadOnlyList<Match> Matches);
