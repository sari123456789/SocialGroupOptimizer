namespace MyProject.Data.Models;

/// <summary>
/// מימד סיווג בקטלוג — מתאים לכותרת עמודה באקסל (למשל "רמה", "מגדר").
/// </summary>
/// <remarks>
/// לא שייך לשיבוץ בודד: אותו קטלוג מימדים משמש את כל הריצות.
/// הרמות האפשריות במימד נמצאות ב־<see cref="ClassificationLevel"/>.
/// </remarks>
public class ClassificationDimension
{
    /// <summary>מפתח מסד פנימי — לא מזהה ליבה.</summary>
    public int ClassificationDimensionId { get; set; }

    /// <summary>קוד טקסטואלי שממופה ל־<c>ClassificationDimensionCode</c> בליבה.</summary>
    public string DimensionCode { get; set; } = string.Empty;
}
