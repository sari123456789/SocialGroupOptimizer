namespace MyProject.Data.Models;

/// <summary>
/// רשומת קונפליקט או בעיה הקשורה לשיבוץ.
/// </summary>
public class AssignmentConflict
{
    public int AssignmentConflictId { get; set; }

    public int AssignmentId { get; set; }

    public string ConflictType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public string SystemRecommendation { get; set; } = string.Empty;
}
