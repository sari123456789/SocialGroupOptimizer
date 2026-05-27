using System;

namespace MyProject.Core.Domain.ValueObjects;

/// <summary>
/// קוד מימד סיווג בליבה — מייצג "ציר" אחד של סיווג, לא ערך בודד.
/// </summary>
/// <remarks>
/// <para>דוגמה מאקסל: כותרת עמודה "רמה", "מגדר", "מחלקה".</para>
/// <para>במסד הנתונים זה מתאים ל־<c>ClassificationDimension.DimensionCode</c>.</para>
/// <para>לכל משתתף יש לכל היותר ערך אחד בכל מימד (מילון מימד → רמה).</para>
/// </remarks>
public readonly record struct ClassificationDimensionCode
{
    public const int MaxLength = 256;

    public ClassificationDimensionCode(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Classification dimension code cannot be empty or whitespace.", nameof(value));
        }

        if (trimmed.Length > MaxLength)
        {
            throw new ArgumentException($"Classification dimension code cannot exceed {MaxLength} characters.", nameof(value));
        }

        Value = trimmed;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
