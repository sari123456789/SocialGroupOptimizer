using MyProject.Core.Domain.ValueObjects;
using MyProject.BL.Algorithm.LocalSearch.Execution;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.LocalSearch.Engine;

/// <summary>
/// תוצאת חיפוש מקומי — מצב סופי, ציונים, מונים וסיבת עצירה.
/// </summary>
public sealed class LocalSearchResult
{
    /// <summary>
    /// יוצר תוצאת חיפוש מקומי.
    /// </summary>
    public LocalSearchResult(
        AssignmentState finalState,
        Score initialScore,
        Score finalScore,
        bool hadImprovement,
        int iterationsExecuted,
        int movesExecuted,
        LocalSearchStopReason stopReason,
        string? detailReason,
        IReadOnlyList<MoveExecutionResult> executedMoves)
    {
        FinalState = finalState ?? throw new ArgumentNullException(nameof(finalState));
        InitialScore = initialScore;
        FinalScore = finalScore;
        HadImprovement = hadImprovement;
        IterationsExecuted = iterationsExecuted;
        MovesExecuted = movesExecuted;
        StopReason = stopReason;
        DetailReason = detailReason;
        ExecutedMoves = executedMoves ?? throw new ArgumentNullException(nameof(executedMoves));
    }

    /// <summary>מצב החלוקה אחרי החיפוש.</summary>
    public AssignmentState FinalState { get; }

    /// <summary>ציון בתחילת החיפוש.</summary>
    public Score InitialScore { get; }

    /// <summary>ציון בסיום החיפוש.</summary>
    public Score FinalScore { get; }

    /// <summary>האם הציון הסופי גבוה מההתחלתי.</summary>
    public bool HadImprovement { get; }

    /// <summary>מספר איטרציות לולאה שבוצעו.</summary>
    public int IterationsExecuted { get; }

    /// <summary>מספר מהלכים שבוצעו בפועל.</summary>
    public int MovesExecuted { get; }

    /// <summary>סיבת עצירה מדויקת.</summary>
    public LocalSearchStopReason StopReason { get; }

    /// <summary>פירוט נוסף — מ-SearchDecision או מהמנוע.</summary>
    public string? DetailReason { get; }

    /// <summary>היסטוריית ביצועי מהלכים.</summary>
    public IReadOnlyList<MoveExecutionResult> ExecutedMoves { get; }
}
