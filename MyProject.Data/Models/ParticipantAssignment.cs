namespace MyProject.Data.Models;

/// <summary>
/// שורת שיבוץ משתתף לריצת שיבוץ מסוימת. מזהי <see cref="FromParticipantAssignmentId"/> / <see cref="ToParticipantAssignmentId"/> בהעדפות חברתיות מתייחסים ל־<see cref="ParticipantAssignmentId"/>.
/// </summary>
public class ParticipantAssignment
{
    public int ParticipantAssignmentId { get; set; }

    public int ParticipantId { get; set; }

    public int AssignmentId { get; set; }

    public int ManagerId { get; set; }

    /// <summary>תרומת המשתתף לאיזון הקבוצה (חיובי/שלילי).</summary>
    public double BalanceContribution { get; set; }
}
    