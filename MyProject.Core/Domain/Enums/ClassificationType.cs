namespace MyProject.Core.Domain.Enums;

/// <summary>
/// סוג סיווג כללי של משתתף בדומיין.
/// </summary>
public enum ClassificationType
{
    /// <summary>
    /// סיווג לא מוגדר.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// דוגמה לסיווג רמת התחלה.
    /// </summary>
    Beginner = 1,

    /// <summary>
    /// דוגמה לסיווג רמת ביניים.
    /// </summary>
    Intermediate = 2,

    /// <summary>
    /// דוגמה לסיווג רמת מתקדמים.
    /// </summary>
    Advanced = 3
}
