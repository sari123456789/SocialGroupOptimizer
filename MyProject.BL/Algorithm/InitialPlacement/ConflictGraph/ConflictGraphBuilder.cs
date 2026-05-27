using MyProject.Core.Domain.Constraints;

namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// בונה גרף קונפליקטים בין יחידות חובה.
/// </summary>
public static class ConflictGraphBuilder
{
    /// <summary>
    /// בונה גרף קונפליקטים בין יחידות חובה.
    /// </summary>
    /// <remarks>
    /// כל יחידת חובה היא צומת.
    /// זוג אסור בין שתי יחידות יוצר קשת.
    /// </remarks>
    public static ConflictGraph Build(
        MandatoryUnitMap mandatoryUnits,
        IReadOnlyList<ForbiddenPairConstraint> forbiddenPairs)
    {
        var graph = mandatoryUnits.Units.Keys.ToDictionary(unitId => unitId, _ => new HashSet<int>());

        foreach (var pair in forbiddenPairs)
        {
            // מדלגים על זוגות באותה יחידה, או על משתתפים שלא נמצאו בקלט.
            if (!mandatoryUnits.TryGetUnitId(pair.ParticipantA, out var unitA)
                || !mandatoryUnits.TryGetUnitId(pair.ParticipantB, out var unitB)
                || unitA == unitB)
            {
                continue;
            }

            graph[unitA].Add(unitB);
            graph[unitB].Add(unitA);
        }

        return new ConflictGraph(graph);
    }
}
