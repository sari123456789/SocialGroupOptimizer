namespace MyProject.API.Placement;

// בקשה להוספת משתתף לחלוקה
public sealed class AddParticipantRequest
{
    // תעודת זהות של המשתתף
    public string ParticipantId { get; set; } = string.Empty;

    // שם תצוגה — אופציונלי
    public string? DisplayName { get; set; }

    // סיווגים: ממד לרמה
    public Dictionary<string, string> Classifications { get; set; } = new();

    // משתתפים מועדפים לפי סדר
    public List<string> Preferences { get; set; } = new();
}
