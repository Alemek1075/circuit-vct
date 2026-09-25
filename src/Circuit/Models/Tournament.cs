using System.ComponentModel.DataAnnotations;

namespace Circuit.Models;

public sealed class Tournament
{
    public int Id { get; set; }

    [Required, StringLength(60)]
    public string Slug { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Range(2020, 2100)]
    public int Season { get; set; }

    [Required, StringLength(80)]
    public string City { get; set; } = string.Empty;

    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public bool IsComplete { get; set; }

    [Required, Url, StringLength(500)]
    public string SourceUrl { get; set; } = string.Empty;

    public DateOnly SnapshotOn { get; set; }
    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
