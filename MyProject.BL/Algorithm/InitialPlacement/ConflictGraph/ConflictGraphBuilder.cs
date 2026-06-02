using MyProject.Core.Domain.Constraints;

namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// תפקיד: בונה גרף קונפליקטים בין יחידות חובה מתוך זוגות איסור.
/// </summary>
/// <remarks>
/// כל יחידת חובה היא צומת; זוג אסור בין שתי יחידות שונות יוצר קשת דו-כיוונית.
/// </remarks>
public static class ConflictGraphBuilder
{
    /// <summary>
    /// תפקיד: בונה ConflictGraph מיחידות חובה וזוגות איסור.
    /// </summary>
    /// <param name="mandatoryUnits">מיפוי יחידות חובה.</param>
    /// <param name="forbiddenPairs">אילוצי זוגות איסור מהדומיין.</param>
    /// <returns>גרף קונפליקטים מוכן לניתוח קושי ולפותר.</returns>
    /// <remarks>
    /// נקרא מ- <see cref="InitialPlacementOrchestrator.Run"/>,
    /// FeasibilityPreChecker, בדיקות _solver_validation.
    /// </remarks>
    public static ConflictGraph Build(
        MandatoryUnitMap mandatoryUnits,
        IReadOnlyList<ForbiddenPairConstraint> forbiddenPairs)
    {
        // כל יחידה = צומת; מתחילים עם HashSet ריק לכל unitId.
        var graph = mandatoryUnits.Units.Keys.ToDictionary(unitId => unitId, _ => new HashSet<int>());

        foreach (var pair in forbiddenPairs)
        {
            // TryGetUnitId — מוצא לאיזו יחידת חובה שייך כל משתתף.
            if (!mandatoryUnits.TryGetUnitId(pair.ParticipantA, out var unitA)
                || !mandatoryUnits.TryGetUnitId(pair.ParticipantB, out var unitB)
                || unitA == unitB)
            {
                // אותה יחידה או משתתף לא נמצא — אין קשת (איסור בתוך יחידה מטופל elsewhere).
                continue;
            }

            // קשת דו-כיוונית בין שתי יחידות שונות.
            graph[unitA].Add(unitB);
            graph[unitB].Add(unitA);
        }

        return new ConflictGraph(graph);
    }
}
