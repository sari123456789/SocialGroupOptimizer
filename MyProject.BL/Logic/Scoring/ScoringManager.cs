using System;
using System.Collections.Generic;
using MyProject.BL.Logic.Configuration;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Logic.Scoring;

/// <summary>
/// מנוע הניקוד: מתאם רכיבי ניקוד ומחזיר ציון הקצאה סופי.
/// </summary>
/// <remarks>
/// <para>נוסחה: ציון סופי = ציון חברתי − קנס בידוד.</para>
/// <para>נקרא מ-: עדיין לא בשימוש מחוץ ל-BL.</para>
/// </remarks>
public sealed class ScoringManager : IAssignmentScorer
{
    private readonly SocialConnectionScorer _socialConnectionScorer;
    private readonly IsolationPenaltyEvaluator _isolationPenaltyEvaluator;

    /// <summary>
    /// מאתחל מופע חדש עם רכיבי ניקוד סטנדרטיים.
    /// </summary>
    public ScoringManager()
    {
        // רכיבים stateless — נוצרים פעם אחת ומשמשים בכל קריאת CalculateScore.
        _socialConnectionScorer = new SocialConnectionScorer();
        _isolationPenaltyEvaluator = new IsolationPenaltyEvaluator();
    }

    /// <summary>
    /// מחשב ציון כולל להקצאה על בסיס כלל רכיבי הניקוד.
    /// </summary>
    /// <param name="assignment">החלוקה לניקוד.</param>
    /// <param name="participants">כל המשתתפים כולל העדפותיהם.</param>
    /// <param name="weights">משקלות מ-<see cref="ScoringWeights"/>.</param>
    /// <returns>ציון סופי כ-<see cref="Score"/> (חברתי מינוס קנס בידוד).</returns>
    /// <exception cref="ArgumentNullException">כאשר אחד מהפרמטרים הוא null.</exception>
    public Score CalculateScore(
        Assignment assignment,
        IReadOnlyList<Participant> participants,
        ScoringWeights weights)
    {
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        if (participants is null)
        {
            throw new ArgumentNullException(nameof(participants));
        }

        if (weights is null)
        {
            throw new ArgumentNullException(nameof(weights));
        }

        // רכיב חיובי — סיפוק העדפות חברתיות, מוכפל במשקל מההגדרות.
        var socialScore = _socialConnectionScorer.Calculate(
            assignment,
            participants,
            weights.SocialPreferenceWeight);

        // רכיב שלילי — קנס על משתתפים מבודדים; penaltyPerIsolatedParticipant: 1.0 — בסיס לפני משקל.
        var isolationPenalty = _isolationPenaltyEvaluator.Evaluate(
            assignment,
            participants,
            penaltyPerIsolatedParticipant: 1.0,
            weight: weights.IsolationPenaltyWeight);

        // .Value — גישה לערך המספרי בתוך Value Object (Score / Penalty).
        var total = socialScore.Value - isolationPenalty.Value;
        return new Score(total);
    }
}
