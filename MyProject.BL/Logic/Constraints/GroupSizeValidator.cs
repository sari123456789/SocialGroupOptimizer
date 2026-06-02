using System.Collections.Generic;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;

namespace MyProject.BL.Logic.Constraints;

/// <summary>
/// מאמת שכל קבוצה מכבדת מגבלות גודל (מינימום / מקסימום משתתפים).
/// </summary>
/// <remarks>נקרא מ-: <see cref="ConstraintEngine"/> בלבד.</remarks>
public sealed class GroupSizeValidator
{
    /// <summary>
    /// בודק אילוצי גודל קבוצה ומחזיר הפרות.
    /// </summary>
    /// <param name="assignment">החלוקה הנוכחית.</param>
    /// <param name="constraints">רשימה שכבר סוננה ל-GroupSizeConstraint ב-ConstraintEngine.</param>
    /// <returns>הודעות שגיאה; ריק אם הכל תקין.</returns>
    public IReadOnlyList<string> Validate(
        Assignment assignment,
        IReadOnlyList<GroupSizeConstraint> constraints)
    {
        var errors = new List<string>();

        foreach (var constraint in constraints)
        {
            // IsSatisfied — לוגיקה ב-Core; אם true — מדלגים.
            if (constraint.IsSatisfied(assignment))
            {
                continue;
            }

            // חיפוש ידני במקום FirstOrDefault — assignment.Groups קטן בדרך כלל.
            var group = null as Group;
            foreach (var g in assignment.Groups)
            {
                if (g.Id == constraint.GroupId)
                {
                    group = g;
                    break;
                }
            }

            if (group is null)
            {
                // אילוץ מפנה לקבוצה שלא קיימת בחלוקה (למשל GroupId 3 חסר).
                errors.Add($"Group {constraint.GroupId} required by GroupSizeConstraint was not found in the assignment.");
            }
            else
            {
                // MinSize / MaxCapacity מוגדרים ב-GroupSizeConstraint ב-Core.
                errors.Add(
                    $"Group {constraint.GroupId} has {group.ParticipantIds.Count} participant(s) " +
                    $"but requires between {constraint.MinSize} and {constraint.MaxCapacity.Value}.");
            }
        }

        return errors;
    }
}
