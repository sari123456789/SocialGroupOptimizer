namespace MyProject.API.Placement;

public sealed class UpdateParticipantRequest
{
    public string? DisplayName { get; set; }

    public Dictionary<string, string>? Classifications { get; set; }
}
