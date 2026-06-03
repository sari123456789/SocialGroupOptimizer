using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using MyProject.BL.Algorithm.RuntimeState;
using MyProject.BL.Logic.Configuration;

namespace MyProject.BL.Algorithm.LocalSearch.Evaluation;

/// <summary>
/// הקשר קבוע להערכת מהלכים באיטרציה — נבנה פעם אחת לכל סבב הערכה.
/// </summary>
/// <remarks>
/// <para>תפקיד: מרכז קלט משותף ל-MoveEvaluator — משתתפים, אילוצים, משקלות, מעקב ביקורים.</para>
/// <para>נקרא מ-: מתזמר Local Search עתידי (יצירה), MoveEvaluationBatch.EvaluateAll.</para>
/// <para>לא כולל RuntimeDataSnapshot — יקר לבנות; נבנה מחדש רק אחרי ביצוע move אמיתי.</para>
/// </remarks>
public sealed class MoveEvaluationContext
{
    /// <summary>
    /// יוצר הקשר הערכה read-only.
    /// </summary>
    /// <param name="participants">כל המשתתפים והעדפות — לניקוד.</param>
    /// <param name="constraints">אילוצים לאימות חוקיות.</param>
    /// <param name="scoringWeights">משקלות לחישוב ציון.</param>
    /// <param name="visitedTracker">מעקב מצבים שבוקרו — null לדילוג על בדיקת visited.</param>
    public MoveEvaluationContext(
        IReadOnlyList<Participant> participants,
        IReadOnlyList<IConstraint> constraints,
        ScoringWeights scoringWeights,
        VisitedStateTracker? visitedTracker = null)
    {
        Participants = participants ?? throw new ArgumentNullException(nameof(participants));
        Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
        ScoringWeights = scoringWeights ?? throw new ArgumentNullException(nameof(scoringWeights));
        // פרמטר אופציונלי עם ברירת מחדל null — אפשר לקרוא לבנאי בלי visitedTracker.
        VisitedTracker = visitedTracker;
    }

    /// <summary>כל המשתתפים — מקור ל-IAssignmentScorer.</summary>
    public IReadOnlyList<Participant> Participants { get; }

    /// <summary>אילוצים לאימות — מועברים ל-IAssignmentValidator.</summary>
    public IReadOnlyList<IConstraint> Constraints { get; }

    /// <summary>משקלות ניקוד — לחישוב ציון אחרי מהלך חוקי.</summary>
    public ScoringWeights ScoringWeights { get; }

    /// <summary>עוקב מצבים שבוקרו — אופציונלי; לסימון WasAlreadyVisited בלבד.</summary>
    public VisitedStateTracker? VisitedTracker { get; }
}
