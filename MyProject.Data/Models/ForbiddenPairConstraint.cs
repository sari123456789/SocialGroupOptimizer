namespace MyProject.Data.Models;

/// <summary>
/// אילוץ איסור בין שני משתתפים באותה ריצת שיבוץ — שניהם אסורים להיות באותה קבוצה.
/// </summary>
public class ForbiddenPairConstraint
{
    public int ForbiddenPairConstraintId { get; set; }

    public int AssignmentId { get; set; }

    public int FirstParticipantAssignmentId { get; set; }

    public int SecondParticipantAssignmentId { get; set; }

    public Assignment? Assignment { get; set; }

    public ParticipantAssignment? FirstParticipantAssignment { get; set; }

    public ParticipantAssignment? SecondParticipantAssignment { get; set; }
}
