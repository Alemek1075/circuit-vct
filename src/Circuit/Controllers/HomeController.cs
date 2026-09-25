using System.Diagnostics;
using Circuit.Data;
using Circuit.Models;
using Circuit.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Circuit.Controllers;

public sealed class HomeController(CircuitDbContext db) : Controller
{
    public async Task<IActionResult> Index(string tournament = "paris-2025")
    {
        var tournaments = await db.Tournaments.OrderByDescending(t => t.Season).ToListAsync();
        var selected = tournaments.FirstOrDefault(t => t.Slug == tournament);
        if (selected is null) return NotFound();

        var teams = await db.Teams.Where(t => t.TournamentId == selected.Id)
            .OrderBy(t => t.Placement ?? 99).ThenBy(t => t.Name).ToListAsync();
        var matches = await db.Matches.Where(m => m.TournamentId == selected.Id)
            .Include(m => m.TeamA).Include(m => m.TeamB)
            .OrderBy(m => m.Lane).ThenBy(m => m.Round).ThenBy(m => m.Slot).ToListAsync();

        return View(new TournamentPageModel(selected, tournaments, teams, matches));
    }

    [HttpGet("match/{id:int}")]
    public async Task<IActionResult> Match(int id)
    {
        var match = await db.Matches.Include(m => m.Tournament).Include(m => m.TeamA).Include(m => m.TeamB)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (match is null) return NotFound();
        var related = await db.Matches.Where(m => m.TournamentId == match.TournamentId && m.Id != id &&
            ((match.TeamAId != null && (m.TeamAId == match.TeamAId || m.TeamBId == match.TeamAId)) ||
             (match.TeamBId != null && (m.TeamAId == match.TeamBId || m.TeamBId == match.TeamBId))))
            .Include(m => m.TeamA).Include(m => m.TeamB).OrderBy(m => m.Round).ToListAsync();
        return View(new MatchPageModel(match, related));
    }

    [HttpGet("team/{id:int}")]
    public async Task<IActionResult> Team(int id)
    {
        var team = await db.Teams.Include(t => t.Tournament).FirstOrDefaultAsync(t => t.Id == id);
        if (team is null) return NotFound();
        var matches = await db.Matches.Where(m => m.TeamAId == id || m.TeamBId == id)
            .Include(m => m.TeamA).Include(m => m.TeamB).OrderBy(m => m.Lane).ThenBy(m => m.Round).ToListAsync();
        return View(new TeamPageModel(team, matches));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });
}
