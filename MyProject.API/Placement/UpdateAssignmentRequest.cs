// מגדיר את מרחב השמות של ה-DTO ומסווג את המחלקה תחת `MyProject.API.Placement`.
// תחביר C#: `namespace ...;`
namespace MyProject.API.Placement;

// מגדיר מחלקת DTO לבקשת עדכון משימה; `sealed` מונע ירושה.
// תחביר C#: `public sealed class ...`
public sealed class UpdateAssignmentRequest
// מתחיל את גוף המחלקה עבור ה-DTO; זהו `class` בלוק שנפתח.
// תחביר C#: `{`
{
    // מגדיר שם משימה אופציונלי (ייתכן שלא יישלח) לעדכון.
    // תחביר C#: `string?` עם `public string? AssignmentName { get; set; }`
    public string? AssignmentName { get; set; }

    // מגדיר הגדרות משימה אופציונליות (ייתכן שלא יישלחו) לעדכון.
    // תחביר C#: `AssignmentSettingsDto?` עם `public ... { get; set; }`
    public AssignmentSettingsDto? Settings { get; set; }
    // סוגר את גוף המחלקה.
    // תחביר C#: `}`
}
