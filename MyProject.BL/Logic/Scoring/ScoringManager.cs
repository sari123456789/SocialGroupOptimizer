using System;
using System.Collections.Generic;
using MyProject.BL.Logic.Configuration;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Logic.Scoring;

/// <summary>
/// תפקיד המחלקה: ניהול חישוב הציון המרכזי של החלוקה.
/// המחלקה משתתפת בשלב הערכת איכות — אחרי בניית חלוקה חוקית.
/// </summary>
/// <remarks>
/// הציון הראשי הוא אחוז מימוש העדפות חברתיות בטווח 0–100.
/// קנס בידוד אינו מופחת מהציון הראשי.
/// </remarks>
public sealed class ScoringManager : IAssignmentScorer
{
    private readonly SocialConnectionScorer _socialConnectionScorer;

    public ScoringManager()
    {
        _socialConnectionScorer = new SocialConnectionScorer();
    }

    /// <summary>
    /// תפקיד הפונקציה: מחשבת את הציון הראשי של החלוקה.
    /// קלט עיקרי: חלוקה, משתתפים עם העדפות, משקלות ניקוד.
    /// פלט עיקרי: אחוז מימוש העדפות חברתיות (0–100).
    /// </summary>
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

        return _socialConnectionScorer.Calculate(
            assignment,
            participants,
            weights.SocialPreferenceWeight);
    }
}
