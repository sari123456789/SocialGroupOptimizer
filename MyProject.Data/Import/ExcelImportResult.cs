namespace MyProject.Data.Import;

public sealed class ExcelImportResult
{
    public bool Success => Errors.Count == 0 && AssignmentId.HasValue;

    public int? AssignmentId { get; init; }

    public string? AssignmentName { get; init; }

    public int ParticipantCount { get; init; }

    public int MandatoryPairCount { get; init; }

    public int ForbiddenPairCount { get; init; }

    public int ClassificationRuleCount { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
