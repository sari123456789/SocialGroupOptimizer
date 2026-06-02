using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;

namespace MyProject.BL.Logic.Constraints;

/// <summary>
/// מאמת שזוגות חובה נמצאים באותה קבוצה.
/// </summary>
/// <remarks>נקרא מ-: <see cref="ConstraintEngine"/> בלבד.</remarks>
public sealed class MandatoryPairValidator
{
    /// <summary>
    /// בודק MandatoryPairConstraint ומחזיר הפרות.
    /// </summary>
    /// <param name="assignment">החלוקה לבדיקה.</param>
    /// <param name="constraints">זוגות חובה (כבר מסוננים).</param>
    /// <returns>הודעות שגיאה מפורטות לפי סוג ההפרה.</returns>
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

            // FirstOrDefault + Contains — מוצא באיזו קבוצה (אם בכלל) נמצא כל משתתף.
            var groupOfA = assignment.Groups.FirstOrDefault(g => g.ParticipantIds.Contains(constraint.ParticipantA));
            var groupOfB = assignment.Groups.FirstOrDefault(g => g.ParticipantIds.Contains(constraint.ParticipantB));

            // ארבעה מצבים — הודעה שונה לכל אחד (עוזר בדיבוג וב-UI).
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
                // שניהם משובצים אבל בקבוצות שונות — הפרת זוג חובה קלאסית.
                errors.Add($"MandatoryPair violated: {constraint.ParticipantA} is in group {groupOfA.Id} but {constraint.ParticipantB} is in group {groupOfB.Id}.");
            }
        }

        return errors;
    }
}
