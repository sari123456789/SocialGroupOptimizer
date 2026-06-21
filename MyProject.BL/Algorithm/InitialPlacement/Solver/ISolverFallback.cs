using MyProject.BL.Algorithm.InitialPlacement.Results;
using MyProject.BL.Logic.Configuration;

namespace MyProject.BL.Algorithm.InitialPlacement.Solver;

/// <summary>
/// תפקיד: חוזה למסלול פותר חלופי — מנסה לפתור חלוקה ראשונית כש-Greedy/Repair נכשלו או כאסטרטגיה ראשונה.
/// </summary>
/// <remarks>
/// מיושם ע"י <see cref="ExternalSolverFallback"/>.
/// </remarks>
public interface ISolverFallback
{
    /// <summary>
    /// תפקיד: מנסה לפתור חלוקה ראשונית במסלול הפותר.
    /// </summary>
    /// <param name="input">קלט ההצבה הראשונית.</param>
    /// <param name="mandatoryUnits">יחידות חובה שנבנו.</param>
    /// <param name="conflictGraph">גרף קונפליקטים בין יחידות.</param>
    /// <param name="settings">הגדרות ריצה (זמן קצוב, סף קושי וכו').</param>
    /// <returns>תוצאת הצבה — SuccessViaSolver, SolverFailed או UnknownTimeout.</returns>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator"/> — במסלול SolverFirst וב-fallback לאחר כישלון Greedy/Repair.</remarks>
    InitialPlacementResult TrySolve(
        InitialPlacementInput input,
        MandatoryUnitMap mandatoryUnits,
        ConflictGraph conflictGraph,
        AlgorithmSettings settings);
}
