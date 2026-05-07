using System;

namespace MyProject.Core.Domain.ValueObjects;

/// <summary>
/// מייצג משקל העדפה חברתית.
/// </summary>
/// <remarks>
/// ערכים גבוהים יותר מציינים העדפה חזקה יותר. ערך אפס מותר ומציין העדפה ניטרלית.
/// </remarks>
public readonly record struct PreferenceWeight
{
    /// <summary>
    /// מאתחל מופע חדש של המבנה <see cref="PreferenceWeight"/>.
    /// </summary>
    /// <param name="value">ערך המשקל.</param>
    /// <exception cref="ArgumentOutOfRangeException">נזרק כאשר <paramref name="value"/> שלילי.</exception>
    public PreferenceWeight(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Preference weight cannot be negative.");
        }

        Value = value;
    }

    /// <summary>
    /// ערך המשקל.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// מחזיר ייצוג טקסטואלי של המשקל.
    /// </summary>
    /// <returns>ייצוג טקסטואלי של המשקל.</returns>
    public override string ToString() => Value.ToString();
}
