namespace MyProject.SolverService.Contracts;

public sealed class SolverJobCreatedResponse
{
    public Guid JobId { get; set; }

    public SolverJobStatus Status { get; set; }

    public DateTime SubmittedAtUtc { get; set; }
}
