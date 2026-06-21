// מרחב השמות של ה-API עבור Placement (file-scoped namespace עם `;`).
namespace MyProject.API.Placement;

// מחלקה סגורה מסוג DTO שמייצגת קבוצה לפי סיווג (Dimension + Level) וכמות משתתפים.
public sealed class ClassificationGroupDto
// הסוגריים פותחים את גוף המחלקה.
{
    // קוד מימד מסוג `string` עם `get; set;` ואתחול ל-`string.Empty`.
    public string DimensionCode { get; set; } = string.Empty;

    // קוד רמה מסוג `string` עם `get; set;` ואתחול ל-`string.Empty`.
    public string LevelCode { get; set; } = string.Empty;

    // ספירת משתתפים מסוג `int` עם `get; set;` (ללא אתחול מפורש).
    public int ParticipantCount { get; set; }
}
