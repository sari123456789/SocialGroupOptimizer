using System.Collections.Generic;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;

namespace MyProject.BL.Logic.Constraints;

/// <summary>
/// מאמת שכל קבוצה מכבדת את אילוצי גודל הקבוצה שלה.
/// </summary>
public sealed class GroupSizeValidator
{
    /// <summary>
    /// בודק את כל אילוצי גודל הקבוצה ומחזיר הודעות שגיאה עבור הפרות.
    /// </summary>
    /// <param name="assignment">ההקצאה לבדיקה.</param>
    /// <param name="constraints">אילוצי גודל קבוצה לאימות.</param>
    /// <returns>רשימת הודעות שגיאה; ריקה אם לא נמצאו הפרות.</returns>
    public IReadOnlyList<string> Validate(
        Assignment assignment,
        IReadOnlyList<GroupSizeConstraint> constraints)
    {
        var errors = new List<string>();

        foreach (var constraint in constraints)
        {
            if (constraint.IsSatisfied(assignment))
            {
                continue;
            }

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
                errors.Add($"Group {constraint.GroupId} required by GroupSizeConstraint was not found in the assignment.");
            }
            else
            {
                errors.Add(
                    $"Group {constraint.GroupId} has {group.ParticipantIds.Count} participant(s) " +
                    $"but requires between {constraint.MinSize} and {constraint.MaxCapacity.Value}.");
            }
        }

        return errors;
    }
}
