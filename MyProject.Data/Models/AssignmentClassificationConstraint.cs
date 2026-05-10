namespace MyProject.Data.Models;

/// <summary>
/// אילוץ סיווג לשיבוץ.
/// </summary>
public class AssignmentClassificationConstraint
{
    public int AssignmentId { get; set; }

    public int ClassificationId { get; set; }

    public bool IsBalanceOrSeparation { get; set; }
}
