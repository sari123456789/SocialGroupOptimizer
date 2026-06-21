namespace MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Models;

/// <summary>
/// סוג הפיצוי שיוצא מקבוצת היעד אחרי איסוף אשכול חברים מפוצל.
/// משמש לתיעוד ולדיבוג — לא משנה את לוגיקת האימות.
/// </summary>
public enum CompensationType
{
    /// <summary>פיצוי כאשכול שלם.</summary>
    Cluster = 0,

    /// <summary>פיצוי כמשתתפים בודדים.</summary>
    Individuals = 1,

    /// <summary>פיצוי תוך שמירה על יחידות חובה.</summary>
    MandatoryUnits = 2,

    /// <summary>שילוב של כמה סוגי פיצוי.</summary>
    Mixed = 3,
}
