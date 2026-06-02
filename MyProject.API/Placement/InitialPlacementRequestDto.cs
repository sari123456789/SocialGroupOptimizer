namespace MyProject.API.Placement;

public sealed class InitialPlacementRequestDto
{
    public List<string> Participants { get; set; } = new();

    public int GroupCount { get; set; }

    public int MinGroupSize { get; set; }

    public int MaxGroupSize { get; set; }

    public List<ParticipantPairDto> MandatoryPairs { get; set; } = new();

    public List<ParticipantPairDto> ForbiddenPairs { get; set; } = new();
}

public sealed class ParticipantPairDto
{
    public string FirstParticipantId { get; set; } = string.Empty;

    public string SecondParticipantId { get; set; } = string.Empty;
}
