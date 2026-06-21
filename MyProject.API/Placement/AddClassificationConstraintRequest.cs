namespace MyProject.API.Placement;

// מחלקה סגורה (sealed) שמייצגת בקשה להוספת אילוץ סיווג.
public sealed class AddClassificationConstraintRequest
// הסוגריים פותחים את גוף המחלקה.
{
    // מאפיין DimensionCode מסוג `string` עם `get; set;` ואתחול ל-`string.Empty`.
    public string DimensionCode { get; set; } = string.Empty;

    // מאפיין RuleType מסוג `string` עם `get; set;` ואתחול ל-`string.Empty`.
    public string RuleType { get; set; } = string.Empty;
}
