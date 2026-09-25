using System.ComponentModel.DataAnnotations;

namespace Circuit.Models;

public sealed class Match
{
    public int Id { get; set; }
    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    [Required, StringLength(12)]
    public string Code { get; set; } = string.Empty;

    [Required, RegularExpression("^(upper|lower|groups|final)$")]
    public string Lane { get; set; } = string.Empty;

    [Range(1, 8)]
    public int Round { get; set; }

    [Range(1, 16)]
    public int Slot { get; set; }

    [Required, StringLength(70)]
    public string Label { get; set; } = string.Empty;

    public int? TeamAId { get; set; }
    public Team? TeamA { get; set; }
    public int? TeamBId { get; set; }
    public Team? TeamB { get; set; }

    [Range(0, 5)]
    public int? ScoreA { get; set; }

    [Range(0, 5)]
    public int? ScoreB { get; set; }

    [Range(1, 5)]
    public int BestOf { get; set; } = 3;

    public DateOnly? MatchDate { get; set; }

    [StringLength(12)]
    public string? NextWinCode { get; set; }

    [StringLength(12)]
    public string? NextLossCode { get; set; }

    public bool IsPlayed => ScoreA.HasValue && ScoreB.HasValue;
    public int? WinnerId => !IsPlayed || ScoreA == ScoreB ? null : ScoreA > ScoreB ? TeamAId : TeamBId;
}
