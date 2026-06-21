namespace MyProject.API.Placement;

// בקשה לעדכון משתתף קיים — כל השדות אופציונליים
public sealed class UpdateParticipantRequest
{
    // שם תצוגה חדש — null אומר לא לשנות
    public string? DisplayName { get; set; }

    // עדכון סיווגים — null אומר לא לשנות
    public Dictionary<string, string>? Classifications { get; set; }

    // עדכון העדפות — null אומר לא לשנות
    public List<string>? Preferences { get; set; }
}
