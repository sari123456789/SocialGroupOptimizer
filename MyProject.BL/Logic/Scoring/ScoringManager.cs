using System;
using System.Collections.Generic;
using MyProject.BL.Logic.Configuration;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Logic.Scoring;

/// <summary>
/// מנוע הניקוד: מתאם את רכיבי הניקוד ומייצר ציון הקצאה סופי.
/// </summary>
public sealed class ScoringManager : IAssignmentScorer
{
    private readonly SocialConnectionScorer _socialConnectionScorer;
    private readonly IsolationPenaltyEvaluator _isolationPenaltyEvaluator;

    /// <summary>
    /// מאתחל מופע חדש של <see cref="ScoringManager"/> עם רכיבי ניקוד סטנדרטיים.
    /// </summary>
    public ScoringManager()
    {
        _socialConnectionScorer = new SocialConnectionScorer();
        _isolationPenaltyEvaluator = new IsolationPenaltyEvaluator();
    }

    /// <summary>
    /// מחשב ציון כולל להקצאה על בסיס כלל רכיבי הניקוד.
    /// </summary>
    /// <param name="assignment">ההקצאה לניקוד.</param>
    /// <param name="participants">כלל המשתתפים כולל העדפותיהם.</param>
    /// <param name="weights">משקלות לרכיבי הניקוד.</param>
    /// <returns>ציון ההקצאה הכולל.</returns>
    /// <exception cref="ArgumentNullException">נזרק כאשר אחד מהפרמטרים הוא null.</exception>
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

        var socialScore = _socialConnectionScorer.Calculate(
            assignment,
            participants,
            weights.SocialPreferenceWeight);

        var isolationPenalty = _isolationPenaltyEvaluator.Evaluate(
            assignment,
            participants,
            penaltyPerIsolatedParticipant: 1.0,
            weight: weights.IsolationPenaltyWeight);

        var total = socialScore.Value - isolationPenalty.Value;
        return new Score(total);
    }
}
