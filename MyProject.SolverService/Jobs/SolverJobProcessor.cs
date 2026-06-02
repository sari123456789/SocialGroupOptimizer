using MyProject.SolverService.Contracts;
using MyProject.SolverService.Solving;
using MyProject.SolverService.Validation;

namespace MyProject.SolverService.Jobs;

public sealed class SolverJobProcessor
{
    private readonly ISolverJobStore _jobStore;
    private readonly CpSatPlacementSolver _solver;

    public SolverJobProcessor(ISolverJobStore jobStore, CpSatPlacementSolver solver)
    {
        _jobStore = jobStore;
        _solver = solver;
    }

    public Guid Submit(SolverJobRequest request, out List<string> validationErrors)
    {
        validationErrors = SolverJobRequestValidator.Validate(request);
        var jobId = Guid.NewGuid();
        var job = new SolverJobRecord
        {
            JobId = jobId,
            Status = validationErrors.Count > 0 ? SolverJobStatus.InvalidInput : SolverJobStatus.Pending,
            SubmittedAtUtc = DateTime.UtcNow,
            Request = request,
            Errors = validationErrors,
        };

        _jobStore.Add(job);

        if (validationErrors.Count == 0)
        {
            _ = Task.Run(() => ProcessJob(jobId));
        }

        return jobId;
    }

    private void ProcessJob(Guid jobId)
    {
        if (!_jobStore.TryGet(jobId, out var job) || job?.Request is null)
        {
            return;
        }

        job.Status = SolverJobStatus.Running;
        job.StartedAtUtc = DateTime.UtcNow;
        _jobStore.Update(job);

        try
        {
            var result = _solver.Solve(job.Request);
            job.Status = result.Status;
            job.IsTimeout = result.IsTimeout;
            job.UnitGroupAssignments = result.UnitGroupAssignments;
            job.SolverMeta = result.Meta;
            job.Errors = result.Errors.ToList();
        }
        catch (Exception ex)
        {
            job.Status = SolverJobStatus.Failed;
            job.Errors = new List<string> { $"Solver execution failed: {ex.Message}" };
        }
        finally
        {
            job.CompletedAtUtc = DateTime.UtcNow;
            _jobStore.Update(job);
        }
    }

    public bool TryCancel(Guid jobId)
    {
        if (!_jobStore.TryGet(jobId, out var job) || job is null)
        {
            return false;
        }

        if (job.Status is SolverJobStatus.Pending or SolverJobStatus.Running)
        {
            job.Status = SolverJobStatus.Failed;
            job.Errors = new List<string> { "Job was cancelled." };
            job.CompletedAtUtc = DateTime.UtcNow;
            _jobStore.Update(job);
            return true;
        }

        return false;
    }
}
