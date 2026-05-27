using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// בונה יחידות חובה מזוגות חובה.
/// </summary>
public static class MandatoryGroupBuilder
{
    /// <summary>
    /// בונה יחידות חובה לפי קשרים מחוברים.
    /// </summary>
    /// <remarks>
    /// אם א חייב להיות עם ב, וב חייב להיות עם ג,
    /// אז א, ב וג הופכים ליחידת חובה אחת.
    /// </remarks>
    public static MandatoryUnitMap Build(
        InitialPlacementInput input,
        IReadOnlyList<MandatoryPairConstraint> mandatoryPairs)
    {
        // Union-Find: כל משתתף מתחיל כקבוצה נפרדת.
        var parentByParticipant = input.Participants
            .Select(participant => participant.Id)
            .Distinct()
            .ToDictionary(id => id, id => id);

        // כל זוג חובה מאחד שני משתתפים לאותה יחידה.
        foreach (var pair in mandatoryPairs)
        {
            EnsureParticipant(parentByParticipant, pair.ParticipantA);
            EnsureParticipant(parentByParticipant, pair.ParticipantB);
            Union(parentByParticipant, pair.ParticipantA, pair.ParticipantB);
        }

        // המרה מקבוצות Union-Find למספרי יחידות רציפים.
        var unitIdByParticipant = new Dictionary<ParticipantId, int>();
        var units = new Dictionary<int, List<ParticipantId>>();
        var rootToUnitId = new Dictionary<ParticipantId, int>();

        foreach (var participantId in parentByParticipant.Keys.ToList())
        {
            var root = Find(parentByParticipant, participantId);
            if (!rootToUnitId.TryGetValue(root, out var unitId))
            {
                unitId = rootToUnitId.Count;
                rootToUnitId[root] = unitId;
                units[unitId] = new List<ParticipantId>();
            }

            unitIdByParticipant[participantId] = unitId;
            units[unitId].Add(participantId);
        }

        return new MandatoryUnitMap(unitIdByParticipant, units);
    }

    /// <summary>
    /// מוסיף משתתף למבנה Union-Find אם הוא עדיין לא קיים.
    /// </summary>
    /// <remarks>
    /// נדרש כשזוג חובה מזכיר משתתף שלא הופיע ברשימת המשתתפים הראשית.
    /// </remarks>
    private static void EnsureParticipant(IDictionary<ParticipantId, ParticipantId> parentByParticipant, ParticipantId participantId)
    {
        if (!parentByParticipant.ContainsKey(participantId))
        {
            parentByParticipant[participantId] = participantId;
        }
    }

    /// <summary>
    /// מוצא את שורש הקבוצה של משתתף, עם דחיסת נתיב.
    /// </summary>
    private static ParticipantId Find(IDictionary<ParticipantId, ParticipantId> parentByParticipant, ParticipantId participantId)
    {
        var parent = parentByParticipant[participantId];
        if (parent == participantId)
        {
            return participantId;
        }

        var root = Find(parentByParticipant, parent);
        parentByParticipant[participantId] = root;
        return root;
    }

    /// <summary>
    /// מאחד שתי קבוצות משתתפים לאותה יחידת חובה.
    /// </summary>
    private static void Union(
        IDictionary<ParticipantId, ParticipantId> parentByParticipant,
        ParticipantId firstParticipantId,
        ParticipantId secondParticipantId)
    {
        var firstRoot = Find(parentByParticipant, firstParticipantId);
        var secondRoot = Find(parentByParticipant, secondParticipantId);

        if (firstRoot != secondRoot)
        {
            parentByParticipant[secondRoot] = firstRoot;
        }
    }
}
