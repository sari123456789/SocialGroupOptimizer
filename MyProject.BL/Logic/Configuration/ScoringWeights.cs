using System;

namespace MyProject.BL.Logic.Configuration;

/// <summary>
/// משקלות לרכיבי מנוע הניקוד — מאפשרים כוונון ללא שינוי קוד.
/// </summary>
/// <remarks>נקרא מ-: <see cref="Scoring.ScoringManager"/>.</remarks>
public sealed class ScoringWeights
{
    /// <summary>
    /// משקל ברירת מחדל לסיפוק העדפות חברתיות.
    /// </summary>
    public const double DefaultSocialPreferenceWeight = 1.0;

    /// <summary>
    /// משקל ברירת מחדל לקנס על בידוד חברתי.
    /// </summary>
    public const double DefaultIsolationPenaltyWeight = 1.0;

    /// <summary>
    /// משקל ברירת מחדל לאיזון סיווגים (רכיב עתידי).
    /// </summary>
    public const double DefaultClassificationBalanceWeight = 0.5;

    /// <summary>
    /// מאתחל עם משקלות ברירת מחדל.
    /// </summary>
    public ScoringWeights()
        : this(DefaultSocialPreferenceWeight, DefaultIsolationPenaltyWeight, DefaultClassificationBalanceWeight)
    {
    }

    /// <summary>
    /// מאתחל עם משקלות מותאמים.
    /// </summary>
    /// <param name="socialPreferenceWeight">משקל לרכיב חברתי — חייב להיות &gt; 0.</param>
    /// <param name="isolationPenaltyWeight">משקל לקנס בידוד — חייב להיות &gt; 0.</param>
    /// <param name="classificationBalanceWeight">משקל לאיזון סיווג — חייב להיות &gt; 0 (שימוש עתידי).</param>
    /// <exception cref="ArgumentOutOfRangeException">כאשר משקל אינו חיובי.</exception>
    public ScoringWeights(
        double socialPreferenceWeight,
        double isolationPenaltyWeight,
        double classificationBalanceWeight)
    {
        // כל משקל חייב להיות חיובי — 0 או שלילי היו מבטלים או הופכים את רכיב הניקוד.
        if (socialPreferenceWeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(socialPreferenceWeight), "Social preference weight must be greater than zero.");
        }

        if (isolationPenaltyWeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(isolationPenaltyWeight), "Isolation penalty weight must be greater than zero.");
        }

        if (classificationBalanceWeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(classificationBalanceWeight), "Classification balance weight must be greater than zero.");
        }

        // properties עם get בלבד — immutable לאחר יצירה.
        SocialPreferenceWeight = socialPreferenceWeight;
        IsolationPenaltyWeight = isolationPenaltyWeight;
        ClassificationBalanceWeight = classificationBalanceWeight;
    }

    /// <summary>
    /// משקל לרכיב סיפוק העדפות חברתיות.
    /// </summary>
    public double SocialPreferenceWeight { get; }

    /// <summary>
    /// משקל לרכיב קנס הבידוד החברתי.
    /// </summary>
    public double IsolationPenaltyWeight { get; }

    /// <summary>
    /// משקל לרכיב איזון הסיווגים (עדיין לא בשימוש).
    /// </summary>
    public double ClassificationBalanceWeight { get; }
}
