using System;

namespace MyProject.Core.Domain.ValueObjects;

/// <summary>
/// מזהה ייחודי לתוצאת הקצאה.
/// </summary>
public readonly record struct AssignmentId
{
    /// <summary>
    /// מאתחל מופע חדש של המבנה <see cref="AssignmentId"/>.
    /// </summary>
    /// <param name="value">ערך המזהה.</param>
    /// <exception cref="ArgumentException">נזרק כאשר <paramref name="value"/> הוא <see cref="Guid.Empty"/>.</exception>
    public AssignmentId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Assignment id cannot be an empty Guid.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// ערך המזהה.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// יוצר מזהה חדש אקראי.
    /// </summary>
    /// <returns>מופע חדש של <see cref="AssignmentId"/> עם <see cref="Guid"/> אקראי.</returns>
    public static AssignmentId New() => new(Guid.NewGuid());

    /// <summary>
    /// מחזיר ייצוג טקסטואלי של המזהה.
    /// </summary>
    /// <returns>ייצוג טקסטואלי של המזהה.</returns>
    public override string ToString() => Value.ToString();
}
