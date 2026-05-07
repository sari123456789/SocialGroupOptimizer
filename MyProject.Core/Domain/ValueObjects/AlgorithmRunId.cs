using System;

namespace MyProject.Core.Domain.ValueObjects;

/// <summary>
/// מזהה ייחודי לריצה יחידה של אלגוריתם הקצאה.
/// </summary>
/// <remarks>
/// משמש למעקב, לוגים, מניעת כפילויות וניתוח תוצאות.
/// מיועד לשימוש עתידי בשכבת ה-BL בהקשר הרצה.
/// </remarks>
public readonly record struct AlgorithmRunId
{
    /// <summary>
    /// מאתחל מופע חדש של המבנה <see cref="AlgorithmRunId"/>.
    /// </summary>
    /// <param name="value">ערך המזהה.</param>
    /// <exception cref="ArgumentException">נזרק כאשר <paramref name="value"/> הוא <see cref="Guid.Empty"/>.</exception>
    public AlgorithmRunId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Algorithm run id cannot be an empty Guid.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// ערך המזהה.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// יוצר מזהה ריצה חדש אקראי.
    /// </summary>
    /// <returns>מופע חדש של <see cref="AlgorithmRunId"/> עם <see cref="Guid"/> אקראי.</returns>
    public static AlgorithmRunId New() => new(Guid.NewGuid());

    /// <summary>
    /// מחזיר ייצוג טקסטואלי של מזהה הריצה.
    /// </summary>
    /// <returns>ייצוג טקסטואלי של מזהה הריצה.</returns>
    public override string ToString() => Value.ToString();
}
