using MyProject.SolverService.Contracts;

namespace MyProject.SolverService.Jobs;

public sealed class SolverJobRecord
{
    public Guid JobId { get; init; }

    public SolverJobStatus Status { get; set; }

    public bool IsTimeout { get; set; }

    public DateTime SubmittedAtUtc { get; init; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public Dictionary<int, int> UnitGroupAssignments { get; set; } = new();

    public SolverMetaWire? SolverMeta { get; set; }

    public List<string> Errors { get; set; } = new();

    public SolverJobRequest? Request { get; init; }
}

public interface ISolverJobStore
{
    void Add(SolverJobRecord job);

    bool TryGet(Guid jobId, out SolverJobRecord? job);

    void Update(SolverJobRecord job);
}

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
