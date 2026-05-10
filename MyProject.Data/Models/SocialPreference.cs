namespace MyProject.Data.Models;

/// <summary>
/// העדפה חברתית בין שני שיבוצי משתתף באותה ריצת שיבוץ.
/// </summary>
public class SocialPreference
{
    /// <summary>מזהה שורת השיבוץ של מכניס ההעדפה (מפתח זר ל־ParticipantAssignment).</summary>
    public int FromParticipantAssignmentId { get; set; }

    /// <summary>מזהה שורת השיבוץ של המשתתף המועדף.</summary>
    public int ToParticipantAssignmentId { get; set; }

    public int PreferenceWeight { get; set; }

    public int AssignmentId { get; set; }

    public ParticipantAssignment? FromParticipantAssignment { get; set; }

    public ParticipantAssignment? ToParticipantAssignment { get; set; }

    public Assignment? Assignment { get; set; }
}
