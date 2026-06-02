namespace MyProject.API.Placement;

public sealed class ParticipantListItemDto
{
    public string ParticipantId { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public Dictionary<string, string> Classifications { get; set; } = new();
}
