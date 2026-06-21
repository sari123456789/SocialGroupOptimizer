using MyProject.Core.Domain.Entities;

namespace MyProject.BL.Algorithm.InitialPlacement.Results;

/// <summary>
/// תפקיד: מעטפת תוצאה אחידה לריצת InitialPlacement — סטטוס, חלוקה (אם קיימת) ושגיאות.
/// </summary>
/// <remarks>
/// מוחזר מ- <see cref="InitialPlacementOrchestrator.Run"/> ו-<see cref="ISolverFallback.TrySolve"/>.
/// כשהסטטוס Success/SuccessViaSolver — <see cref="Assignment"/> מלא ו-<see cref="Errors"/> ריק.
/// </remarks>
public sealed class InitialPlacementResult
{
    private InitialPlacementResult(
        InitialPlacementStatus status,
        Assignment? assignment,
        IReadOnlyList<string> errors)
    {
        Status = status;
        Assignment = assignment;
        Errors = errors;
    }

    /// <summary>
    /// תפקיד: סטטוס הריצה.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator"/>, <see cref="InitialPlacementUserMessages.ForDisplay"/>.</remarks>
    public InitialPlacementStatus Status { get; }

    /// <summary>
    /// תפקיד: החלוקה שנבנתה — מוגדר רק ב-Success/SuccessViaSolver.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator"/>, AssignmentInitialPlacementValidator.</remarks>
    public Assignment? Assignment { get; }

    /// <summary>
    /// תפקיד: שגיאות כשלון; ריקה בהצלחה.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="InitialPlacementUserMessages"/>, AssignmentInitialPlacementValidator.</remarks>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// תפקיד: יוצר תוצאת הצלחה מ-Greedy/Repair.
    /// </summary>
    /// <param name="assignment">החלוקה החוקית.</param>
    /// <returns>מעטפת Success.</returns>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator.Run"/>.</remarks>
    public static InitialPlacementResult Success(Assignment assignment)
    {
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        return new InitialPlacementResult(InitialPlacementStatus.Success, assignment, Array.Empty<string>());
    }

    /// <summary>
    /// תפקיד: יוצר תוצאת כישלון מוקדם — pre-check.
    /// </summary>
    /// <param name="errors">שגיאות שנמצאו.</param>
    /// <returns>מעטפת InfeasiblePreCheck.</returns>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator.Run"/>.</remarks>
    public static InitialPlacementResult InfeasiblePreCheck(IReadOnlyList<string> errors) =>
        new(InitialPlacementStatus.InfeasiblePreCheck, null, errors ?? Array.Empty<string>());

    /// <summary>
    /// תפקיד: יוצר תוצאת כישלון בנייה חמדנית.
    /// </summary>
    /// <param name="errors">שגיאות הבנייה.</param>
    /// <returns>מעטפת BuildFailed.</returns>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator.Run"/>.</remarks>
    public static InitialPlacementResult BuildFailed(IReadOnlyList<string> errors) =>
        new(InitialPlacementStatus.BuildFailed, null, errors ?? Array.Empty<string>());

    /// <summary>
    /// תפקיד: יוצר תוצאת כישלון לאחר Greedy ו-Repair.
    /// </summary>
    /// <param name="errors">שגיאות מעושרות.</param>
    /// <returns>מעטפת RepairFailed.</returns>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator.Run"/>.</remarks>
    public static InitialPlacementResult RepairFailed(IReadOnlyList<string> errors) =>
        new(InitialPlacementStatus.RepairFailed, null, errors ?? Array.Empty<string>());

    /// <summary>
    /// תפקיד: יוצר תוצאת הצלחה ממסלול פותר.
    /// </summary>
    /// <param name="assignment">החלוקה שהוחזרה מהפותר.</param>
    /// <returns>מעטפת SuccessViaSolver.</returns>
    /// <remarks>נקרא מ- <see cref="ExternalSolverFallback.TrySolve"/>.</remarks>
    public static InitialPlacementResult SuccessViaSolver(Assignment assignment)
    {
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        return new InitialPlacementResult(InitialPlacementStatus.SuccessViaSolver, assignment, Array.Empty<string>());
    }

    /// <summary>
    /// תפקיד: יוצר תוצאת כישלון ממסלול פותר.
    /// </summary>
    /// <param name="errors">שגיאות הפותר.</param>
    /// <returns>מעטפת SolverFailed.</returns>
    /// <remarks>נקרא מ- <see cref="ExternalSolverFallback.TrySolve"/>.</remarks>
    public static InitialPlacementResult SolverFailed(IReadOnlyList<string> errors) =>
        new(InitialPlacementStatus.SolverFailed, null, errors ?? Array.Empty<string>());

    /// <summary>
    /// תפקיד: יוצר תוצאת timeout ממסלול פותר.
    /// </summary>
    /// <param name="errors">שגיאות timeout.</param>
    /// <returns>מעטפת UnknownTimeout.</returns>
    /// <remarks>נקרא מ- <see cref="ExternalSolverFallback.TrySolve"/>.</remarks>
    public static InitialPlacementResult UnknownTimeout(IReadOnlyList<string> errors) =>
        new(InitialPlacementStatus.UnknownTimeout, null, errors ?? Array.Empty<string>());
}