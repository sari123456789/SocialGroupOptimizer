namespace MyProject.SolverService.Contracts;

/// <summary>תשובת שגיאה — קלט לא תקין או כשל לפני/במהלך פתרון.</summary>
public sealed class SolverErrorResponse
{
    public SolverJobStatus Status { get; set; }

    public bool IsTimeout { get; set; }

    public List<string> Errors { get; set; } = new();
}
