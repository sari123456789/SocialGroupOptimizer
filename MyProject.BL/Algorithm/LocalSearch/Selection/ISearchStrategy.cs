using MyProject.BL.Algorithm.LocalSearch.Evaluation;

namespace MyProject.BL.Algorithm.LocalSearch.Selection;

/// <summary>
/// חוזה לבחירת מהלך מתוך תוצאות הערכה.
/// </summary>
public interface ISearchStrategy
{
    /// <summary>
    /// בוחר את המהלך הטוב ביותר מתוך רשימת תוצאות הערכה.
    /// </summary>
    /// <param name="results">תוצאות מ-MoveEvaluationBatch — בסדר הקלט.</param>
    /// <returns>החלטה — מהלך נבחר או סיבת עצירה.</returns>
    SearchDecision SelectBest(IReadOnlyList<MoveEvaluationResult> results);
}
