namespace MyProject.SolverService.Contracts;

/// <summary>תשובת polling — סטטוס עבודה, שיבוץ (אם הצליח) ומטא-נתונים.</summary>
public sealed class SolverJobStatusResponse
{
    public Guid JobId { get; set; }

    public SolverJobStatus Status { get; set; }

    public bool IsTimeout { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>unitId → groupId — מתמלא רק כש-Status הוא Success.</summary>
    public Dictionary<int, int> UnitGroupAssignments { get; set; } = new();

    public SolverMetaWire? SolverMeta { get; set; }

    public List<string> Errors { get; set; } = new();
}

/// <summary>מידע טכני על ריצת הפותר — מנוע, זמן קיר וערך אובייקטיבי.</summary>
public sealed class SolverMetaWire
{
    public string Engine { get; set; } = "OR-Tools-CP-SAT";

    public long WallTimeMs { get; set; }

    public double ObjectiveValue { get; set; }
}
