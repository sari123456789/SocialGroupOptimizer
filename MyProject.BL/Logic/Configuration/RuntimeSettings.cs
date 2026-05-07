using System;

namespace MyProject.BL.Logic.Configuration;

/// <summary>
/// הגדרות זמן ריצה לתהליך ההקצאה.
/// </summary>
public sealed class RuntimeSettings
{
    /// <summary>
    /// מגבלת זמן ריצה מקסימלית ברירת מחדל (30 שניות).
    /// </summary>
    public static readonly TimeSpan DefaultMaxRuntime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// סף קיפאון ברירת מחדל: מספר האיטרציות ללא שיפור לפני הפעלת גיוון.
    /// </summary>
    public const int DefaultStagnationThreshold = 50;

    /// <summary>
    /// סף גיוון ברירת מחדל: מספר פעמים שניתן להפעיל גיוון לפני עצירה.
    /// </summary>
    public const int DefaultDiversificationThreshold = 5;

    /// <summary>
    /// מאתחל מופע חדש של <see cref="RuntimeSettings"/> עם הגדרות ברירת מחדל.
    /// </summary>
    public RuntimeSettings()
        : this(DefaultMaxRuntime, DefaultStagnationThreshold, DefaultDiversificationThreshold)
    {
    }

    /// <summary>
    /// מאתחל מופע חדש של <see cref="RuntimeSettings"/> עם ערכים מותאמים.
    /// </summary>
    /// <param name="maxRuntime">זמן ריצה מקסימלי. חייב להיות חיובי.</param>
    /// <param name="stagnationThreshold">מספר איטרציות ללא שיפור לפני גיוון. חייב להיות גדול מאפס.</param>
    /// <param name="diversificationThreshold">מספר פעמים מקסימלי להפעלת גיוון. חייב להיות גדול מאפס.</param>
    /// <exception cref="ArgumentOutOfRangeException">נזרק כאשר אחד מהערכים אינו חיובי.</exception>
    public RuntimeSettings(TimeSpan maxRuntime, int stagnationThreshold, int diversificationThreshold)
    {
        if (maxRuntime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRuntime), "Max runtime must be greater than zero.");
        }

        if (stagnationThreshold <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stagnationThreshold), "Stagnation threshold must be greater than zero.");
        }

        if (diversificationThreshold <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(diversificationThreshold), "Diversification threshold must be greater than zero.");
        }

        MaxRuntime = maxRuntime;
        StagnationThreshold = stagnationThreshold;
        DiversificationThreshold = diversificationThreshold;
    }

    /// <summary>
    /// מגבלת זמן ריצה מקסימלית לתהליך ההקצאה.
    /// </summary>
    public TimeSpan MaxRuntime { get; }

    /// <summary>
    /// מספר איטרציות ללא שיפור לפני הפעלת גיוון מבוקר.
    /// </summary>
    public int StagnationThreshold { get; }

    /// <summary>
    /// מספר הפעלות מקסימלי של גיוון מבוקר לפני עצירת החיפוש.
    /// </summary>
    public int DiversificationThreshold { get; }
}
