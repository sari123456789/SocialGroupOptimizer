using System;

namespace MyProject.Core.Domain.ValueObjects;

/// <summary>
/// מזהה קבוצה בדומיין.
/// </summary>
public readonly record struct GroupId
{
    /// <summary>
    /// מאתחל מופע חדש של המבנה <see cref="GroupId"/>.
    /// </summary>
    /// <param name="value">ערך המזהה.</param>
    /// <exception cref="ArgumentOutOfRangeException">נזרק כאשר <paramref name="value"/> קטן או שווה לאפס.</exception>
    public GroupId(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Group id must be greater than zero.");
        }

        Value = value;
    }

    /// <summary>
    /// מחזיר את ערך המזהה המספרי.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// מחזיר את המזהה כמחרוזת.
    /// </summary>
    /// <returns>ייצוג טקסטואלי של המזהה.</returns>
    public override string ToString() => Value.ToString();
}
