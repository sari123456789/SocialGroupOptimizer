namespace MyProject.Data.Models;

/// <summary>
/// פירוט ציוני שיבוץ (שורה אחת לשיבוץ).
/// </summary>
public class AssignmentScoreBreakdown
{
    public int AssignmentId { get; set; }

    public double SocialScore { get; set; }

    public double BalancePenalty { get; set; }

    public double IsolationPenalty { get; set; }

    public double FinalSigma { get; set; }

    public string? ScoreExplanation { get; set; }
}
