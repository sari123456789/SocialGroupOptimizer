using System.Collections.Generic;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.Core.Domain.Constraints;

/// <summary>
/// עזר לאילוצי סיווג: קורא את רמת המשתתף רק במימד שהאילוץ מגדיר.
/// </summary>
/// <remarks>
/// לפני המודל המאוחד נסרקה רשימה שטוחה של "סוגי סיווג".
/// עכשיו לכל משתתף יש מילון מימד→רמה, והאילוץ מצביע במפורש על מימד אחד (<c>TargetDimension</c>).
/// </remarks>
internal static class ClassificationDimensionConstraintSupport
{
    /// <summary>
    /// מחזיר את רמת הערך של המשתתף במימד המבוקש, רק אם היא אחת מהרמות המותרות לאילוץ.
    /// </summary>
    /// <returns>null אם אין מימד במילון, או שהרמה לא ברשימת הרמות המותרות.</returns>
    public static ClassificationLevelCode? TryGetLevelInDimension(
        IReadOnlyDictionary<ClassificationDimensionCode, ClassificationLevelCode> participantClassifications,
        ClassificationDimensionCode targetDimension,
        IReadOnlySet<ClassificationLevelCode> allowedLevels)
    {
        if (participantClassifications is null || participantClassifications.Count == 0)
        {
            return null;
        }

        if (!participantClassifications.TryGetValue(targetDimension, out var level))
        {
            return null;
        }

        return allowedLevels.Contains(level) ? level : null;
    }

    public static HashSet<ClassificationLevelCode> ValidateAndCopyDimensionLevels(
        IEnumerable<ClassificationLevelCode> dimensionLevels,
        string paramName)
    {
        if (dimensionLevels is null)
        {
            throw new System.ArgumentNullException(paramName);
        }

        var set = new HashSet<ClassificationLevelCode>();
        foreach (var level in dimensionLevels)
        {
            set.Add(level);
        }

        if (set.Count < 1)
        {
            throw new System.ArgumentException("At least one dimension level is required.", paramName);
        }

        return set;
    }
}
