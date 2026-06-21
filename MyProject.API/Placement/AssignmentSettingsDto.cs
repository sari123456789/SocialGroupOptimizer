// מגדיר את מרחב השמות של ה-DTO ומסווג את המחלקה תחת `MyProject.API.Placement`.
// תחביר C#: `namespace ...;`
namespace MyProject.API.Placement;

// מגדיר מחלקת DTO שמאגדת הגדרות להצבה (מינימום/מקסימום); `sealed` מונע ירושה.
// תחביר C#: `public sealed class ...`
public sealed class AssignmentSettingsDto
// מתחיל את גוף המחלקה עבור ה-DTO; זהו `class` בלוק שנפתח.
// תחביר C#: `{`
{
    // מגדיר את מספר הקבוצות המינימלי המותר.
    // תחביר C#: `public int MinGroups { get; set; }`
    public int MinGroups { get; set; }

    // מגדיר את מספר הקבוצות המקסימלי המותר.
    // תחביר C#: `public int MaxGroups { get; set; }`
    public int MaxGroups { get; set; }

    // מגדיר את גודל הקבוצה המינימלי.
    // תחביר C#: `public int MinGroupSize { get; set; }`
    public int MinGroupSize { get; set; }

    // מגדיר את גודל הקבוצה המקסימלי.
    // תחביר C#: `public int MaxGroupSize { get; set; }`
    public int MaxGroupSize { get; set; }
    // סוגר את גוף המחלקה.
    // תחביר C#: `}`
}
