namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// תפקיד: מעטפת תוצאה לבדיקת היתכנות מוקדמת — מפרידה Feasible מ-Infeasible עם רשימת שגיאות.
/// </summary>
/// <remarks>
/// נוצר ע"י <see cref="FeasibilityPreChecker.Check"/>; נצרך ע"י <see cref="InitialPlacementOrchestrator"/>.
/// רשימת השגיאות ריקה כשהסטטוס הוא Feasible.
/// </remarks>
public sealed class FeasibilityPreCheckResult
{
    private FeasibilityPreCheckResult(FeasibilityPreCheckStatus status, IReadOnlyList<string> errors)
    {
        Status = status;
        Errors = errors;
    }

    /// <summary>
    /// תפקיד: סטטוס בדיקת ההיתכנות.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator.Run"/>.</remarks>
    public FeasibilityPreCheckStatus Status { get; }

    /// <summary>
    /// תפקיד: שגיאות שנמצאו; ריקה כש-Feasible.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator.Run"/> — מועבר ל-InfeasiblePreCheck.</remarks>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// תפקיד: יוצר תוצאה שמאפשרת להמשיך לבניית חלוקה.
    /// </summary>
    /// <returns>מעטפת Feasible ללא שגיאות.</returns>
    /// <remarks>נקרא מ- <see cref="FeasibilityPreChecker.Check"/>.</remarks>
    public static FeasibilityPreCheckResult Feasible() =>
        new(FeasibilityPreCheckStatus.Feasible, Array.Empty<string>());

    /// <summary>
    /// תפקיד: יוצר תוצאה שחוסמת המשך עם רשימת סתירות.
    /// </summary>
    /// <param name="errors">הסתירות שנמצאו.</param>
    /// <returns>מעטפת Infeasible.</returns>
    /// <remarks>נקרא מ- <see cref="FeasibilityPreChecker.Check"/>.</remarks>
    public static FeasibilityPreCheckResult Infeasible(IReadOnlyList<string> errors) =>
        new(FeasibilityPreCheckStatus.Infeasible, errors);
}
