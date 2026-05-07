using System;

namespace MyProject.Core.Domain.ValueObjects;

/// <summary>
/// מזהה משתתף בדומיין: מספר זהות ישראלי (תשע ספרות, כולל אפסים מובילים).
/// </summary>
public readonly record struct ParticipantId
{
    /// <summary>
    /// מאתחל מופע חדש של המבנה <see cref="ParticipantId"/>.
    /// </summary>
    /// <param name="value">מחרוזת בת תשע ספרות בלבד.</param>
    /// <exception cref="ArgumentNullException">נזרק כאשר <paramref name="value"/> הוא null.</exception>
    /// <exception cref="ArgumentException">נזרק כאשר <paramref name="value"/> אינו מחרוזת חוקית של מספר זהות.</exception>
    public ParticipantId(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var trimmed = value.Trim();

        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Participant id cannot be empty or whitespace.", nameof(value));
        }

        if (trimmed.Length != 9)
        {
            throw new ArgumentException("Participant id must be exactly 9 digits.", nameof(value));
        }

        foreach (var c in trimmed)
        {
            if (!char.IsDigit(c))
            {
                throw new ArgumentException("Participant id must contain digits only.", nameof(value));
            }
        }

        Value = trimmed;
    }

    /// <summary>
    /// מחזיר את מספר הזהות כמחרוזת בת תשע ספרות.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// מחזיר את המזהה כמחרוזת.
    /// </summary>
    /// <returns>ייצוג טקסטואלי של המזהה.</returns>
    public override string ToString() => Value;
}
