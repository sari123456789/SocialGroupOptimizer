using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Enums;

namespace MyProject.Core.Domain.Constraints;

/// <summary>
/// חוזה לאילוץ קשה לאימות פתרון הקצאה.
/// </summary>
public interface IConstraint
{
    /// <summary>
    /// מחזיר את קטגוריית האילוץ.
    /// </summary>
    ConstraintType Type { get; }

    /// <summary>
    /// בודק האם ההקצאה הנתונה מקיימת את האילוץ.
    /// </summary>
    /// <param name="assignment">ההקצאה לבדיקה.</param>
    /// <returns><see langword="true"/> אם האילוץ מתקיים; אחרת <see langword="false"/>.</returns>
    bool IsSatisfied(Assignment assignment);
}
