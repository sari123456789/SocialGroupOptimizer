namespace MyProject.Data.Models;

/// <summary>
/// אילוץ גודל קבוצה לשיבוץ.
/// </summary>
public class GroupSizeConstraint
{
    public int GroupId { get; set; }

    public int AssignmentId { get; set; }

    public int MinGroupSize { get; set; }

    public int MaxGroupSize { get; set; }
}
