namespace MyProject.SolverService.Contracts;

public sealed class SolverErrorResponse
{
    public SolverJobStatus Status { get; set; }

    public bool IsTimeout { get; set; }

    public List<string> Errors { get; set; } = new();
}
