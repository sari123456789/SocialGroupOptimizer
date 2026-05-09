using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;

namespace MyProject.BL.Logic.Constraints;

/// <summary>
/// מאמת אילוצי הפרדה הומוגנית לפי רמת מימד בקבוצה.
/// </summary>
public sealed class ClassificationHomogeneousGroupValidator
{
    /// <summary>
    /// בודק את כל אילוצי ההומוגניות ומחזיר הודעות שגיאה עבור הפרות.
    /// </summary>
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

            var levels = string.Join(", ", constraint.DimensionLevels.OrderBy(x => (int)x));
            errors.Add(
                $"ClassificationHomogeneousGroup violated for dimension levels [{levels}]: " +
                $"each group must contain participants of at most one level, and each participant must map to exactly one of these levels.");
        }

        return errors;
    }
}
