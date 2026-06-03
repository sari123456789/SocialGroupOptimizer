using MyProject.BL.Algorithm.LocalSearch.Evaluation;
using MyProject.BL.Algorithm.LocalSearch.Execution;
using MyProject.BL.Algorithm.LocalSearch.Generation;
using MyProject.BL.Algorithm.LocalSearch.RuntimeData;
using MyProject.BL.Algorithm.LocalSearch.Selection;
using MyProject.BL.Algorithm.RuntimeState;
using MyProject.BL.Algorithm.SolutionState;
using MyProject.BL.Logic.Configuration;

namespace MyProject.BL.Algorithm.LocalSearch.Engine;

/// <summary>
/// מנוע חיפוש מקומי — מתזמר build, generate, evaluate, select, execute.
/// </summary>
public sealed class LocalSearchEngine : ILocalSearchEngine
{
    private readonly AlgorithmSettings _settings;
    private readonly SwapMoveGenerator _generator;
    private readonly IMoveEvaluationBatch _evaluationBatch;
    private readonly ISearchStrategy _searchStrategy;
    private readonly IMoveExecutor _moveExecutor;

    /// <summary>
    /// יוצר מנוע עם תלויות מוזרקות.
    /// </summary>
    public LocalSearchEngine(
        AlgorithmSettings settings,
        SwapMoveGenerator generator,
        IMoveEvaluationBatch evaluationBatch,
        ISearchStrategy searchStrategy,
        IMoveExecutor moveExecutor)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _evaluationBatch = evaluationBatch ?? throw new ArgumentNullException(nameof(evaluationBatch));
        _searchStrategy = searchStrategy ?? throw new ArgumentNullException(nameof(searchStrategy));
        _moveExecutor = moveExecutor ?? throw new ArgumentNullException(nameof(moveExecutor));
    }

    /// <inheritdoc />
    public LocalSearchResult Improve(LocalSearchInput input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (_settings.MaxIterations <= 0)
        {
            throw new InvalidOperationException("Max iterations must be greater than zero.");
        }

        var currentState = input.InitialState;
        var initialScore = input.InitialScore ?? currentState.CurrentScore;

        var visitedStateTracker = new VisitedStateTracker();
        visitedStateTracker.MarkSeen(currentState.CurrentHash);

        var evaluationContext = new MoveEvaluationContext(
            input.Participants,
            input.Constraints,
            input.ScoringWeights,
            visitedStateTracker);

        var iterationIndex = 0;
        var iterationsSinceImprovement = 0;
        var movesExecuted = 0;
        var executedMoves = new List<MoveExecutionResult>();
        var stopReason = LocalSearchStopReason.MaxIterationsReached;
        string? detailReason = null;

        while (iterationIndex < _settings.MaxIterations)
        {
            var snapshot = RuntimeDataBuilder.Build(currentState, input.Participants);

            var generationContext = new MoveGenerationContext(
                iterationIndex,
                iterationsSinceImprovement,
                snapshot.IsolatedParticipantIds.Count,
                snapshot.WeakGroupIdsByWeakness.Count,
                currentState.ParticipantToGroup.Count,
                currentState.GroupToParticipants.Count);

            var proposals = _generator.Generate(currentState, snapshot, generationContext);

            if (proposals.Count == 0)
            {
                stopReason = LocalSearchStopReason.NoCandidates;
                detailReason = "No candidate moves were generated.";
                break;
            }

            var results = _evaluationBatch.EvaluateAll(currentState, proposals, evaluationContext);
            var decision = _searchStrategy.SelectBest(results);

            if (!decision.HasSelectedMove)
            {
                stopReason = MapStopReason(decision.Status);
                detailReason = decision.Reason;
                break;
            }

            var selectedResult = decision.SelectedResult!;
            var executionResult = _moveExecutor.ExecuteSelectedMove(
                currentState,
                selectedResult,
                visitedStateTracker);

            executedMoves.Add(executionResult);
            movesExecuted++;

            if (selectedResult.ScoreDelta > 0)
            {
                iterationsSinceImprovement = 0;
            }
            else
            {
                iterationsSinceImprovement++;
            }

            iterationIndex++;
        }

        if (detailReason is null)
        {
            detailReason = "Maximum local search iterations reached.";
        }

        return new LocalSearchResult(
            currentState,
            initialScore,
            currentState.CurrentScore,
            currentState.CurrentScore.Value > initialScore.Value,
            iterationIndex,
            movesExecuted,
            stopReason,
            detailReason,
            executedMoves);
    }

    private static LocalSearchStopReason MapStopReason(SearchDecisionStatus status) =>
        status switch
        {
            SearchDecisionStatus.NoValidMoves => LocalSearchStopReason.NoValidMoves,
            SearchDecisionStatus.NoImprovingMoves => LocalSearchStopReason.NoImprovingMoves,
            SearchDecisionStatus.OnlyVisitedMoves => LocalSearchStopReason.OnlyVisitedMoves,
            _ => LocalSearchStopReason.NoValidMoves,
        };
}
