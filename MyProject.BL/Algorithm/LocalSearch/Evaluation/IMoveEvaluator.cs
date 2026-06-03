using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.LocalSearch.Evaluation;

/// <summary>
/// חוזה להערכת מהלך בודד על עותק זמני של מצב החלוקה.
/// </summary>
/// <remarks>
/// <para>תפקיד: ניסוי move, אימות חוקיות, חישוב ציון — בלי לשנות את המצב הראשי.</para>
/// <para>מימוש: <see cref="MoveEvaluator"/> — נרשם ב-DI כ-Singleton/Scoped לפי Program.cs.</para>
/// <para>נקרא מ-: <see cref="MoveEvaluationBatch"/> (לולאה), מתזמר Local Search עתידי.</para>
/// </remarks>
public interface IMoveEvaluator
{
    /// <summary>
    /// מעריך הצעת move אחת מול מצב החלוקה הנוכחי.
    /// </summary>
    /// <param name="currentState">מצב פעיל — לא משתנה; clone נוצר בתוך המימוש.</param>
    /// <param name="proposal">CandidateProposal — כולל Move + מטא-דאטה ייצור.</param>
    /// <param name="context">משתתפים, אילוצים, משקלות, VisitedTracker אופציונלי.</param>
    /// <returns>MoveEvaluationResult — Invalid או Valid (factory methods).</returns>
    MoveEvaluationResult Evaluate(
        AssignmentState currentState,
        CandidateProposal proposal,
        MoveEvaluationContext context);
}
