using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;

namespace MyProject.BL.Logic.Constraints;

/// <summary>
/// מאמת שמשתתפים שחייבים להיות יחד אכן מוקצים לאותה קבוצה.
/// </summary>
public sealed class MandatoryPairValidator
{
    /// <summary>
    /// בודק את כל אילוצי הזוג החובה ומחזיר הודעות שגיאה עבור הפרות.
    /// </summary>
    /// <param name="assignment">ההקצאה לבדיקה.</param>
    /// <param name="constraints">אילוצי זוג חובה לאימות.</param>
    /// <returns>רשימת הודעות שגיאה; ריקה אם לא נמצאו הפרות.</returns>
    public IReadOnlyList<string> Validate(
        Assignment assignment,
        IReadOnlyList<MandatoryPairConstraint> constraints)
    {
        var errors = new List<string>();

        foreach (var constraint in constraints)
        {
            if (constraint.IsSatisfied(assignment))
            {
                continue;
            }

            var groupOfA = assignment.Groups.FirstOrDefault(g => g.ParticipantIds.Contains(constraint.ParticipantA));
            var groupOfB = assignment.Groups.FirstOrDefault(g => g.ParticipantIds.Contains(constraint.ParticipantB));

            if (groupOfA is null && groupOfB is null)
            {
                errors.Add($"MandatoryPair violated: both {constraint.ParticipantA} and {constraint.ParticipantB} are missing from the assignment.");
            }
            else if (groupOfA is null)
            {
                errors.Add($"MandatoryPair violated: participant {constraint.ParticipantA} is missing from the assignment.");
            }
            else if (groupOfB is null)
            {
                errors.Add($"MandatoryPair violated: participant {constraint.ParticipantB} is missing from the assignment.");
            }
            else
            {
                errors.Add($"MandatoryPair violated: {constraint.ParticipantA} is in group {groupOfA.Id} but {constraint.ParticipantB} is in group {groupOfB.Id}.");
            }
        }

        return errors;
    }
}
