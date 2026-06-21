// מרחב שמות לבקשות ו-DTO של מודול החלוקה
namespace MyProject.API.Placement;

/// <summary>
/// בקשה ליצירת חלוקה חדשה מרשימת משתתפים שנבחרו בממשק.
/// </summary>
// מחלקת בקשה ליצירת חלוקה ממשתתפים שנבחרו — sealed למניעת ירושה
public sealed class CreateAssignmentFromParticipantsRequest
{
    // שם החלוקה החדשה; ברירת מחדל בעברית
    public string AssignmentName { get; set; } = "חלוקה חדשה";

    // מספר הקבוצות הרצוי בשיבוץ; ברירת מחדל 2
    public int GroupCount { get; set; } = 2;

    // גודל מינימלי לקבוצה
    public int MinGroupSize { get; set; } = 2;

    // גודל מקסימלי לקבוצה
    public int MaxGroupSize { get; set; } = 2;

    // רשימת המשתתפים שנבחרו לכלול בחלוקה; רשימה ריקה כברירת מחדל
    public List<SelectedParticipantForAssignmentDto> Participants { get; set; } = new();
}

/// <summary>
/// משתתף שנבחר לחלוקה חדשה — ת.ז., שם, סיווגים והעדפות חברתיות.
/// </summary>
// DTO לייצוג משתתף בודד שנבחר ליצירת חלוקה
public sealed class SelectedParticipantForAssignmentDto
{
    // תעודת זהות כמחרוזת (ParticipantId); ערך ריק כברירת מחדל
    public string ParticipantId { get; set; } = string.Empty;

    // שם לתצוגה; nullable כי עשוי להיות חסר
    public string? DisplayName { get; set; }

    // מילון סיווגים: שם ממד → ערך; מילון ריק כברירת מחדל
    public Dictionary<string, string> Classifications { get; set; } = new();

    // רשימת תעודות זהות של חברים מועדפים (העדפות חברתיות)
    public List<string> Preferences { get; set; } = new();
}
