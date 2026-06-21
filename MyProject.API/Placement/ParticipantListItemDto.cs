namespace MyProject.API.Placement;

// מחלקת DTO סגורה לפרטי תצוגה של משתתף, כולל סיווגים והעדפות.
public sealed class ParticipantListItemDto
{
    public string ParticipantId { get; set; } = string.Empty;
    //שם תצוגה של המשתתף; יכול להיות null אם לא סופק
    public string? DisplayName { get; set; }

    // אוסף סיווגים מסוג 
    public Dictionary<string, string> Classifications { get; set; } = new();

    // רשימת העדפות מסוג 
    public List<ParticipantPreferenceItemDto> Preferences { get; set; } = new();
}
