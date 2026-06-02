namespace MyProject.API.Placement;

public sealed class AssignmentImportResponseDto
{
    public bool Success { get; set; }

    public int? AssignmentId { get; set; }

    public string? AssignmentName { get; set; }

    public int ParticipantCount { get; set; }

    public int MandatoryPairCount { get; set; }

    public int ForbiddenPairCount { get; set; }

    public int ClassificationRuleCount { get; set; }

    public List<string> Errors { get; set; } = new();

    public static AssignmentImportResponseDto FromResult(MyProject.Data.Import.ExcelImportResult result) =>
        new()
        {
            Success = result.Success,
            AssignmentId = result.AssignmentId,
            AssignmentName = result.AssignmentName,
            ParticipantCount = result.ParticipantCount,
            MandatoryPairCount = result.MandatoryPairCount,
            ForbiddenPairCount = result.ForbiddenPairCount,
            ClassificationRuleCount = result.ClassificationRuleCount,
            Errors = result.Errors.ToList(),
        };
}
