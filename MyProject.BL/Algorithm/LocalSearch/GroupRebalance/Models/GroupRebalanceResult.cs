using MyProject.BL.Algorithm.SolutionState;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Models;

/// <summary>
/// תוצאת הערכה להצעת איזון בודדת — חוקיות, שיפור בציון ומצב מתקבל.
/// </summary>
public sealed class GroupRebalanceResult
{
    private GroupRebalanceResult(
        bool isValid,
        bool isImproving,
        GroupRebalanceCandidate? candidate,
        AssignmentState? resultingState,
        AssignmentHash? resultingHash,
        Score scoreBefore,
        Score? scoreAfter,
        double scoreDelta,
        IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        IsImproving = isImproving;
        Candidate = candidate;
        ResultingState = resultingState;
        ResultingHash = resultingHash;
        ScoreBefore = scoreBefore;
        ScoreAfter = scoreAfter;
        ScoreDelta = scoreDelta;
        Errors = errors ?? Array.Empty<string>();
    }

    public bool IsValid { get; }

    public bool IsImproving { get; }

    public GroupRebalanceCandidate? Candidate { get; }

    public AssignmentState? ResultingState { get; }

    public AssignmentHash? ResultingHash { get; }

    public Score ScoreBefore { get; }

    public Score? ScoreAfter { get; }

    public double ScoreDelta { get; }

    public IReadOnlyList<string> Errors { get; }

    public static GroupRebalanceResult Rejected(
        GroupRebalanceCandidate candidate,
        Score scoreBefore,
        IReadOnlyList<string> errors) =>
        new(
            isValid: false,
            isImproving: false,
            candidate,
            resultingState: null,
            resultingHash: null,
            scoreBefore,
            scoreAfter: null,
            scoreDelta: 0,
            errors);

    public static GroupRebalanceResult Accepted(
        GroupRebalanceCandidate candidate,
        Score scoreBefore,
        Score scoreAfter,
        AssignmentState resultingState) =>
        new(
            isValid: true,
            isImproving: scoreAfter.Value > scoreBefore.Value,
            candidate,
            resultingState,
            resultingHash: resultingState.CurrentHash,
            scoreBefore,
            scoreAfter,
            scoreDelta: scoreAfter.Value - scoreBefore.Value,
            errors: Array.Empty<string>());
}
