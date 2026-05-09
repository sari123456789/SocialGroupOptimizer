using System.Collections.Generic;
using MyProject.Core.Domain.Enums;

namespace MyProject.Core.Domain.Constraints;

/// <summary>
/// עזר לפענוח רמת מימד בודדת מתוך רשימת סיווגים של משתתף.
/// </summary>
internal static class ClassificationDimensionConstraintSupport
{
    /// <summary>
    /// מחזיר את ערך המימד אם בדיוק אחד מערכי <paramref name="dimensionLevels"/> מופיע אצל המשתתף; אחרת null.
    /// </summary>
    public static ClassificationType? TryResolveSingleDimensionLevel(
        IReadOnlyList<ClassificationType> participantClassifications,
        IReadOnlySet<ClassificationType> dimensionLevels)
    {
        if (participantClassifications is null || participantClassifications.Count == 0)
        {
            return null;
        }

        ClassificationType? found = null;
        foreach (var classification in participantClassifications)
        {
            if (classification == ClassificationType.Unspecified)
            {
                continue;
            }

            if (!dimensionLevels.Contains(classification))
            {
                continue;
            }

            if (found.HasValue && found.Value != classification)
            {
                return null;
            }

            found = classification;
        }

        return found;
    }

    public static HashSet<ClassificationType> ValidateAndCopyDimensionLevels(
        IEnumerable<ClassificationType> dimensionLevels,
        string paramName)
    {
        if (dimensionLevels is null)
        {
            throw new System.ArgumentNullException(paramName);
        }

        var set = new HashSet<ClassificationType>();
        foreach (var level in dimensionLevels)
        {
            if (level == ClassificationType.Unspecified)
            {
                throw new System.ArgumentException("Dimension levels cannot include Unspecified.", paramName);
            }

            set.Add(level);
        }

        if (set.Count < 1)
        {
            throw new System.ArgumentException("At least one dimension level is required.", paramName);
        }

        return set;
    }
}
