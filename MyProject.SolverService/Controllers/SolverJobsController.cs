using Microsoft.AspNetCore.Mvc;
using MyProject.SolverService.Contracts;
using MyProject.SolverService.Jobs;

namespace MyProject.SolverService.Controllers;

[ApiController]
[Route("api/v1/solver/jobs")]
public sealed class SolverJobsController : ControllerBase
{
    private readonly SolverJobProcessor _processor;
    private readonly ISolverJobStore _jobStore;

    public SolverJobsController(SolverJobProcessor processor, ISolverJobStore jobStore)
    {
        _processor = processor;
        _jobStore = jobStore;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SolverJobCreatedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(SolverErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult SubmitJob([FromBody] SolverJobRequest request)
    {
        var jobId = _processor.Submit(request, out var validationErrors);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new SolverErrorResponse
            {
                Status = SolverJobStatus.InvalidInput,
                IsTimeout = false,
                Errors = validationErrors,
            });
        }

        return Accepted(new SolverJobCreatedResponse
        {
            JobId = jobId,
            Status = SolverJobStatus.Pending,
            SubmittedAtUtc = DateTime.UtcNow,
        });
    }

    [HttpGet("{jobId:guid}")]
    [ProducesResponseType(typeof(SolverJobStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<SolverJobStatusResponse> GetJobStatus(Guid jobId)
    {
        if (!_jobStore.TryGet(jobId, out var job) || job is null)
        {
            return NotFound();
        }

        return Ok(new SolverJobStatusResponse
        {
            JobId = job.JobId,
            Status = job.Status,
            IsTimeout = job.IsTimeout,
            StartedAtUtc = job.StartedAtUtc,
            CompletedAtUtc = job.CompletedAtUtc,
            UnitGroupAssignments = new Dictionary<int, int>(job.UnitGroupAssignments),
            SolverMeta = job.SolverMeta,
            Errors = job.Errors,
        });
    }

    [HttpDelete("{jobId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult CancelJob(Guid jobId)
    {
        if (!_processor.TryCancel(jobId))
        {
            return NotFound();
        }

        return NoContent();
    }
}
