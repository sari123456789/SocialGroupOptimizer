using System;

namespace MyProject.Core.Domain.ValueObjects;

/// <summary>
/// מייצג קנס בפונקציית הניקוד, לדוגמה קנס על בידוד.
/// </summary>
/// <remarks>
/// <see cref="Penalty"/> הוא מושג נפרד מ-<see cref="Score"/>: קנס הוא עלות שלילית שמפחיתה את הציון,
/// בעוד שציון הוא ערך תוצאה כולל.
/// </remarks>
public readonly record struct Penalty
{
    /// <summary>
    /// מאתחל מופע חדש של המבנה <see cref="Penalty"/>.
    /// </summary>
    /// <param name="value">ערך הקנס.</param>
    /// <exception cref="ArgumentException">נזרק כאשר <paramref name="value"/> הוא NaN.</exception>
    /// <exception cref="ArgumentOutOfRangeException">נזרק כאשר <paramref name="value"/> שלילי.</exception>
    public Penalty(double value)
    {
        if (double.IsNaN(value))
        {
            throw new ArgumentException("Penalty value cannot be NaN.", nameof(value));
        }

        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Penalty value cannot be negative.");
        }

        Value = value;
    }

    /// <summary>
    /// ערך הקנס.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// קנס בערך אפס, לשימוש כברירת מחדל.
    /// </summary>
    public static Penalty Zero => new(0.0);

    /// <summary>
    /// מחזיר ייצוג טקסטואלי של הקנס.
    /// </summary>
    /// <returns>ייצוג טקסטואלי של הקנס.</returns>
    public override string ToString() => Value.ToString("G");
}
