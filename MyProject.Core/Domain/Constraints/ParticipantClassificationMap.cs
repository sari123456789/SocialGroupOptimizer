using System.Collections.Generic;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.Core.Domain.Constraints;

/// <summary>
/// שם טיפוסי למבנה מפת סיווגי משתתפים: מזהה משתתף → (מימד → רמת ערך).
/// </summary>
/// <remarks>
/// <para>אותו מבנה מופיע ישירות באילוצי סיווג ובמיפוי מ־Data.</para>
/// <para><see cref="Empty"/> — מילון ריק לשימוש עתידי; כרגע אין קריאות ממנו בפרויקט.</para>
/// </remarks>
public static class ParticipantClassificationMap
{
    /// <summary>מילון ריק — ברירת מחדל כשאין עדיין סיווגי משתתפים.</summary>
    public static IReadOnlyDictionary<ParticipantId, IReadOnlyDictionary<ClassificationDimensionCode, ClassificationLevelCode>> Empty { get; } =
        new Dictionary<ParticipantId, IReadOnlyDictionary<ClassificationDimensionCode, ClassificationLevelCode>>();
}
