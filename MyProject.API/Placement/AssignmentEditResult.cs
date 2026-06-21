namespace MyProject.API.Placement;

// מגדיר מחלקת DTO לתוצאת עריכה (הצלחה/כשל) של פרטי משימה;
public sealed class AssignmentEditResult
{
    // מציין האם פעולת העריכה הצליחה.
    public bool Success { get; init; }

    // אוסף הודעות שגיאה במקרה כשל; אתחול לרשימה ריקה כדי להימנע מ-null.
    public List<string> Errors { get; init; } = new();

    // מציין פרטי משימה מפורטים עבור מצב הצלחה (או null במצב כשל).
    public AssignmentDetailDto? Detail { get; init; }

    // יוצר תוצאת עריכה מוצלחת על בסיס הפרט הנתון; זהו חבר סטטי בביטוי-גוף.
    public static AssignmentEditResult Ok(AssignmentDetailDto detail) =>
        // מאתחל מופע חדש של `AssignmentEditResult` באמצעות object initializer.
        new() { Success = true, Detail = detail };

    // יוצר תוצאת כשל ממערך שגיאות המועבר כ-`params`; זהו חבר סטטי בביטוי-גוף.
    public static AssignmentEditResult Fail(params string[] errors) =>
        // מאתחל מופע כשל וממיר את המערך לרשימה  .
        new() { Success = false, Errors = errors.ToList() };

    // יוצר תוצאת כשל ממקור שגיאות שמיוצג כ-`IEnumerable<string>`.
    public static AssignmentEditResult Fail(IEnumerable<string> errors) =>
        // מאתחל מופע כשל וממיר את ה-`IEnumerable` לרשימה  
        new() { Success = false, Errors = errors.ToList() };
}
