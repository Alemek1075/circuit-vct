using Circuit.Data;
using Circuit.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Circuit.Controllers;

[Route("studio")]
public sealed class StudioController(CircuitDbContext db, IWebHostEnvironment environment) : Controller
{
    private bool Enabled => environment.IsDevelopment();

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        if (!Enabled) return NotFound();
        ViewBag.Notice = TempData["Notice"];
        ViewBag.Tournaments = await db.Tournaments.OrderByDescending(t => t.Season).ToListAsync();
        ViewBag.Teams = await db.Teams.Include(t => t.Tournament).OrderBy(t => t.Name).ToListAsync();
        ViewBag.Matches = await db.Matches.Include(m => m.Tournament).Include(m => m.TeamA).Include(m => m.TeamB)
            .OrderBy(m => m.TournamentId).ThenBy(m => m.Code).ToListAsync();
        return View();
    }

    [HttpGet("tournaments/new")]
    public IActionResult NewTournament()
    {
        if (!Enabled) return NotFound();
        return View("TournamentForm", new TournamentInput { Season = DateTime.UtcNow.Year, StartsOn = DateOnly.FromDateTime(DateTime.UtcNow), EndsOn = DateOnly.FromDateTime(DateTime.UtcNow), SnapshotOn = DateOnly.FromDateTime(DateTime.UtcNow) });
    }

    [HttpPost("tournaments/new"), ValidateAntiForgeryToken]
    public async Task<IActionResult> NewTournament(TournamentInput input)
    {
        if (!Enabled) return NotFound();
        if (ModelState.IsValid)
        {
            var error = await EditorRules.CheckTournamentAsync(db, input);
            if (error is not null) ModelState.AddModelError(string.Empty, error);
        }
        if (!ModelState.IsValid) return View("TournamentForm", input);
        var item = new Tournament(); EditorRules.Apply(item, input); db.Tournaments.Add(item);
        await db.SaveChangesAsync(); TempData["Notice"] = "Турнір створено."; return RedirectToAction(nameof(Index));
    }

    [HttpGet("tournaments/{id:int}")]
    public async Task<IActionResult> EditTournament(int id)
    {
        if (!Enabled) return NotFound();
        var item = await db.Tournaments.FindAsync(id);
        if (item is null) return NotFound();
        ViewBag.EditId = id; return View("TournamentForm", EditorRules.ToInput(item));
    }

    [HttpPost("tournaments/{id:int}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTournament(int id, TournamentInput input)
    {
        if (!Enabled) return NotFound();
        var item = await db.Tournaments.FindAsync(id);
        if (item is null) return NotFound();
        ViewBag.EditId = id;
        if (ModelState.IsValid)
        {
            var error = await EditorRules.CheckTournamentAsync(db, input, id);
            if (error is not null) ModelState.AddModelError(string.Empty, error);
        }
        if (!ModelState.IsValid) return View("TournamentForm", input);
        EditorRules.Apply(item, input); await db.SaveChangesAsync();
        TempData["Notice"] = "Турнір оновлено."; return RedirectToAction(nameof(Index));
    }

    [HttpPost("tournaments/{id:int}/delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTournament(int id)
    {
        if (!Enabled) return NotFound();
        var item = await db.Tournaments.FindAsync(id);
        if (item is null) return NotFound();
        if (await db.Teams.AnyAsync(t => t.TournamentId == id) || await db.Matches.AnyAsync(m => m.TournamentId == id))
        {
            TempData["Notice"] = "Спочатку видаліть команди й матчі турніру.";
            return RedirectToAction(nameof(Index));
        }
        db.Tournaments.Remove(item); await db.SaveChangesAsync();
        TempData["Notice"] = "Турнір видалено."; return RedirectToAction(nameof(Index));
    }

    [HttpGet("teams/new")]
    public async Task<IActionResult> NewTeam()
    {
        if (!Enabled) return NotFound();
        await LoadChoicesAsync(); return View("TeamForm", new TeamInput());
    }

    [HttpPost("teams/new"), ValidateAntiForgeryToken]
    public async Task<IActionResult> NewTeam(TeamInput input)
    {
        if (!Enabled) return NotFound();
        if (ModelState.IsValid)
        {
            var error = await EditorRules.CheckTeamAsync(db, input);
            if (error is not null) ModelState.AddModelError(string.Empty, error);
        }
        if (!ModelState.IsValid) { await LoadChoicesAsync(); return View("TeamForm", input); }
        var item = new Team(); EditorRules.Apply(item, input); db.Teams.Add(item);
        await db.SaveChangesAsync(); TempData["Notice"] = "Команду створено."; return RedirectToAction(nameof(Index));
    }

    [HttpGet("teams/{id:int}")]
    public async Task<IActionResult> EditTeam(int id)
    {
        if (!Enabled) return NotFound();
        var item = await db.Teams.FindAsync(id);
        if (item is null) return NotFound();
        ViewBag.EditId = id; await LoadChoicesAsync(); return View("TeamForm", EditorRules.ToInput(item));
    }

    [HttpPost("teams/{id:int}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTeam(int id, TeamInput input)
    {
        if (!Enabled) return NotFound();
        var item = await db.Teams.FindAsync(id);
        if (item is null) return NotFound();
        ViewBag.EditId = id;
        if (item.TournamentId != input.TournamentId && await db.Matches.AnyAsync(m => m.TeamAId == id || m.TeamBId == id))
            ModelState.AddModelError(nameof(input.TournamentId), "Команду з матчами не можна перенести в інший турнір.");
        if (ModelState.IsValid)
        {
            var error = await EditorRules.CheckTeamAsync(db, input, id);
            if (error is not null) ModelState.AddModelError(string.Empty, error);
        }
        if (!ModelState.IsValid) { await LoadChoicesAsync(); return View("TeamForm", input); }
        EditorRules.Apply(item, input); await db.SaveChangesAsync();
        TempData["Notice"] = "Команду оновлено."; return RedirectToAction(nameof(Index));
    }

    [HttpPost("teams/{id:int}/delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTeam(int id)
    {
        if (!Enabled) return NotFound();
        var item = await db.Teams.FindAsync(id);
        if (item is null) return NotFound();
        if (await db.Matches.AnyAsync(m => m.TeamAId == id || m.TeamBId == id))
        {
            TempData["Notice"] = "Команда має матчі. Спочатку змініть або видаліть їх.";
            return RedirectToAction(nameof(Index));
        }
        db.Teams.Remove(item); await db.SaveChangesAsync();
        TempData["Notice"] = "Команду видалено."; return RedirectToAction(nameof(Index));
    }

    [HttpGet("matches/new")]
    public async Task<IActionResult> NewMatch()
    {
        if (!Enabled) return NotFound();
        await LoadChoicesAsync(); return View("MatchForm", new MatchInput());
    }

    [HttpPost("matches/new"), ValidateAntiForgeryToken]
    public async Task<IActionResult> NewMatch(MatchInput input)
    {
        if (!Enabled) return NotFound();
        if (ModelState.IsValid)
        {
            var error = await EditorRules.CheckMatchAsync(db, input);
            if (error is not null) ModelState.AddModelError(string.Empty, error);
        }
        if (!ModelState.IsValid) { await LoadChoicesAsync(); return View("MatchForm", input); }
        var item = new Match(); EditorRules.Apply(item, input); db.Matches.Add(item);
        await db.SaveChangesAsync(); TempData["Notice"] = "Матч створено."; return RedirectToAction(nameof(Index));
    }

    [HttpGet("matches/{id:int}")]
    public async Task<IActionResult> EditMatch(int id)
    {
        if (!Enabled) return NotFound();
        var item = await db.Matches.FindAsync(id);
        if (item is null) return NotFound();
        ViewBag.EditId = id; await LoadChoicesAsync(); return View("MatchForm", EditorRules.ToInput(item));
    }

    [HttpPost("matches/{id:int}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMatch(int id, MatchInput input)
    {
        if (!Enabled) return NotFound();
        var item = await db.Matches.FindAsync(id);
        if (item is null) return NotFound();
        ViewBag.EditId = id;
        if (ModelState.IsValid)
        {
            var error = await EditorRules.CheckMatchAsync(db, input, id);
            if (error is not null) ModelState.AddModelError(string.Empty, error);
        }
        if (!ModelState.IsValid) { await LoadChoicesAsync(); return View("MatchForm", input); }
        EditorRules.Apply(item, input); await db.SaveChangesAsync();
        TempData["Notice"] = "Матч оновлено."; return RedirectToAction(nameof(Index));
    }

    [HttpPost("matches/{id:int}/delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMatch(int id)
    {
        if (!Enabled) return NotFound();
        var item = await db.Matches.FindAsync(id);
        if (item is null) return NotFound();
        db.Matches.Remove(item); await db.SaveChangesAsync();
        TempData["Notice"] = "Матч видалено."; return RedirectToAction(nameof(Index));
    }

    private async Task LoadChoicesAsync()
    {
        ViewBag.Tournaments = await db.Tournaments.OrderByDescending(t => t.Season).ToListAsync();
        ViewBag.Teams = await db.Teams.Include(t => t.Tournament).OrderBy(t => t.Name).ToListAsync();
    }
}
