namespace MyProject.API.Placement;

public sealed class PairConstraintItemDto
{
    public int ConstraintId { get; set; }

    public string ParticipantA { get; set; } = string.Empty;

    public string ParticipantB { get; set; } = string.Empty;
}
