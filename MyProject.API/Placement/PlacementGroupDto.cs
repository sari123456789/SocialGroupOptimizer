namespace MyProject.API.Placement;

public sealed class PlacementGroupDto
{
    public int GroupId { get; set; }

    public List<string> ParticipantIds { get; set; } = new();
}
