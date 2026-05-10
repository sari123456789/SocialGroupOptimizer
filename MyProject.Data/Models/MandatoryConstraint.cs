namespace MyProject.Data.Models;

/// <summary>
/// אילוץ חובה או איסור לשיבוץ.
/// </summary>
public class MandatoryConstraint
{
    public int MandatoryConstraintId { get; set; }

    public int AssignmentId { get; set; }

    public string ConstraintName { get; set; } = string.Empty;

    public bool IsMandatory { get; set; }
}
