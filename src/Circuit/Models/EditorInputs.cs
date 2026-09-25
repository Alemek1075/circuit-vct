using System.ComponentModel.DataAnnotations;

namespace Circuit.Models;

public sealed class TournamentInput : IValidatableObject
{
    [Required, RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$"), StringLength(60)]
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

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (EndsOn < StartsOn) yield return new ValidationResult("Дата завершення має бути після початку.", [nameof(EndsOn)]);
        if (SnapshotOn == default) yield return new ValidationResult("Вкажіть дату знімка.", [nameof(SnapshotOn)]);
        if (!Uri.TryCreate(SourceUrl, UriKind.Absolute, out var source) || source.Scheme != Uri.UriSchemeHttps)
            yield return new ValidationResult("Джерело повинно використовувати HTTPS.", [nameof(SourceUrl)]);
    }
}

public sealed class TeamInput : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int TournamentId { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required, RegularExpression("^[A-Za-z0-9]{2,12}$"), StringLength(12)]
    public string ShortName { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Region { get; set; } = string.Empty;

    [Required, RegularExpression("^#[0-9A-Fa-f]{6}$")]
    public string AccentColor { get; set; } = "#76B8BA";

    [StringLength(250)]
    public string? MarkUrl { get; set; }

    [Range(1, 16)]
    public int? Placement { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (MarkUrl is not null && !(MarkUrl.StartsWith("/img/", StringComparison.Ordinal) ||
            (Uri.TryCreate(MarkUrl, UriKind.Absolute, out var url) && url.Scheme == Uri.UriSchemeHttps)))
            yield return new ValidationResult("Посилання на зображення має починатися з /img/ або HTTPS.", [nameof(MarkUrl)]);
    }
}

public sealed class MatchInput : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int TournamentId { get; set; }

    [Required, RegularExpression("^[A-Z0-9]{1,12}$")]
    public string Code { get; set; } = string.Empty;

    [Required, RegularExpression("^(upper|lower|groups|final)$")]
    public string Lane { get; set; } = "groups";

    [Range(1, 8)]
    public int Round { get; set; } = 1;

    [Range(1, 16)]
    public int Slot { get; set; } = 1;

    [Required, StringLength(70)]
    public string Label { get; set; } = string.Empty;

    public int? TeamAId { get; set; }
    public int? TeamBId { get; set; }

    [Range(0, 5)]
    public int? ScoreA { get; set; }

    [Range(0, 5)]
    public int? ScoreB { get; set; }

    [Range(3, 5)]
    public int BestOf { get; set; } = 3;

    public DateOnly? MatchDate { get; set; }

    [RegularExpression("^[A-Z0-9]{1,12}$")]
    public string? NextWinCode { get; set; }

    [RegularExpression("^[A-Z0-9]{1,12}$")]
    public string? NextLossCode { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (BestOf is not (3 or 5)) yield return new ValidationResult("Підтримуються тільки BO3 і BO5.", [nameof(BestOf)]);
        if (TeamAId.HasValue && TeamAId == TeamBId)
            yield return new ValidationResult("Команда не може грати проти себе.", [nameof(TeamBId)]);
        if (ScoreA.HasValue != ScoreB.HasValue)
            yield return new ValidationResult("Вкажіть обидва рахунки або залиште обидва порожніми.", [nameof(ScoreA), nameof(ScoreB)]);
        if (ScoreA.HasValue && ScoreB.HasValue)
        {
            if (!TeamAId.HasValue || !TeamBId.HasValue)
                yield return new ValidationResult("Для результату потрібні обидві команди.", [nameof(TeamAId), nameof(TeamBId)]);
            if (ScoreA == ScoreB || Math.Max(ScoreA.Value, ScoreB.Value) != (BestOf / 2 + 1) ||
                Math.Min(ScoreA.Value, ScoreB.Value) >= (BestOf / 2 + 1))
                yield return new ValidationResult("Рахунок має визначати переможця формату BO3 або BO5.", [nameof(ScoreA), nameof(ScoreB)]);
        }
        if (NextWinCode == Code || NextLossCode == Code)
            yield return new ValidationResult("Матч не може переходити сам у себе.", [nameof(NextWinCode), nameof(NextLossCode)]);
    }
}
