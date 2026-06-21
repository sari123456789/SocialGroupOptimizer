using MyProject.BL.Algorithm.LocalSearch.Evaluation;
using MyProject.BL.Algorithm.LocalSearch.State;
using MyProject.BL.Algorithm.RuntimeState;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.LocalSearch.Execution;

/// <summary>
/// תפקיד המחלקה: מבצע מהלך שנבחר על מצב החלוקה הראשי — לא על עותק זמני.
/// המחלקה משתתפת בשלב ביצוע — אחרי בחירת המהלך הטוב ביותר.
/// </summary>
public sealed class MoveExecutor : IMoveExecutor
{
    /// <summary>
    /// תפקיד הפונקציה: מיישם את המהלך על המצב הפעיל ומעדכן ציון, Hash ו-VisitedStateTracker.
    /// קלט עיקרי: מצב נוכחי, תוצאת הערכה של המהלך שנבחר, עוקב מצבים.
    /// פלט עיקרי: MoveExecutionResult — תוצאת ביצוע.
    /// </summary>
    /// <inheritdoc />
    public MoveExecutionResult ExecuteSelectedMove(
        AssignmentState currentState,
        MoveEvaluationResult selectedMove,
        VisitedStateTracker visitedStateTracker)
    {
        if (currentState is null)
        {
            throw new ArgumentNullException(nameof(currentState));
        }

        if (selectedMove is null)
        {
            throw new ArgumentNullException(nameof(selectedMove));
        }

        if (visitedStateTracker is null)
        {
            throw new ArgumentNullException(nameof(visitedStateTracker));
        }

        if (!selectedMove.IsValid)
        {
            throw new ArgumentException("Selected move must be valid.", nameof(selectedMove));
        }

        if (selectedMove.ScoreAfter is null)
        {
            throw new ArgumentException("Selected move must have a score after evaluation.", nameof(selectedMove));
        }

        if (selectedMove.ResultingState is null)
        {
            throw new ArgumentException("Selected move must have a resulting state.", nameof(selectedMove));
        }

        if (selectedMove.WasAlreadyVisited)
        {
            throw new ArgumentException("Selected move must not lead to an already visited state.", nameof(selectedMove));
        }

        if (selectedMove.Proposal is null)
        {
            throw new ArgumentException("Selected move must have a proposal.", nameof(selectedMove));
        }

        var move = selectedMove.Proposal.Move
            ?? throw new ArgumentException("Selected move proposal must contain a move.", nameof(selectedMove));

        // שלב 1: עדכון המיפויים הדו-כיווניים — ParticipantToGroup ו-GroupToParticipants.
        AssignmentStateUpdater.ApplyMoveInPlace(currentState, move);

        var newScore = selectedMove.ScoreAfter.Value;
        currentState.CurrentScore = newScore;

        if (selectedMove.ResultingHash is AssignmentHash expectedHash
            && expectedHash != currentState.CurrentHash)
        {
            throw new InvalidOperationException(
                "Current hash after applying the move does not match the evaluated resulting hash.");
        }

        visitedStateTracker.MarkSeen(currentState.CurrentHash);

        return MoveExecutionResult.Succeeded(
            selectedMove,
            currentState,
            newScore,
            currentState.CurrentHash,
            "Selected move executed successfully.");
    }
}
