using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;

namespace MyProject.BL.Logic.Constraints;

/// <summary>
/// מאמת שמשתתפים שאסור להם להיות יחד אינם מוקצים לאותה קבוצה.
/// </summary>
public sealed class ForbiddenPairValidator
{
    /// <summary>
    /// בודק את כל אילוצי הזוג האסור ומחזיר הודעות שגיאה עבור הפרות.
    /// </summary>
    /// <param name="assignment">ההקצאה לבדיקה.</param>
    /// <param name="constraints">אילוצי זוג אסור לאימות.</param>
    /// <returns>רשימת הודעות שגיאה; ריקה אם לא נמצאו הפרות.</returns>
    public IReadOnlyList<string> Validate(
        Assignment assignment,
        IReadOnlyList<ForbiddenPairConstraint> constraints)
    {
        var errors = new List<string>();

        foreach (var constraint in constraints)
        {
            if (constraint.IsSatisfied(assignment))
            {
                continue;
            }

            var group = assignment.Groups.FirstOrDefault(g =>
                g.ParticipantIds.Contains(constraint.ParticipantA)
                && g.ParticipantIds.Contains(constraint.ParticipantB));

            if (group is not null)
            {
                errors.Add($"ForbiddenPair violated: {constraint.ParticipantA} and {constraint.ParticipantB} are both in group {group.Id}.");
            }
        }

        return errors;
    }
}
