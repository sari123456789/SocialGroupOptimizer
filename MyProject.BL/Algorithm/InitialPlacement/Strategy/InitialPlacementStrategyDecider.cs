using MyProject.BL.Algorithm.InitialPlacement.Difficulty;
using MyProject.BL.Logic.Configuration;

namespace MyProject.BL.Algorithm.InitialPlacement.Strategy;

/// <summary>
/// תפקיד: מחליט האם להתחיל במסלול Greedy או Solver — לפי ציון קושי והגדרות.
/// </summary>
/// <remarks>נוצר ע"י <see cref="InitialPlacementOrchestrator"/>; נקרא ב-Run.</remarks>
public sealed class InitialPlacementStrategyDecider
{
    /// <summary>
    /// תפקיד: בוחר אסטרטגיה לפי ציון קושי וסף ב-AlgorithmSettings.
    /// </summary>
    /// <param name="profile">פרופיל קושי מ-ProblemDifficultyAnalyzer.</param>
    /// <param name="settings">הגדרות — SolverDifficultyThreshold.</param>
    /// <returns>GreedyFirst או SolverFirst.</returns>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator.Run"/>, בדיקות _solver_validation.</remarks>
    public InitialPlacementStrategy Decide(
        ProblemDifficultyProfile profile,
        AlgorithmSettings settings)
    {
        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        // ternary: ציון מעל/שווה לסף → פותר קודם; אחרת Greedy קודם.
        return profile.DifficultyScore >= settings.SolverDifficultyThreshold
            ? InitialPlacementStrategy.SolverFirst
            : InitialPlacementStrategy.GreedyFirst;
    }
}
