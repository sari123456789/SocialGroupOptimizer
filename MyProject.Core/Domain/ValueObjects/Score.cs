using System;

namespace MyProject.Core.Domain.ValueObjects;

/// <summary>
/// מייצג ציון הקצאה בדומיין.
/// </summary>
public readonly record struct Score
{
    /// <summary>
    /// מאתחל מופע חדש של המבנה <see cref="Score"/>.
    /// </summary>
    /// <param name="value">ערך הציון.</param>
    /// <exception cref="ArgumentException">נזרק כאשר <paramref name="value"/> הוא NaN.</exception>
    public Score(double value)
    {
        if (double.IsNaN(value))
        {
            throw new ArgumentException("Score value cannot be NaN.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// ערך הציון.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// מחזיר ייצוג טקסטואלי של הציון.
    /// </summary>
    /// <returns>ייצוג טקסטואלי של הציון.</returns>
    public override string ToString() => Value.ToString("G");
}
