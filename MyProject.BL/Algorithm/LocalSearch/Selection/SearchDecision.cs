using MyProject.BL.Algorithm.LocalSearch.Evaluation;

namespace MyProject.BL.Algorithm.LocalSearch.Selection;

/// <summary>
/// החלטת חיפוש — האם נבחר מהלך ואיזו תוצאת הערכה נבחרה.
/// </summary>
public sealed class SearchDecision
{
    private SearchDecision(
        SearchDecisionStatus status,
        MoveEvaluationResult? selectedResult,
        string reason)
    {
        Status = status;
        SelectedResult = selectedResult;
        Reason = reason ?? string.Empty;
        HasSelectedMove = status == SearchDecisionStatus.MoveSelected;

        if (status == SearchDecisionStatus.MoveSelected && selectedResult is null)
        {
            throw new ArgumentException("Selected result is required when status is MoveSelected.", nameof(selectedResult));
        }

        if (status != SearchDecisionStatus.MoveSelected && selectedResult is not null)
        {
            throw new ArgumentException("Selected result must be null when status is not MoveSelected.", nameof(selectedResult));
        }
    }

    /// <summary>סטטוס ההחלטה.</summary>
    public SearchDecisionStatus Status { get; }

    /// <summary>תוצאת ההערכה שנבחרה — null אם לא נבחר מהלך.</summary>
    public MoveEvaluationResult? SelectedResult { get; }

    /// <summary>סיבה קריאה — לוג ודיבוג.</summary>
    public string Reason { get; }

    /// <summary>האם נבחר מהלך לביצוע.</summary>
    public bool HasSelectedMove { get; }

    /// <summary>נבחר מהלך משפר חוקי שלא בוקר.</summary>
    public static SearchDecision Selected(MoveEvaluationResult result, string reason)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return new SearchDecision(SearchDecisionStatus.MoveSelected, result, reason);
    }

    /// <summary>אין תוצאות חוקיות.</summary>
    public static SearchDecision NoValidMoves(string reason) =>
        new(SearchDecisionStatus.NoValidMoves, null, reason);

    /// <summary>אין תוצאות משפרות.</summary>
    public static SearchDecision NoImprovingMoves(string reason) =>
        new(SearchDecisionStatus.NoImprovingMoves, null, reason);

    /// <summary>כל המשפרים מובילים למצב שכבר בוקר.</summary>
    public static SearchDecision OnlyVisitedMoves(string reason) =>
        new(SearchDecisionStatus.OnlyVisitedMoves, null, reason);
}
