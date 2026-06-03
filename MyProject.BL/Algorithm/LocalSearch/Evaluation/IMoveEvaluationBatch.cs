using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.LocalSearch.Evaluation;

/// <summary>
/// חוזה להערכת אוסף מועמדי move — לולאה על IMoveEvaluator.
/// </summary>
/// <remarks>
/// <para>תפקיד: מריץ Evaluate על כל CandidateProposal; לא בוחר move ולא מבצע move.</para>
/// <para>מימוש: <see cref="MoveEvaluationBatch"/> — מגביל עד CandidateCount הערכות (הגנה).</para>
/// <para>נקרא מ-: מתזמר Local Search עתידי, אחרי SwapMoveGenerator.Generate.</para>
/// </remarks>
public interface IMoveEvaluationBatch
{
    /// <summary>
    /// מעריך את כל המועמדים ברשימה — בסדר הקלט, עד מגבלת CandidateCount הגנתית.
    /// </summary>
    /// <param name="currentState">מצב החלוקה הנוכחי — לא משתנה.</param>
    /// <param name="proposals">לרוב הפלט מ-SwapMoveGenerator (כבר ממוין ומוגבל).</param>
    /// <param name="context">אותו הקשר לכל ההערכות באיטרציה.</param>
    /// <returns>רשימה באורך ≤ proposals.Count; כל תוצאה תואמת proposal באותו אינדקס (עד המגבלה).</returns>
    IReadOnlyList<MoveEvaluationResult> EvaluateAll(
        AssignmentState currentState,
        IReadOnlyList<CandidateProposal> proposals,
        MoveEvaluationContext context);
}
