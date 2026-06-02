using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;

namespace MyProject.BL.Logic.Constraints;

/// <summary>
/// מאמת שיחסי רמות מימד בכל קבוצה תואמים לפרופורציות הגלובליות (עם סטייה מותרת).
/// </summary>
/// <remarks>נקרא מ-: <see cref="ConstraintEngine"/> בלבד.</remarks>
public sealed class ClassificationProportionalBalanceValidator
{
    /// <summary>
    /// בודק ClassificationProportionalBalanceConstraint ומחזיר הפרות.
    /// </summary>
    /// <param name="assignment">החלוקה לבדיקה.</param>
    /// <param name="constraints">אילוצי איזון יחסי (כבר מסוננים).</param>
    /// <returns>הודעות שגיאה; ריק אם הפרופורציות בכל קבוצה בטווח הסטייה המותרת.</returns>
    public IReadOnlyList<string> Validate(
        Assignment assignment,
        IReadOnlyList<ClassificationProportionalBalanceConstraint> constraints)
    {
        var errors = new List<string>();

        foreach (var constraint in constraints)
        {
            if (constraint.IsSatisfied(assignment))
            {
                continue;
            }

            // OrderBy + string.Join — ממיינים רמות לפי ערך ומציגים ברשימה לקריאות ההודעה.
            var levels = string.Join(", ", constraint.DimensionLevels.OrderBy(x => x.Value));

            // MaxScaledDeviation — סף סטייה מותרת מהפרופורציה הגלובלית (נוסחה בהודעה).
            errors.Add(
                $"ClassificationProportionalBalance violated for dimension '{constraint.TargetDimension}' levels [{levels}]: " +
                $"each group's level counts must match global proportions within scaled deviation {constraint.MaxScaledDeviation} " +
                $"(|groupCount*N - globalCount*groupSize|). Participants must have exactly one allowed level in this dimension.");
        }

        return errors;
    }
}
