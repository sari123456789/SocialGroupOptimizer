// מרחב השמות של ה-API למודול Placement (file-scoped namespace עם `;`).
namespace MyProject.API.Placement;

// DTO מחלקה סגורה שמכילה פריט של אילוץ על סיווג.
public sealed class ClassificationConstraintItemDto
// הסוגריים פותחים את גוף המחלקה.
{
    // קוד מימד מסוג `string` עם `get; set;` ואתחול בעזרת `= string.Empty`.
    public string DimensionCode { get; set; } = string.Empty;

    // סוג כלל מסוג `string` עם `get; set;` ואתחול בעזרת `= string.Empty`.
    public string RuleType { get; set; } = string.Empty;
}
