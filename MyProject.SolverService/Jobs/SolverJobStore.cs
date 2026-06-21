using MyProject.SolverService.Contracts;

namespace MyProject.SolverService.Jobs;

/// <summary>
/// רשומת עבודת פותר בזיכרון — מחזיקה קלט, סטטוס, תוצאה ושגיאות לאורך מחזור החיים.
/// </summary>
public sealed class SolverJobRecord
{
    public Guid JobId { get; init; }

    public SolverJobStatus Status { get; set; }

    /// <summary>true כשהפותר נעצר בגלל timeout (לא בהכרח Infeasible).</summary>
    public bool IsTimeout { get; set; }

    public DateTime SubmittedAtUtc { get; init; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>מיפוי unitId → groupId — מתמלא רק בהצלחה.</summary>
    public Dictionary<int, int> UnitGroupAssignments { get; set; } = new();

    public SolverMetaWire? SolverMeta { get; set; }

    public List<string> Errors { get; set; } = new();

    /// <summary>הקלט המקורי — נשמר לריצת CP-SAT.</summary>
    public SolverJobRequest? Request { get; init; }
}

/// <summary>חוזה לאחסון עבודות פותר — מאפשר החלפת מאגר (בזיכרון / עתידי: Redis).</summary>
public interface ISolverJobStore
{
    void Add(SolverJobRecord job);

    bool TryGet(Guid jobId, out SolverJobRecord? job);

    void Update(SolverJobRecord job);
}

/// <summary>
/// מאגר עבודות בזיכרון — מתאים לשירות עצמאי; נמחק עם הפעלה מחדש.
/// </summary>
public sealed class InMemorySolverJobStore : ISolverJobStore
{
    private readonly Dictionary<Guid, SolverJobRecord> _jobs = new();

    private readonly object _lock = new();

    public void Add(SolverJobRecord job)
    {
        lock (_lock)
        {
            _jobs[job.JobId] = job;
        }
    }

    public bool TryGet(Guid jobId, out SolverJobRecord? job)
    {
        lock (_lock)
        {
            if (_jobs.TryGetValue(jobId, out var found))
            {
                job = found;
                return true;
            }
        }

        job = null;
        return false;
    }

    public void Update(SolverJobRecord job)
    {
        lock (_lock)
        {
            _jobs[job.JobId] = job;
        }
    }
}
