namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// סטטוס תוצאת בדיקת היתכנות מוקדמת.
/// </summary>
/// <remarks>
/// הבדיקה הזו לא מוכיחה שיש חלוקה תקינה.
/// היא רק מסננת סתירות ברורות לפני ניסיון בנייה.
/// </remarks>
public enum FeasibilityPreCheckStatus
{
    /// <summary>
    /// לא נמצאה סתירה מוקדמת. מותר לנסות לבנות חלוקה.
    /// </summary>
    Feasible,

    /// <summary>
    /// נמצאה סתירה ברורה. לא מתחילים בניית חלוקה.
    /// </summary>
    Infeasible,
}
