using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;

namespace MyProject.BL.Logic.Constraints;

/// <summary>
/// מאמת שבכל קבוצה כל המשתתפים שייכים לרמה אחת בלבד במימד הסיווג (הפרדה הומוגנית).
/// </summary>
/// <remarks>נקרא מ-: <see cref="ConstraintEngine"/> בלבד.</remarks>
public sealed class ClassificationHomogeneousGroupValidator
{
    /// <summary>
    /// בודק ClassificationHomogeneousGroupConstraint ומחזיר הפרות.
    /// </summary>
    /// <param name="assignment">החלוקה לבדיקה.</param>
    /// <param name="constraints">אילוצי הפרדה הומוגנית (כבר מסוננים).</param>
    /// <returns>הודעות שגיאה; ריק אם כל קבוצה מכילה לכל היותר רמה אחת מהמימד.</returns>
    public IReadOnlyList<string> Validate(
        Assignment assignment,
        IReadOnlyList<ClassificationHomogeneousGroupConstraint> constraints)
    {
        var errors = new List<string>();

        foreach (var constraint in constraints)
        {
            if (constraint.IsSatisfied(assignment))
            {
                continue;
            }

            // DimensionLevels — רמות מותרות במימד; OrderBy מסדר לתצוגה עקבית בהודעה.
            var levels = string.Join(", ", constraint.DimensionLevels.OrderBy(x => x.Value));

            // הפרה = ערבוב רמות באותה קבוצה, או משתתף עם יותר/פחות מרמה אחת במימד.
            errors.Add(
                $"ClassificationHomogeneousGroup violated for dimension '{constraint.TargetDimension}' levels [{levels}]: " +
                $"each group must contain participants of at most one level, and each participant must have exactly one allowed level in this dimension.");
        }

        return errors;
    }
}
