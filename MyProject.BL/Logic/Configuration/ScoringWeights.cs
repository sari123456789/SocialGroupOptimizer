using System;

namespace MyProject.BL.Logic.Configuration;

/// <summary>
/// משקלות לרכיבי מנוע הניקוד.
/// </summary>
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
    /// משקל ברירת מחדל לאיזון סיווגים.
    /// </summary>
    public const double DefaultClassificationBalanceWeight = 0.5;

    /// <summary>
    /// מאתחל מופע חדש של <see cref="ScoringWeights"/> עם הגדרות ברירת מחדל.
    /// </summary>
    public ScoringWeights()
        : this(DefaultSocialPreferenceWeight, DefaultIsolationPenaltyWeight, DefaultClassificationBalanceWeight)
    {
    }

    /// <summary>
    /// מאתחל מופע חדש של <see cref="ScoringWeights"/> עם ערכים מותאמים.
    /// </summary>
    /// <param name="socialPreferenceWeight">משקל לסיפוק העדפות חברתיות. חייב להיות גדול מאפס.</param>
    /// <param name="isolationPenaltyWeight">משקל לקנס על בידוד חברתי. חייב להיות גדול מאפס.</param>
    /// <param name="classificationBalanceWeight">משקל לאיזון סיווגים. חייב להיות גדול מאפס.</param>
    /// <exception cref="ArgumentOutOfRangeException">נזרק כאשר אחד מהמשקלות אינו חיובי.</exception>
    public ScoringWeights(
        double socialPreferenceWeight,
        double isolationPenaltyWeight,
        double classificationBalanceWeight)
    {
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
    /// משקל לרכיב איזון הסיווגים.
    /// </summary>
    public double ClassificationBalanceWeight { get; }
}
