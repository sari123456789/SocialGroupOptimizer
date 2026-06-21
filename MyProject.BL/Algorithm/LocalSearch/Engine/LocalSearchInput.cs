using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;
using MyProject.BL.Algorithm.SolutionState;
using MyProject.BL.Logic.Configuration;

namespace MyProject.BL.Algorithm.LocalSearch.Engine;

/// <summary>
/// קלט לחיפוש מקומי — מצב התחלתי, משתתפים, אילוצים ומשקלות.
/// </summary>
public sealed class LocalSearchInput
{
    /// <summary>
    /// יוצר קלט חיפוש מקומי.
    /// </summary>
    public LocalSearchInput(
        AssignmentState initialState, // מצב התחלתי — מתעדכן in-place במהלך החיפוש
        IReadOnlyList<Participant> participants, // כל המשתתפים — ל-RuntimeDataBuilder ולהערכה
        IReadOnlyList<IConstraint> constraints, // אילוצים — מועברים להערכת מועמדים
        ScoringWeights scoringWeights, // משקלות ניקוד — ל
        Score? initialScore = null) // ציון התחלתי לתיעוד — אם 
    {
        InitialState = initialState ?? throw new ArgumentNullException(nameof(initialState));
        Participants = participants ?? throw new ArgumentNullException(nameof(participants));

        if (participants.Count == 0)
        {
            throw new ArgumentException("Participants must not be empty.", nameof(participants));
        }

        Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
        ScoringWeights = scoringWeights ?? throw new ArgumentNullException(nameof(scoringWeights));
        InitialScore = initialScore;
    }

    /// <summary>מצב החלוקה ההתחלתי — מתעדכן in-place במהלך החיפוש.</summary>
    public AssignmentState InitialState { get; }

    /// <summary>כל המשתתפים — ל-RuntimeDataBuilder ולהערכה.</summary>
    public IReadOnlyList<Participant> Participants { get; }

    /// <summary>אילוצים — מועברים להערכת מועמדים.</summary>
    public IReadOnlyList<IConstraint> Constraints { get; }

    /// <summary>משקלות ניקוד — ל-MoveEvaluationContext.</summary>
    public ScoringWeights ScoringWeights { get; }

    /// <summary>ציון התחלתי לתיעוד — אם null, נלקח מ-InitialState.CurrentScore.</summary>
    public Score? InitialScore { get; }
}