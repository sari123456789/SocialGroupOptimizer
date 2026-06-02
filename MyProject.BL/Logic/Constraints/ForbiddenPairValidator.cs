using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;

namespace MyProject.BL.Logic.Constraints;

/// <summary>
/// מאמת שזוגות משתתפים אסורים אינם מוקצים לאותה קבוצה.
/// </summary>
/// <remarks>נקרא מ-: <see cref="ConstraintEngine"/> בלבד.</remarks>
public sealed class ForbiddenPairValidator
{
    /// <summary>
    /// בודק ForbiddenPairConstraint ומחזיר הפרות.
    /// </summary>
    /// <param name="assignment">החלוקה לבדיקה.</param>
    /// <param name="constraints">זוגות אסורים (כבר מסוננים).</param>
    /// <returns>הודעות שגיאה; ריק אם אין זוג אסור שחולק קבוצה.</returns>
    public IReadOnlyList<string> Validate(
        Assignment assignment,
        IReadOnlyList<ForbiddenPairConstraint> constraints)
    {
        var errors = new List<string>();

        foreach (var constraint in constraints)
        {
            // IsSatisfied — לוגיקה ב-Core; אם true — האילוץ מתקיים, מדלגים.
            if (constraint.IsSatisfied(assignment))
            {
                continue;
            }

            // FirstOrDefault + Contains — מחפש קבוצה שמכילה את שני המשתתפים יחד.
            // && (AND) — שני התנאים חייבים להתקיים באותה קבוצה.
            var group = assignment.Groups.FirstOrDefault(g =>
                g.ParticipantIds.Contains(constraint.ParticipantA)
                && g.ParticipantIds.Contains(constraint.ParticipantB));

            // "is not null" — pattern matching; רק אם מצאנו קבוצה משותפת — זו הפרה.
            if (group is not null)
            {
                errors.Add($"ForbiddenPair violated: {constraint.ParticipantA} and {constraint.ParticipantB} are both in group {group.Id}.");
            }
        }

        return errors;
    }
}
