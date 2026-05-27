using System;

namespace MyProject.Core.Domain.ValueObjects;

/// <summary>
/// קוד רמת ערך בתוך מימד סיווג בליבה.
/// </summary>
/// <remarks>
/// <para>דוגמה מאקסל: תוכן התא "מתחיל", "זכר", "מכירות".</para>
/// <para>במסד הנתונים זה מתאים ל־<c>ClassificationLevel.LevelCode</c> תחת מימד מסוים.</para>
/// <para>תמיד משויך למימד אחד — לא עומד לבד בלי <see cref="ClassificationDimensionCode"/>.</para>
/// </remarks>
public readonly record struct ClassificationLevelCode
{
    public const int MaxLength = 128;

    public ClassificationLevelCode(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Classification level code cannot be empty or whitespace.", nameof(value));
        }

        if (trimmed.Length > MaxLength)
        {
            throw new ArgumentException($"Classification level code cannot exceed {MaxLength} characters.", nameof(value));
        }

        Value = trimmed;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
