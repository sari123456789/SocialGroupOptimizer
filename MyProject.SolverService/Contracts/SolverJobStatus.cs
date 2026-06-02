namespace MyProject.SolverService.Contracts;

public enum SolverJobStatus
{
    Pending,
    Running,
    Success,
    Infeasible,
    InvalidInput,
    Timeout,
    Failed,
}
