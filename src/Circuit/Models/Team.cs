using System.ComponentModel.DataAnnotations;

namespace Circuit.Models;

public sealed class Team
{
    public int Id { get; set; }
    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(12)]
    public string ShortName { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Region { get; set; } = string.Empty;

    [Required, RegularExpression("^#[0-9A-Fa-f]{6}$")]
    public string AccentColor { get; set; } = "#76B8BA";

    [StringLength(250)]
    public string? MarkUrl { get; set; }

    [Range(1, 16)]
    public int? Placement { get; set; }
}
