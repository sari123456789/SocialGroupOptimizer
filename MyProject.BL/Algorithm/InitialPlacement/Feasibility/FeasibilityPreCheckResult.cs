namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// תוצאת בדיקת היתכנות מוקדמת.
/// </summary>
/// <remarks>
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
    /// סטטוס בדיקת ההיתכנות.
    /// </summary>
    public FeasibilityPreCheckStatus Status { get; }

    /// <summary>
    /// שגיאות שנמצאו בבדיקות המוקדמות.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// יוצר תוצאה שמאפשרת להמשיך לניסיון בניית חלוקה.
    /// </summary>
    public static FeasibilityPreCheckResult Feasible() =>
        new(FeasibilityPreCheckStatus.Feasible, Array.Empty<string>());

    /// <summary>
    /// יוצר תוצאה שחוסמת המשך, עם רשימת הסתירות שנמצאו.
    /// </summary>
    public static FeasibilityPreCheckResult Infeasible(IReadOnlyList<string> errors) =>
        new(FeasibilityPreCheckStatus.Infeasible, errors);
}
