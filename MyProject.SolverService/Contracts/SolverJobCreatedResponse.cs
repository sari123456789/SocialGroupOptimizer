namespace MyProject.SolverService.Contracts;

/// <summary>תשובה להגשת עבודה מוצלחת — 202 Accepted עם מזהה למעקב.</summary>
public sealed class SolverJobCreatedResponse
{
    public Guid JobId { get; set; }

    public SolverJobStatus Status { get; set; }

    public DateTime SubmittedAtUtc { get; set; }
}
