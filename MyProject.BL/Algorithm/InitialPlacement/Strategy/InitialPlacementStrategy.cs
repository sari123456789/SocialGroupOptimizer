namespace MyProject.BL.Algorithm.InitialPlacement.Strategy;

/// <summary>
/// תפקיד: אסטרטגיית פתיחה לבניית חלוקה ראשונית — Greedy קודם או פותר קודם.
/// </summary>
/// <remarks>נבחר ע"י <see cref="InitialPlacementStrategyDecider"/>; נצרך ע"י <see cref="InitialPlacementOrchestrator"/>.</remarks>
public enum InitialPlacementStrategy
{
    /// <summary>
    /// תפקיד: ניסיון Greedy/Repair לפני מסלול פותר.
    /// </summary>
    /// <remarks>נבחר כשציון הקושי נמוך מסף ההגדרות.</remarks>
    GreedyFirst,

    /// <summary>
    /// תפקיד: מעבר למסלול פותר כבר בתחילת הזרימה.
    /// </summary>
    /// <remarks>נבחר כשציון הקושי גבוה מסף ההגדרות.</remarks>
    SolverFirst,
}
