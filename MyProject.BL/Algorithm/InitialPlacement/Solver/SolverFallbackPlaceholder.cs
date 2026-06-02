using MyProject.BL.Algorithm.InitialPlacement.Results;
using MyProject.BL.Logic.Configuration;

namespace MyProject.BL.Algorithm.InitialPlacement.Solver;

/// <summary>
/// תפקיד: מימוש זמני של <see cref="ISolverFallback"/> — מחזיר SolverFailed עד שמחובר פותר אמיתי.
/// </summary>
/// <remarks>
/// מיועד להחלפה ב-<see cref="ExternalSolverFallback"/>; אין שימושים בייצור כרגע.
/// </remarks>
public sealed class SolverFallbackPlaceholder : ISolverFallback
{
    /// <summary>
    /// תפקיד: מחזיר כישלון קבוע — הפותר עדיין לא מומש.
    /// </summary>
    /// <param name="input">קלט ההצבה הראשונית.</param>
    /// <param name="mandatoryUnits">יחידות חובה (לא בשימוש).</param>
    /// <param name="conflictGraph">גרף קונפליקטים (לא בשימוש).</param>
    /// <param name="settings">הגדרות אלגוריתם (לא בשימוש).</param>
    /// <returns>תוצאה עם סטטוס SolverFailed והודעה קבועה.</returns>
    /// <remarks>אין קריאות בייצור — מחלק חלופי ל-<see cref="ExternalSolverFallback"/>.</remarks>
    public InitialPlacementResult TrySolve(
        InitialPlacementInput input,
        MandatoryUnitMap mandatoryUnits,
        ConflictGraph conflictGraph,
        AlgorithmSettings settings)
    {
        _ = input ?? throw new ArgumentNullException(nameof(input));
        _ = mandatoryUnits ?? throw new ArgumentNullException(nameof(mandatoryUnits));
        _ = conflictGraph ?? throw new ArgumentNullException(nameof(conflictGraph));
        _ = settings ?? throw new ArgumentNullException(nameof(settings));

        // placeholder — מחזיר כישלון קבוע עד חיבור ExternalSolverFallback.
        return InitialPlacementResult.SolverFailed(new[] { "Solver fallback is not implemented yet." });
    }
}
