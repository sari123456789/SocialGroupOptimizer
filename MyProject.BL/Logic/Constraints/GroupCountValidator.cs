using System.Collections.Generic;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;

namespace MyProject.BL.Logic.Constraints;

/// <summary>
/// מאמת שמספר הקבוצות בהקצאה עומד בדרישות האילוץ.
/// </summary>
public sealed class GroupCountValidator
{
    /// <summary>
    /// בודק את כל אילוצי מספר הקבוצות ומחזיר הודעות שגיאה עבור הפרות.
    /// </summary>
    /// <param name="assignment">ההקצאה לבדיקה.</param>
    /// <param name="constraints">אילוצי מספר קבוצות לאימות.</param>
    /// <returns>רשימת הודעות שגיאה; ריקה אם לא נמצאו הפרות.</returns>
    public IReadOnlyList<string> Validate(
        Assignment assignment,
        IReadOnlyList<GroupCountConstraint> constraints)
    {
        var errors = new List<string>();
        var actualCount = assignment.Groups.Count;

        foreach (var constraint in constraints)
        {
            if (constraint.IsSatisfied(assignment))
            {
                continue;
            }

            errors.Add(
                $"Assignment has {actualCount} group(s) " +
                $"but requires between {constraint.MinGroups} and {constraint.MaxGroups}.");
        }

        return errors;
    }
}
