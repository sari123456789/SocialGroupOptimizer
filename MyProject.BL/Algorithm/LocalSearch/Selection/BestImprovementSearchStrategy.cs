using MyProject.BL.Algorithm.LocalSearch.Evaluation;

namespace MyProject.BL.Algorithm.LocalSearch.Selection;

/// <summary>
/// תפקיד המחלקה: בחירת המהלך הטוב ביותר מתוך תוצאות ההערכה.
/// המחלקה משתתפת בשלב בחירה — אחרי MoveEvaluationBatch.
/// </summary>
public sealed class BestImprovementSearchStrategy : ISearchStrategy
{
    /// <summary>
    /// תפקיד הפונקציה: בוחרת מהלך חוקי, משפר ציון, שלא מחזיר למצב שכבר נבדק.
    /// קלט עיקרי: רשימת תוצאות הערכה.
    /// פלט עיקרי: SearchDecision — עם המהלך הנבחר או סיבת כשלון.
    /// </summary>
    /// <inheritdoc />
    public SearchDecision SelectBest(IReadOnlyList<MoveEvaluationResult> results)
    {
        if (results is null)
        {
            throw new ArgumentNullException(nameof(results));
        }

        if (results.Count == 0)
        {
            return SearchDecision.NoValidMoves("No evaluation results were provided.");
        }

        var hasValid = false;
        var hasImproving = false;
        var hasUnvisitedImproving = false;

        foreach (var result in results)
        {
            if (!result.IsValid)
            {
                continue;
            }

            hasValid = true;

            if (result.ScoreDelta <= 0)
            {
                continue;
            }

            hasImproving = true;

            if (!result.WasAlreadyVisited)
            {
                hasUnvisitedImproving = true;
            }
        }

        if (!hasValid)
        {
            return SearchDecision.NoValidMoves("No valid moves were found among the evaluated candidates.");
        }

        if (!hasImproving)
        {
            return SearchDecision.NoImprovingMoves("No improving moves were found among the valid candidates.");
        }

        if (!hasUnvisitedImproving)
        {
            return SearchDecision.OnlyVisitedMoves("All improving valid moves lead to an already visited state.");
        }

        MoveEvaluationResult? best = null;

        foreach (var result in results)
        {
            if (!result.IsValid || result.ScoreDelta <= 0 || result.WasAlreadyVisited)
            {
                continue;
            }

            if (best is null)
            {
                best = result;
                continue;
            }

            if (result.ScoreDelta > best.ScoreDelta)
            {
                best = result;
                continue;
            }

            if (result.ScoreDelta < best.ScoreDelta)
            {
                continue;
            }

            if (result.Proposal.Priority > best.Proposal.Priority)
            {
                best = result;
            }
        }

        return SearchDecision.Selected(
            best!,
            $"Best improving unvisited move with score delta {best!.ScoreDelta} and priority {best.Proposal.Priority}.");
    }
}
