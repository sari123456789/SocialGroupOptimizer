namespace MyProject.API.Placement;

public sealed class ClassificationGroupDto
{
    public string DimensionCode { get; set; } = string.Empty;

    public string LevelCode { get; set; } = string.Empty;

    public int ParticipantCount { get; set; }
}
