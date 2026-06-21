// מרחב השמות של ה-API ל-Placement (file-scoped namespace עם `;`).
namespace MyProject.API.Placement;

// מחלקת DTO סגורה שסכום נתוני שיוך (Assignment) מוצג בה.
public sealed class AssignmentSummaryDto
// הסוגריים פותחים את גוף המחלקה.
{
    // מאפיין AssignmentId מסוג `int` עם `get; set;` (ללא אתחול מפורש, ברירת מחדל 0).
    public int AssignmentId { get; set; }

    // מאפיין AssignmentName מסוג `string` עם `get; set;` ואתחול באמצעות `= string.Empty`.
    public string AssignmentName { get; set; } = string.Empty;

    // מאפיין ParticipantCount מסוג `int` עם `get; set;` (ברירת מחדל 0).
    public int ParticipantCount { get; set; }
}
