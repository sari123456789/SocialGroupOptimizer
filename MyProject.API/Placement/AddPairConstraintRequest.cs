namespace MyProject.API.Placement;

// מחלקה סגורה (sealed) שמייצגת בקשה לצימוד שני משתתפים.
public sealed class AddPairConstraintRequest
{
    public string ParticipantA { get; set; } = string.Empty;

    public string ParticipantB { get; set; } = string.Empty;
}
