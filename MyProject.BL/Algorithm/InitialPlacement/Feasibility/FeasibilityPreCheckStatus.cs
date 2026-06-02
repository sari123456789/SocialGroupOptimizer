namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// תפקיד: סטטוס תוצאת בדיקת היתכנות מוקדמת — מסנן סתירות ברורות לפני ניסיון בנייה.
/// </summary>
/// <remarks>
/// הבדיקה לא מוכיחה שיש חלוקה תקינה — רק שאין סתירה מובנית.
/// </remarks>
public enum FeasibilityPreCheckStatus
{
    /// <summary>
    /// תפקיד: לא נמצאה סתירה מוקדמת — מותר לנסות לבנות חלוקה.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator.Run"/>.</remarks>
    Feasible,

    /// <summary>
    /// תפקיד: נמצאה סתירה ברורה — לא מתחילים בניית חלוקה.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator.Run"/> — מוביל ל-InfeasiblePreCheck.</remarks>
    Infeasible,
}
