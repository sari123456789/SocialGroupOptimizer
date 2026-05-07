using System;

namespace MyProject.Core.Domain.ValueObjects;

/// <summary>
/// מייצג קיבולת קבוצה בצורה בטוחה.
/// </summary>
public readonly record struct GroupCapacity
{
    /// <summary>
    /// ערך קיבולת מקסימלי מותר.
    /// </summary>
    public const int MaxAllowed = 10_000;

    /// <summary>
    /// מאתחל מופע חדש של המבנה <see cref="GroupCapacity"/>.
    /// </summary>
    /// <param name="value">גודל הקיבולת.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// נזרק כאשר <paramref name="value"/> קטן או שווה לאפס, או גדול מ-<see cref="MaxAllowed"/>.
    /// </exception>
    public GroupCapacity(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Group capacity must be greater than zero.");
        }

        if (value > MaxAllowed)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Group capacity cannot exceed {MaxAllowed}.");
        }

        Value = value;
    }

    /// <summary>
    /// ערך הקיבולת.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// מחזיר ייצוג טקסטואלי של הקיבולת.
    /// </summary>
    /// <returns>ייצוג טקסטואלי של הקיבולת.</returns>
    public override string ToString() => Value.ToString();
}
