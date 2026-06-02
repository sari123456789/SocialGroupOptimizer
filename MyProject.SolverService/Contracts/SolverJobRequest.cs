namespace MyProject.SolverService.Contracts;

public sealed class SolverJobRequest
{
    public Guid? RequestId { get; set; }

    public int TimeoutMs { get; set; } = 30_000;

    public List<SolverParticipantWire> Participants { get; set; } = new();

    public List<PlacementUnitWire> PlacementUnits { get; set; } = new();

    public List<SolverGroupWire> Groups { get; set; } = new();

    public List<ForbiddenUnitPairWire> ForbiddenUnitPairs { get; set; } = new();

    public List<ClassificationConstraintWire> ClassificationConstraints { get; set; } = new();
}

public sealed class SolverParticipantWire
{
    public string ParticipantId { get; set; } = string.Empty;

    public Dictionary<string, string> Classifications { get; set; } = new();
}

public sealed class PlacementUnitWire
{
    public int UnitId { get; set; }

    public List<string> ParticipantIds { get; set; } = new();

    public PlacementUnitKindWire Kind { get; set; }
}

public sealed class SolverGroupWire
{
    public int GroupId { get; set; }

    public int MinSize { get; set; }

    public int MaxSize { get; set; }
}

public sealed class ForbiddenUnitPairWire
{
    public int FirstUnitId { get; set; }

    public int SecondUnitId { get; set; }
}

public sealed class ClassificationConstraintWire
{
    public string ConstraintKind { get; set; } = string.Empty;

    public string TargetDimension { get; set; } = string.Empty;

    public string? TargetLevel { get; set; }

    public int? MinCountPerGroup { get; set; }

    public int? MaxCountPerGroup { get; set; }

    public List<string>? AllowedLevels { get; set; }

    public long? MaxScaledDeviation { get; set; }
}
