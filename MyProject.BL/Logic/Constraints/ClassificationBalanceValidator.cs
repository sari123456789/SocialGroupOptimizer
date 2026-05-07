using System.Collections.Generic;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;

namespace MyProject.BL.Logic.Constraints;

/// <summary>
/// מאמת שהסיווגים מחולקים בין הקבוצות בהתאם לכללים המוגדרים.
/// </summary>
public sealed class ClassificationBalanceValidator
{
    /// <summary>
    /// בודק את כל אילוצי איזון הסיווגים ומחזיר הודעות שגיאה עבור הפרות.
    /// </summary>
    /// <param name="assignment">ההקצאה לבדיקה.</param>
    /// <param name="constraints">אילוצי איזון סיווגים לאימות.</param>
    /// <returns>רשימת הודעות שגיאה; ריקה אם לא נמצאו הפרות.</returns>
    public IReadOnlyList<string> Validate(
        Assignment assignment,
        IReadOnlyList<ClassificationBalanceConstraint> constraints)
    {
        var errors = new List<string>();

        foreach (var constraint in constraints)
        {
            if (!constraint.IsSatisfied(assignment))
            {
                errors.Add(
                    $"ClassificationBalance violated for classification {constraint.TargetClassification}: " +
                    $"each group must contain between {constraint.MinCountPerGroup} and {constraint.MaxCountPerGroup} participant(s).");
            }
        }

        return errors;
    }
}
