using MyProject.Core.Domain.Services;
using MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Models;
using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.BL.Algorithm.LocalSearch.State;
using MyProject.BL.Algorithm.SolutionState;
using MyProject.BL.Logic.Scoring;

namespace MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Evaluation;

/// <summary>
/// מעריך הצעת איזון אחת — מיישם את כל ההעברות על שיבוט, מאמת ומנקד את המצב הסופי בלבד.
/// </summary>
public sealed class GroupRebalanceEvaluator
{
    private readonly IAssignmentValidator _validator;
    private readonly IAssignmentScorer _scorer;

    public GroupRebalanceEvaluator(IAssignmentValidator validator, IAssignmentScorer scorer)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _scorer = scorer ?? throw new ArgumentNullException(nameof(scorer));
    }

    public GroupRebalanceResult Evaluate(
        AssignmentState currentState,
        GroupRebalanceCandidate candidate,
        GroupRebalanceEvaluationContext context)
    {
        if (currentState is null)
        {
            throw new ArgumentNullException(nameof(currentState));
        }

        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var scoreBefore = currentState.CurrentScore;
        // מיישמים את כל ההעברות על עותק — המצב המקורי נשאר ללא שינוי.
        var resultingState = ApplyAllTransfers(currentState, candidate.TransferMoves);
        var resultingAssignment = AssignmentStateConverter.ToAssignment(resultingState);

        if (!_validator.IsValid(resultingAssignment, context.Constraints, out var errors))
        {
            return GroupRebalanceResult.Rejected(candidate, scoreBefore, errors);
        }

        var scoreAfter = _scorer.CalculateScore(
            resultingAssignment,
            context.Participants,
            context.ScoringWeights);

        return GroupRebalanceResult.Accepted(candidate, scoreBefore, scoreAfter, resultingState);
    }

    private static AssignmentState ApplyAllTransfers(
        AssignmentState currentState,
        IReadOnlyList<MoveCandidate> transferMoves)
    {
        var clone = AssignmentStateCloner.Clone(currentState);

        foreach (var move in transferMoves)
        {
            AssignmentStateUpdater.ApplyMoveInPlace(clone, move);
        }

        return clone;
    }
}
