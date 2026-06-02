namespace MyProject.API.Placement;

public sealed class AssignmentSummaryDto
{
    public int AssignmentId { get; set; }

    public string AssignmentName { get; set; } = string.Empty;

    public int ParticipantCount { get; set; }
}
