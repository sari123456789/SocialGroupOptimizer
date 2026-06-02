using System;

namespace MyProject.BL.Logic.Configuration;

/// <summary>
/// הגדרות זמן ריצה ללולאת חיפוש/שיפור (עצירה, קיפאון, גיוון).
/// </summary>
/// <remarks>תפקיד עתידי: LocalSearchEngine ו-Orchestrator לשיפור חלוקה.</remarks>
public sealed class RuntimeSettings
{
    /// <summary>
    /// מגבלת זמן ריצה מקסימלית ברירת מחדל (30 שניות).
    /// </summary>
    public static readonly TimeSpan DefaultMaxRuntime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// סף קיפאון ברירת מחדל: איטרציות ללא שיפור לפני גיוון.
    /// </summary>
    public const int DefaultStagnationThreshold = 50;

    /// <summary>
    /// סף גיוון ברירת מחדל: מקסימום הפעלות גיוון לפני עצירה.
    /// </summary>
    public const int DefaultDiversificationThreshold = 5;

    /// <summary>
    /// מאתחל עם ערכי ברירת מחדל.
    /// </summary>
    public RuntimeSettings()
        : this(DefaultMaxRuntime, DefaultStagnationThreshold, DefaultDiversificationThreshold)
    {
    }

    /// <summary>
    /// מאתחל עם ערכי זמן ריצה מותאמים.
    /// </summary>
    /// <param name="maxRuntime">משך מקסימלי לתהליך — חייב להיות חיובי.</param>
    /// <param name="stagnationThreshold">איטרציות ללא שיפור לפני גיוון — חייב להיות &gt; 0.</param>
    /// <param name="diversificationThreshold">מקסימום הפעלות גיוון — חייב להיות &gt; 0.</param>
    /// <exception cref="ArgumentOutOfRangeException">כאשר ערך אינו חיובי.</exception>
    public RuntimeSettings(TimeSpan maxRuntime, int stagnationThreshold, int diversificationThreshold)
    {
        // TimeSpan.Zero — משך אפס; maxRuntime חייב להיות גדול מ-0.
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

        // StagnationThreshold — כמה איטרציות בלי שיפור לפני גיוון.
        // DiversificationThreshold — כמה פעמים מותר להפעיל גיוון לפני עצירה.
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
