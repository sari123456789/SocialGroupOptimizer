using MyProject.BL.Algorithm.InitialPlacement.Solver.Models;
using MyProject.BL.Logic.Configuration;

namespace MyProject.BL.Algorithm.InitialPlacement.Solver;

/// <summary>
/// תפקיד: חוזה לקריאה לשירות הפותר החיצוני (OR-Tools CP-SAT).
/// </summary>
/// <remarks>
/// מיושם ע"י <see cref="ExternalSolverClient"/>; ניתן להחלפה ב-stub לבדיקות.
/// </remarks>
public interface IExternalSolverClient
{
    /// <summary>
    /// תפקיד: שולח בקשת פותר לשירות החיצוני וממתין לתשובה.
    /// </summary>
    /// <param name="request">בקשת הפותר הפנימית.</param>
    /// <param name="settings">הגדרות (כתובת בסיס, זמן קצוב, מרווח polling).</param>
    /// <param name="cancellationToken">אסימון ביטול.</param>
    /// <returns>תשובת הפותר — Success, Infeasible, Failed או timeout.</returns>
    /// <remarks>נקרא מ- <see cref="ExternalSolverFallback.TrySolve"/>.</remarks>
    Task<SolverResponse> SolveAsync(
        SolverRequest request,
        AlgorithmSettings settings,
        CancellationToken cancellationToken = default);
}
