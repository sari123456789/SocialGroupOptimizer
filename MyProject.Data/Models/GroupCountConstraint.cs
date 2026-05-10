namespace MyProject.Data.Models;

/// <summary>
/// אילוץ מספר קבוצות לשיבוץ (שורה אחת לשיבוץ).
/// </summary>
public class GroupCountConstraint
{
    public int AssignmentId { get; set; }

    public int MinGroups { get; set; }

    public int MaxGroups { get; set; }
}
