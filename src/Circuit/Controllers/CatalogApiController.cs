using Circuit.Data;
using Circuit.Models;
using Circuit.Realtime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Circuit.Controllers;

[ApiController]
[Route("api")]
public sealed class CatalogApiController(CircuitDbContext db, IWebHostEnvironment environment, ScorePublisher scores) : ControllerBase
{
    [HttpGet("tournaments")]
    public async Task<IActionResult> Tournaments() => Ok((await db.Tournaments.AsNoTracking()
        .OrderByDescending(t => t.Season).ToListAsync()).Select(TournamentDto));

    [HttpGet("tournaments/{id:int}")]
    public async Task<IActionResult> Tournament(int id)
    {
        var item = await db.Tournaments.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        return item is null ? NotFound() : Ok(TournamentDto(item));
    }

    [HttpPost("tournaments")]
    public async Task<IActionResult> CreateTournament(TournamentInput input)
    {
        if (!environment.IsDevelopment()) return WriteDisabled();
        var error = await EditorRules.CheckTournamentAsync(db, input);
        if (error is not null) return BadRequest(new { error });
        var item = new Tournament(); EditorRules.Apply(item, input);
        db.Tournaments.Add(item); await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Tournament), new { id = item.Id }, TournamentDto(item));
    }

    [HttpPut("tournaments/{id:int}")]
    public async Task<IActionResult> UpdateTournament(int id, TournamentInput input)
    {
        if (!environment.IsDevelopment()) return WriteDisabled();
        var item = await db.Tournaments.FindAsync(id);
        if (item is null) return NotFound();
        var error = await EditorRules.CheckTournamentAsync(db, input, id);
        if (error is not null) return BadRequest(new { error });
        EditorRules.Apply(item, input); await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("tournaments/{id:int}")]
    public async Task<IActionResult> DeleteTournament(int id)
    {
        if (!environment.IsDevelopment()) return WriteDisabled();
        var item = await db.Tournaments.FindAsync(id);
        if (item is null) return NotFound();
        if (await db.Teams.AnyAsync(t => t.TournamentId == id) || await db.Matches.AnyAsync(m => m.TournamentId == id))
            return Conflict(new { error = "Спочатку видаліть команди й матчі турніру." });
        db.Tournaments.Remove(item); await db.SaveChangesAsync(); return NoContent();
    }

    [HttpGet("teams")]
    public async Task<IActionResult> Teams(int? tournamentId)
    {
        var query = db.Teams.AsNoTracking().AsQueryable();
        if (tournamentId.HasValue) query = query.Where(t => t.TournamentId == tournamentId.Value);
        return Ok((await query.OrderBy(t => t.Name).ToListAsync()).Select(TeamDto));
    }

    [HttpGet("teams/{id:int}")]
    public async Task<IActionResult> Team(int id)
    {
        var item = await db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        return item is null ? NotFound() : Ok(TeamDto(item));
    }

    [HttpGet("teams/search")]
    public async Task<IActionResult> SearchTeams(string q, int tournamentId)
    {
        var term = q?.Trim() ?? string.Empty;
        if (term.Length < 3 || tournamentId < 1) return Ok(Array.Empty<object>());
        var results = await db.Teams.AsNoTracking()
            .Where(t => t.TournamentId == tournamentId &&
                (t.Name.ToLower().Contains(term.ToLower()) || t.ShortName.ToLower().Contains(term.ToLower())))
            .OrderBy(t => t.Name).Take(12)
            .Select(t => new { t.Id, t.Name, t.ShortName }).ToListAsync();
        return Ok(results);
    }

    [HttpPost("teams")]
    public async Task<IActionResult> CreateTeam(TeamInput input)
    {
        if (!environment.IsDevelopment()) return WriteDisabled();
        var error = await EditorRules.CheckTeamAsync(db, input);
        if (error is not null) return BadRequest(new { error });
        var item = new Team(); EditorRules.Apply(item, input);
        db.Teams.Add(item); await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Team), new { id = item.Id }, TeamDto(item));
    }

    [HttpPut("teams/{id:int}")]
    public async Task<IActionResult> UpdateTeam(int id, TeamInput input)
    {
        if (!environment.IsDevelopment()) return WriteDisabled();
        var item = await db.Teams.FindAsync(id);
        if (item is null) return NotFound();
        if (item.TournamentId != input.TournamentId && await db.Matches.AnyAsync(m => m.TeamAId == id || m.TeamBId == id))
            return Conflict(new { error = "Команду з матчами не можна перенести в інший турнір." });
        var error = await EditorRules.CheckTeamAsync(db, input, id);
        if (error is not null) return BadRequest(new { error });
        EditorRules.Apply(item, input); await db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("teams/{id:int}")]
    public async Task<IActionResult> DeleteTeam(int id)
    {
        if (!environment.IsDevelopment()) return WriteDisabled();
        var item = await db.Teams.FindAsync(id);
        if (item is null) return NotFound();
        if (await db.Matches.AnyAsync(m => m.TeamAId == id || m.TeamBId == id))
            return Conflict(new { error = "Команда має матчі. Спочатку змініть або видаліть їх." });
        db.Teams.Remove(item); await db.SaveChangesAsync(); return NoContent();
    }

    [HttpGet("matches")]
    public async Task<IActionResult> Matches(int? tournamentId, int skip = 0, int limit = 20)
    {
        if (skip < 0 || limit is < 1 or > 100) return BadRequest(new { error = "skip ≥ 0, limit від 1 до 100." });
        var query = db.Matches.AsNoTracking().AsQueryable();
        if (tournamentId.HasValue) query = query.Where(m => m.TournamentId == tournamentId.Value);
        var total = await query.CountAsync();
        var items = await query.OrderBy(m => m.TournamentId).ThenBy(m => m.Id).Skip(skip).Take(limit).ToListAsync();
        var nextLink = skip + items.Count < total
            ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/matches?skip={skip + items.Count}&limit={limit}" +
              (tournamentId.HasValue ? $"&tournamentId={tournamentId.Value}" : string.Empty)
            : null;
        return Ok(new { items = items.Select(MatchDto), total, skip, limit, nextLink });
    }

    [HttpGet("matches/{id:int}")]
    public async Task<IActionResult> Match(int id)
    {
        var item = await db.Matches.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        return item is null ? NotFound() : Ok(MatchDto(item));
    }

    [HttpPost("matches")]
    public async Task<IActionResult> CreateMatch(MatchInput input)
    {
        if (!environment.IsDevelopment()) return WriteDisabled();
        var error = await EditorRules.CheckMatchAsync(db, input);
        if (error is not null) return BadRequest(new { error });
        var item = new Match(); EditorRules.Apply(item, input);
        db.Matches.Add(item); await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Match), new { id = item.Id }, MatchDto(item));
    }

    [HttpPut("matches/{id:int}")]
    public async Task<IActionResult> UpdateMatch(int id, MatchInput input)
    {
        if (!environment.IsDevelopment()) return WriteDisabled();
        var item = await db.Matches.FindAsync(id);
        if (item is null) return NotFound();
        var error = await EditorRules.CheckMatchAsync(db, input, id);
        if (error is not null) return BadRequest(new { error });
        var previousTournamentId = item.TournamentId;
        EditorRules.Apply(item, input); await db.SaveChangesAsync();
        await scores.ChangedAsync(item.TournamentId, item.Id);
        if (previousTournamentId != item.TournamentId) await scores.ChangedAsync(previousTournamentId, item.Id);
        return NoContent();
    }

    [HttpDelete("matches/{id:int}")]
    public async Task<IActionResult> DeleteMatch(int id)
    {
        if (!environment.IsDevelopment()) return WriteDisabled();
        var item = await db.Matches.FindAsync(id);
        if (item is null) return NotFound();
        db.Matches.Remove(item); await db.SaveChangesAsync(); return NoContent();
    }

    private ObjectResult WriteDisabled() => StatusCode(StatusCodes.Status403Forbidden,
        new { error = "Запис доступний лише в локальному режимі розробки." });

    private static object TournamentDto(Tournament t) => new
    {
        t.Id, t.Slug, t.Name, t.Season, t.City, t.StartsOn, t.EndsOn, t.IsComplete, t.SourceUrl, t.SnapshotOn
    };
    private static object TeamDto(Team t) => new
    {
        t.Id, t.TournamentId, t.Name, t.ShortName, t.Region, t.AccentColor, t.MarkUrl, t.Placement
    };
    private static object MatchDto(Match m) => new
    {
        m.Id, m.TournamentId, m.Code, m.Lane, m.Round, m.Slot, m.Label,
        m.TeamAId, m.TeamBId, m.ScoreA, m.ScoreB, m.BestOf, m.MatchDate, m.NextWinCode, m.NextLossCode
    };
}
