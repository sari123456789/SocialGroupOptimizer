namespace MyProject.Data.Models;

/// <summary>
/// אילוץ חובה בין שני משתתפים באותה ריצת שיבוץ — שניהם חייבים להיות באותה קבוצה.
/// </summary>
public class MandatoryPairConstraint
{
    public int MandatoryPairConstraintId { get; set; }

    public int AssignmentId { get; set; }

    public int FirstParticipantAssignmentId { get; set; }

    public int SecondParticipantAssignmentId { get; set; }

    public Assignment? Assignment { get; set; }

    public ParticipantAssignment? FirstParticipantAssignment { get; set; }

    public ParticipantAssignment? SecondParticipantAssignment { get; set; }
}
