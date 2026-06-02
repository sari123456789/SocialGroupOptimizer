namespace MyProject.API.Placement;

public sealed class AssignmentDetailDto
{
    public int AssignmentId { get; set; }

    public string AssignmentName { get; set; } = string.Empty;

    public string Status { get; set; } = "PendingValidation";

    public int ParticipantCount { get; set; }

    public string? LastPlacementStatus { get; set; }

    public List<string> ValidationErrors { get; set; } = new();

    public List<PlacementGroupDto> PlacementGroups { get; set; } = new();

    public AssignmentSettingsDto Settings { get; set; } = new();

    public List<ParticipantListItemDto> Participants { get; set; } = new();

    public List<PairConstraintItemDto> MandatoryPairs { get; set; } = new();

    public List<PairConstraintItemDto> ForbiddenPairs { get; set; } = new();

    public List<ClassificationConstraintItemDto> ClassificationConstraints { get; set; } = new();

    public List<string> AvailableDimensions { get; set; } = new();
}
