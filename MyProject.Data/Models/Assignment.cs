namespace MyProject.Data.Models;

/// <summary>
/// מסגרת שיבוץ או ריצה במערכת (שכבת נתונים). מזהה השיבוץ במסד הוא מספרי; מיפוי ל־<see cref="MyProject.Core.Domain.ValueObjects.AssignmentId"/> נעשה בשכבות עליונות.
/// </summary>
public class Assignment
{
    /// <summary>מפתח ראשי — מזהה שיבוץ פנימי במסד.</summary>
    public int AssignmentId { get; set; }

    public string AssignmentName { get; set; } = string.Empty;

    public int ManagementGroupId { get; set; }

    /// <summary>PendingValidation | Validated | ValidationFailed</summary>
    public string ValidationStatus { get; set; } = "PendingValidation";

    public string? LastPlacementStatus { get; set; }

    public string? LastValidationErrors { get; set; }

    public string? LastPlacementGroupsJson { get; set; }

    public double? LastPlacementScore { get; set; }

    public double? LastInitialPlacementScore { get; set; }

    public DateTime? LastValidatedAtUtc { get; set; }
}