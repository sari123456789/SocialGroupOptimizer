namespace MyProject.API.Placement;

public sealed class AssignmentParticipantsDto
{
    public int AssignmentId { get; set; }

    public string AssignmentName { get; set; } = string.Empty;

    public IReadOnlyList<ClassificationGroupDto> ClassificationGroups { get; set; } =
        Array.Empty<ClassificationGroupDto>();

    public IReadOnlyList<ParticipantListItemDto> Participants { get; set; } =
        Array.Empty<ParticipantListItemDto>();
}
