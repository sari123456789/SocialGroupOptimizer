using MyProject.Core.Domain.ValueObjects;
using MyProject.BL.Algorithm.LocalSearch.Evaluation;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.LocalSearch.Execution;

/// <summary>
/// תוצאת ביצוע מהלך שנבחר — על מצב החלוקה הראשי.
/// </summary>
public sealed class MoveExecutionResult
{
    private MoveExecutionResult(
        MoveEvaluationResult executedMove,
        AssignmentState state,
        Score newScore,
        AssignmentHash newHash,
        string reason)
    {
        ExecutedMove = executedMove ?? throw new ArgumentNullException(nameof(executedMove));
        State = state ?? throw new ArgumentNullException(nameof(state));
        NewScore = newScore;
        NewHash = newHash;
        Reason = reason ?? string.Empty;
        IsSucceeded = true;
    }

    /// <summary>תוצאת ההערכה שבוצעה.</summary>
    public MoveEvaluationResult ExecutedMove { get; }

    /// <summary>מצב החלוקה אחרי הביצוע (אותו currentState שעודכן).</summary>
    public AssignmentState State { get; }

    /// <summary>הציון אחרי המהלך.</summary>
    public Score NewScore { get; }

    /// <summary>Hash של המצב אחרי המהלך.</summary>
    public AssignmentHash NewHash { get; }

    /// <summary>סיכום קצר — לוג ודיבוג.</summary>
    public string Reason { get; }

    /// <summary>האם הביצוע הצליח — תמיד true ב-factory <see cref="Succeeded"/>.</summary>
    public bool IsSucceeded { get; }

    /// <summary>יוצר תוצאת ביצוע מוצלחת.</summary>
    public static MoveExecutionResult Succeeded(
        MoveEvaluationResult executedMove,
        AssignmentState state,
        Score newScore,
        AssignmentHash newHash,
        string reason)
    {
        return new MoveExecutionResult(executedMove, state, newScore, newHash, reason);
    }
}
