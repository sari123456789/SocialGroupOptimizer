using MyProject.BL.Algorithm.InitialPlacement.Solver.Models;

namespace MyProject.BL.Algorithm.InitialPlacement.Solver;

/// <summary>
/// תפקיד: מעטפת תוצאה לבניית בקשת פותר — מפרידה בין הצלחה (בקשה תקינה) לכישלון (רשימת שגיאות).
/// </summary>
/// <remarks>
/// נוצר ע"י <see cref="SolverInputBuilder.TryBuild"/>; נצרך ע"י <see cref="ExternalSolverFallback"/>.
/// </remarks>
public sealed class SolverInputBuildResult
{
    private SolverInputBuildResult(SolverRequest? request, IReadOnlyList<string> errors)
    {
        Request = request;
        Errors = errors;
    }

    /// <summary>
    /// תפקיד: מציין האם הבנייה הצליחה ויש בקשת פותר תקינה.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="ExternalSolverFallback.TrySolve"/>, <see cref="SolverInputBuilder.Build"/>.</remarks>
    public bool IsSuccess => Request is not null;

    /// <summary>
    /// תפקיד: הבקשה שנבנתה; null כשהבנייה נכשלה.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="ExternalSolverFallback.TrySolve"/> לאחר בדיקת <see cref="IsSuccess"/>.</remarks>
    public SolverRequest? Request { get; }

    /// <summary>
    /// תפקיד: שגיאות בנייה; ריקה בהצלחה.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="ExternalSolverFallback.TrySolve"/> — מועבר ל-<see cref="InitialPlacementResult.SolverFailed"/>.</remarks>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// תפקיד: יוצר תוצאת הצלחה עם בקשת פותר תקינה.
    /// </summary>
    /// <param name="request">בקשת הפותר שנבנתה.</param>
    /// <returns>מעטפת הצלחה עם הבקשה וללא שגיאות.</returns>
    /// <remarks>נקרא מ- <see cref="SolverInputBuilder.TryBuild"/>.</remarks>
    public static SolverInputBuildResult Success(SolverRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return new SolverInputBuildResult(request, Array.Empty<string>());
    }

    /// <summary>
    /// תפקיד: יוצר תוצאת כישלון עם רשימת שגיאות.
    /// </summary>
    /// <param name="errors">שגיאות הבנייה; null מומר לרשימה ריקה.</param>
    /// <returns>מעטפת כישלון ללא בקשה.</returns>
    /// <remarks>נקרא מ- <see cref="SolverInputBuilder.TryBuild"/>.</remarks>
    public static SolverInputBuildResult Failure(IReadOnlyList<string> errors) =>
        new(null, errors ?? Array.Empty<string>());
}
