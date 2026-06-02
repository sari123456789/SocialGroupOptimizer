using System.Collections.Generic;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;

namespace MyProject.BL.Logic.Constraints;

/// <summary>
/// מאמת שמספר הקבוצות בהקצאה נמצא בטווח המותר.
/// </summary>
/// <remarks>נקרא מ-: <see cref="ConstraintEngine"/> בלבד.</remarks>
public sealed class GroupCountValidator
{
    /// <summary>
    /// בודק GroupCountConstraint ומחזיר הפרות.
    /// </summary>
    /// <param name="assignment">החלוקה לבדיקה.</param>
    /// <param name="constraints">אילוצי מספר קבוצות (כבר מסוננים).</param>
    /// <returns>הודעות שגיאה; ריק אם לא נמצאו הפרות.</returns>
    public IReadOnlyList<string> Validate(
        Assignment assignment,
        IReadOnlyList<GroupCountConstraint> constraints)
    {
        var errors = new List<string>();

        // Count — מספר הקבוצות בפועל; נשמר מחוץ ללולאה כי זהה לכל האילוצים.
        var actualCount = assignment.Groups.Count;

        foreach (var constraint in constraints)
        {
            if (constraint.IsSatisfied(assignment))
            {
                continue;
            }

            // MinGroups / MaxGroups — גבולות הטווח המותר מוגדרים ב-Core.
            errors.Add(
                $"Assignment has {actualCount} group(s) " +
                $"but requires between {constraint.MinGroups} and {constraint.MaxGroups}.");
        }

        return errors;
    }
}
