using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;

namespace MyProject.BL.Logic.Constraints;

/// <summary>
/// מאמת אילוצי איזון יחסים של רמות מימד בקבוצות.
/// </summary>
public sealed class ClassificationProportionalBalanceValidator
{
    /// <summary>
    /// בודק את כל אילוצי האיזון היחסי ומחזיר הודעות שגיאה עבור הפרות.
    /// </summary>
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

            var levels = string.Join(", ", constraint.DimensionLevels.OrderBy(x => (int)x));
            errors.Add(
                $"ClassificationProportionalBalance violated for dimension levels [{levels}]: " +
                $"each group's level counts must match global proportions within scaled deviation {constraint.MaxScaledDeviation} " +
                $"(|groupCount*N - globalCount*groupSize|). Participants must map to exactly one of these levels.");
        }

        return errors;
    }
}
