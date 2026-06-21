namespace MyProject.API.Placement;

// DTO מחלקה סגורה שמייצגת דירוג (Rank) והעדפה עבור משתתף.
public sealed class ParticipantPreferenceItemDto
{
    public int Rank { get; set; }

    // מאפיין מזהה משתתף מסוג 
    public string ParticipantId { get; set; } = string.Empty;

    // מאפיין DisplayName 
    public string? DisplayName { get; set; }
}
