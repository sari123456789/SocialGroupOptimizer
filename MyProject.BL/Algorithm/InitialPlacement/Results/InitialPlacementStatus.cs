namespace MyProject.BL.Algorithm.InitialPlacement.Results;

/// <summary>
/// תפקיד: סטטוס תוצאת בניית החלוקה הראשונית — מכסה הצלחה, כישלון בשלבים שונים, ומסלול פותר.
/// </summary>
/// <remarks>נצרך ע"י <see cref="InitialPlacementResult"/> ו-<see cref="InitialPlacementOrchestrator.Run"/>.</remarks>
public enum InitialPlacementStatus
{
    /// <summary>
    /// תפקיד: נבנתה חלוקה חוקית (Greedy/Repair).
    /// </summary>
    /// <remarks>מוחזר מ- <see cref="InitialPlacementOrchestrator.Run"/>.</remarks>
    Success,

    /// <summary>
    /// תפקיד: בדיקת היתכנות מוקדמת נכשלה.
    /// </summary>
    /// <remarks>מוחזר מ- <see cref="InitialPlacementOrchestrator.Run"/>.</remarks>
    InfeasiblePreCheck,

    /// <summary>
    /// תפקיד: הבנייה החמדנית לא הצליחה לשבץ את כל המשתתפים.
    /// </summary>
    /// <remarks>מוחזר מ- <see cref="InitialPlacementOrchestrator.Run"/>.</remarks>
    BuildFailed,

    /// <summary>
    /// תפקיד: Greedy ו-Repair נכשלו — לא נמצאה חלוקה חוקית.
    /// </summary>
    /// <remarks>מוחזר מ- <see cref="InitialPlacementOrchestrator.Run"/>.</remarks>
    RepairFailed,

    /// <summary>
    /// תפקיד: נבנתה חלוקה חוקית במסלול פותר.
    /// </summary>
    /// <remarks>מוחזר מ- <see cref="ExternalSolverFallback.TrySolve"/>.</remarks>
    SuccessViaSolver,

    /// <summary>
    /// תפקיד: מסלול הפותר נכשל.
    /// </summary>
    /// <remarks>מוחזר מ- <see cref="ExternalSolverFallback.TrySolve"/>.</remarks>
    SolverFailed,

    /// <summary>
    /// תפקיד: מסלול הפותר חרג ממגבלת זמן.
    /// </summary>
    /// <remarks>מוחזר מ- <see cref="ExternalSolverFallback.TrySolve"/>.</remarks>
    UnknownTimeout,
}
