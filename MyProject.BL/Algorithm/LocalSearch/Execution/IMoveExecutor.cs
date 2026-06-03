using MyProject.BL.Algorithm.LocalSearch.Evaluation;
using MyProject.BL.Algorithm.RuntimeState;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.LocalSearch.Execution;

/// <summary>
/// חוזה לביצוע מהלך שנבחר על מצב החלוקה הראשי.
/// </summary>
public interface IMoveExecutor
{
    /// <summary>
    /// מבצע את המהלך שנבחר — מעדכן currentState, ציון, hash ו-visited.
    /// </summary>
    /// <param name="currentState">מצב החלוקה הפעיל — מתעדכן in-place.</param>
    /// <param name="selectedMove">תוצאת הערכה חוקית שנבחרה ע"י SearchStrategy.</param>
    /// <param name="visitedStateTracker">מעקב מצבים שבוקרו — מסמן את המצב החדש.</param>
    /// <returns>תוצאת ביצוע מוצלחת.</returns>
    MoveExecutionResult ExecuteSelectedMove(
        AssignmentState currentState,
        MoveEvaluationResult selectedMove,
        VisitedStateTracker visitedStateTracker);
}
