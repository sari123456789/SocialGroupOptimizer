namespace MyProject.API.Placement;

// מחלקת DTO סגורה שמייצגת פריט של אילוץ בין זוג משתתפים.
public sealed class PairConstraintItemDto
// הסוגריים פותחים את גוף המחלקה.
{
    // מאפיין ConstraintId מסוג `int` עם `get; set;` (ברירת מחדל של `int` היא 0).
    public int ConstraintId { get; set; }

    // מאפיין ParticipantA מסוג `string` עם `get; set;` ואתחול ל-`string.Empty`.
    public string ParticipantA { get; set; } = string.Empty;

    // מאפיין ParticipantB מסוג `string` עם `get; set;` ואתחול ל-`string.Empty`.
    public string ParticipantB { get; set; } = string.Empty;
}
