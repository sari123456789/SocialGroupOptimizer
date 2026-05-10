namespace MyProject.Data.Models;

/// <summary>
/// קישור בין אילוץ חובה/אסור לשורת שיבוץ משתתף.
/// </summary>
public class MandatoryConstraintAssignment
{
    public int MandatoryConstraintId { get; set; }

    public int ParticipantAssignmentId { get; set; }
}
