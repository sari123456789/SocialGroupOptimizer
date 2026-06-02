using MyProject.Core.Domain.Entities;

namespace MyProject.BL.Algorithm.InitialPlacement.Solver;

/// <summary>
/// תפקיד: מעטפת תוצאה למיפוי תשובת פותר לישות חלוקה פנימית.
/// </summary>
/// <remarks>
/// נוצר ע"י <see cref="SolverResultMapper.Map"/> / <see cref="SolverResultMapper.TryMapAssignment"/>.
/// </remarks>
public sealed class SolverResultMapResult
{
    private SolverResultMapResult(Assignment? assignment, IReadOnlyList<string> errors)
    {
        Assignment = assignment;
        Errors = errors;
    }

    /// <summary>
    /// תפקיד: מציין האם המיפוי הצליח ויש חלוקה תקינה.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="ExternalSolverFallback.TrySolve"/> דרך <see cref="SolverResultMapper.TryMapAssignment"/>.</remarks>
    public bool IsSuccess => Assignment is not null;

    /// <summary>
    /// תפקיד: החלוקה שנמפתה; null כשהמיפוי נכשל.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="ExternalSolverFallback.TrySolve"/> לאחר מיפוי מוצלח.</remarks>
    public Assignment? Assignment { get; }

    /// <summary>
    /// תפקיד: שגיאות מיפוי; ריקה בהצלחה.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="ExternalSolverFallback.TrySolve"/> — מועבר ל-<see cref="InitialPlacementResult.SolverFailed"/>.</remarks>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// תפקיד: יוצר תוצאת הצלחה עם חלוקה ממופה.
    /// </summary>
    /// <param name="assignment">החלוקה שנבנתה מתשובת הפותר.</param>
    /// <returns>מעטפת הצלחה ללא שגיאות.</returns>
    /// <remarks>נקרא מ- <see cref="SolverResultMapper.Map"/>.</remarks>
    public static SolverResultMapResult Success(Assignment assignment)
    {
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        return new SolverResultMapResult(assignment, Array.Empty<string>());
    }

    /// <summary>
    /// תפקיד: יוצר תוצאת כישלון עם רשימת שגיאות מיפוי.
    /// </summary>
    /// <param name="errors">שגיאות המיפוי; null מומר לרשימה ריקה.</param>
    /// <returns>מעטפת כישלון ללא חלוקה.</returns>
    /// <remarks>נקרא מ- <see cref="SolverResultMapper.Map"/>.</remarks>
    public static SolverResultMapResult Failure(IReadOnlyList<string> errors) =>
        new(null, errors ?? Array.Empty<string>());
}
