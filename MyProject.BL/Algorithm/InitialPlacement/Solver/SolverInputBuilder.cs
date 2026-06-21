using MyProject.BL.Algorithm.InitialPlacement.Solver.Models;
using MyProject.BL.Logic.Configuration;
using MyProject.Core.Domain.Constraints;

namespace MyProject.BL.Algorithm.InitialPlacement.Solver;

/// <summary>
/// תפקיד: מתרגם קלט דומיין (משתתפים, יחידות חובה, גרף קונפליקטים, אילוצים) לבקשת פותר פנימית.
/// </summary>
/// <remarks>נוצר ע"י <see cref="ExternalSolverFallback"/>; נקרא גם מבדיקות _solver_validation.</remarks>
public sealed class SolverInputBuilder
{
    /// <summary>
    /// תפקיד: בונה בקשת פותר; זורק חריגה אם הבנייה נכשלה.
    /// </summary>
    /// <param name="input">קלט ההצבה הראשונית.</param>
    /// <param name="mandatoryUnits">יחידות חובה.</param>
    /// <param name="conflictGraph">גרף קונפליקטים.</param>
    /// <param name="settings">הגדרות (לא בשימוש כרגע).</param>
    /// <returns>בקשת פותר תקינה.</returns>
    /// <remarks>נקרא מבדיקות _solver_validation; בייצור — <see cref="TryBuild"/>.</remarks>
    public SolverRequest Build(
        InitialPlacementInput input,
        MandatoryUnitMap mandatoryUnits,
        ConflictGraph conflictGraph,
        AlgorithmSettings? settings = null)
    {
        // מנסים לבנות בקשה; TryBuild מחזיר מעטפת במקום לזרוק שגיאות מפורטות.
        var result = TryBuild(input, mandatoryUnits, conflictGraph, settings);
        if (result.IsSuccess)
        {
            // Request! — null-forgiving: אחרי IsSuccess==true הבקשה קיימת.
            return result.Request!;
        }

        // כישלון: מאחדים את כל השגיאות לחריגה אחת.
        throw new InvalidOperationException(string.Join(" ", result.Errors));
    }

    /// <summary>
    /// תפקיד: מנסה לבנות בקשת פותר ומחזיר שגיאות מפורטות במקרה כישלון.
    /// </summary>
    /// <param name="input">קלט ההצבה הראשונית.</param>
    /// <param name="mandatoryUnits">יחידות חובה.</param>
    /// <param name="conflictGraph">גרף קונפליקטים.</param>
    /// <param name="settings">הגדרות (לא בשימוש כרגע).</param>
    /// <returns>מעטפת הצלחה/כישלון עם <see cref="SolverInputBuildResult"/>.</returns>
    /// <remarks>נקרא מ- <see cref="ExternalSolverFallback.TrySolve"/>, <see cref="Build"/>.</remarks>
    public SolverInputBuildResult TryBuild(
        InitialPlacementInput input,
        MandatoryUnitMap mandatoryUnits,
        ConflictGraph conflictGraph,
        AlgorithmSettings? settings = null)
    {

        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (mandatoryUnits is null)
        {
            throw new ArgumentNullException(nameof(mandatoryUnits));
        }

        if (conflictGraph is null)
        {
            throw new ArgumentNullException(nameof(conflictGraph));
        }

        // אוספים שגיאות מכל שלב בנייה — לא עוצרים בשלב הראשון כדי לדווח על כמה בעיות.
        var errors = new List<string>();

        // שלב 1: יחידות שיבוץ (חובה + בודדים).
        if (!TryBuildPlacementUnits(input, mandatoryUnits, out var placementUnits, out var placementErrors))
        {
            errors.AddRange(placementErrors);
        }

        // שלב 2: הגדרת קבוצות יעד וקיבולות.
        if (!TryBuildGroups(input, input.Participants.Count, out var groups, out var groupErrors))
        {
            errors.AddRange(groupErrors);
        }

        if (errors.Count > 0)
        {
            return SolverInputBuildResult.Failure(errors);
        }

        // שלב 3: זוגות יחידות אסורים מתוך גרף הקונפליקטים.
        var forbiddenUnitPairs = BuildForbiddenUnitPairs(conflictGraph, placementUnits!, errors);
        if (errors.Count > 0)
        {
            return SolverInputBuildResult.Failure(errors);
        }

        // שלב 4: משתתפים וסיווגים + אילוצי סיווג לפותר.
        var participants = BuildParticipants(input);
        var classificationConstraints = BuildClassificationConstraints(input);

        // מספר הקבוצות המינימלי מאפשר לפותר להשתמש בפחות קבוצות מהמקסימום.
        var minGroups = input.Constraints
            .OfType<GroupCountConstraint>()
            .FirstOrDefault()?.MinGroups ?? 0;

        return SolverInputBuildResult.Success(new SolverRequest(
            participants,
            placementUnits!,
            groups!,
            forbiddenUnitPairs,
            classificationConstraints,
            minGroups));
    }

    private static bool TryBuildPlacementUnits(
        InitialPlacementInput input,
        MandatoryUnitMap mandatoryUnits,
        out IReadOnlyList<PlacementUnitRequest>? placementUnits,
        out IReadOnlyList<string> errors)
    {
        var errorList = new List<string>();
        var units = new List<PlacementUnitRequest>();
        // עוקב אחר משתתפים שכבר שובצו ליחידה — כל משתתף חייב להופיע בדיוק פעם אחת.
        var coveredParticipants = new HashSet<string>(StringComparer.Ordinal);
        var inputParticipantIds = input.Participants
            .Select(participant => participant.Id.Value)
            .ToHashSet(StringComparer.Ordinal);

        if (mandatoryUnits.Units.Count == 0)
        {
            placementUnits = null;
            errors = new[] { "Mandatory unit map is empty." };
            return false;
        }

        // OrderBy על מפתח — סדר יציב ליחידות בבקשה לפותר.
        foreach (var unit in mandatoryUnits.Units.OrderBy(entry => entry.Key))
        {
            var memberIds = unit.Value
                .Select(participantId => participantId.Value)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            foreach (var memberId in memberIds)
            {
                if (!inputParticipantIds.Contains(memberId))
                {
                    errorList.Add($"Placement unit {unit.Key} references unknown participant '{memberId}'.");
                    continue;
                }

                // Add מחזיר false אם המשתתף כבר ב-coveredParticipants — כפילות בין יחידות.
                if (!coveredParticipants.Add(memberId))
                {
                    errorList.Add($"Participant '{memberId}' appears in more than one placement unit.");
                }
            }

            if (memberIds.Count == 0)
            {
                errorList.Add($"Placement unit {unit.Key} is empty.");
                continue;
            }

            // יותר ממשתתף אחד = יחידת חובה; אחרת יחידת בודד.
            var kind = memberIds.Count > 1
                ? PlacementUnitKind.MandatoryUnit
                : PlacementUnitKind.SingleParticipant;

            try
            {
                units.Add(new PlacementUnitRequest(unit.Key, memberIds, kind));
            }
            catch (Exception ex)
            {
                errorList.Add($"Placement unit {unit.Key} is invalid: {ex.Message}");
            }
        }

        // משתתפים שלא הופיעו באף יחידה — חסר כיסוי מלא.
        foreach (var participantId in inputParticipantIds)
        {
            if (!coveredParticipants.Contains(participantId))
            {
                errorList.Add($"Participant '{participantId}' is missing from placement units.");
            }
        }

        if (errorList.Count > 0)
        {
            placementUnits = null;
            errors = errorList;
            return false;
        }

        placementUnits = units;
        errors = Array.Empty<string>();
        return true;
    }

    private static bool TryBuildGroups(
        InitialPlacementInput input,
        int participantCount,
        out IReadOnlyList<SolverGroupRequest>? groups,
        out IReadOnlyList<string> errors)
    {
        var errorList = new List<string>();
        // GroupCountConstraint קובע כמה קבוצות; GroupSizeConstraint — גודל לכל קבוצה.
        var groupCountConstraint = input.Constraints.OfType<GroupCountConstraint>().FirstOrDefault();
        var sizeByGroupId = input.Constraints
            .OfType<GroupSizeConstraint>()
            .ToDictionary(constraint => constraint.GroupId.Value, constraint => constraint);

        var groupCount = groupCountConstraint?.MaxGroups ?? 0;
        // אם אין GroupCount — מסיקים ממספר הקבוצות שמוגדרות ב-GroupSizeConstraint.
        if (groupCount == 0 && sizeByGroupId.Count > 0)
        {
            groupCount = sizeByGroupId.Keys.Max();
        }

        if (groupCount <= 0)
        {
            groups = null;
            errors = new[] { "Group count is missing. Provide GroupCountConstraint or GroupSizeConstraint." };
            return false;
        }

        if (groupCountConstraint is not null && groupCountConstraint.MinGroups > groupCount)
        {
            errorList.Add("Group count constraint is inconsistent: MinGroups exceeds MaxGroups.");
        }

        var builtGroups = new List<SolverGroupRequest>();
        var totalMaxCapacity = 0;

        for (var groupId = 1; groupId <= groupCount; groupId++)
        {
            if (sizeByGroupId.TryGetValue(groupId, out var sizeConstraint))
            {
                builtGroups.Add(new SolverGroupRequest(
                    groupId,
                    sizeConstraint.MinSize,
                    sizeConstraint.MaxCapacity.Value));
                totalMaxCapacity += sizeConstraint.MaxCapacity.Value;
                continue;
            }

            // קבוצה ללא GroupSizeConstraint — קיבולת מקסימלית = כל המשתתפים.
            builtGroups.Add(new SolverGroupRequest(groupId, 0, participantCount));
            totalMaxCapacity += participantCount;
        }

        // סכום הקיבולות חייב לכסות את כל המשתתפים.
        if (totalMaxCapacity < participantCount)
        {
            errorList.Add(
                $"Total group capacity ({totalMaxCapacity}) is less than participant count ({participantCount}).");
        }

        if (errorList.Count > 0)
        {
            groups = null;
            errors = errorList;
            return false;
        }

        groups = builtGroups;
        errors = Array.Empty<string>();
        return true;
    }

    private static IReadOnlyList<SolverForbiddenUnitPairRequest> BuildForbiddenUnitPairs(
        ConflictGraph conflictGraph,
        IReadOnlyList<PlacementUnitRequest> placementUnits,
        List<string> errors)
    {
        var validUnitIds = placementUnits.Select(unit => unit.UnitId).ToHashSet();
        var pairs = new List<SolverForbiddenUnitPairRequest>();
        // seenPairs מונע כפילות — הגרף לא מכוון ולכן (a,b)==(b,a).
        var seenPairs = new HashSet<(int, int)>();

        foreach (var adjacency in conflictGraph.Adjacency)
        {
            if (!validUnitIds.Contains(adjacency.Key))
            {
                errors.Add($"Conflict graph references unknown unit id {adjacency.Key}.");
                continue;
            }

            foreach (var neighbor in adjacency.Value)
            {
                if (!validUnitIds.Contains(neighbor))
                {
                    errors.Add($"Conflict graph references unknown unit id {neighbor}.");
                    continue;
                }

                // Math.Min/Max — נרמול לזוג ממוין כדי לא לשכפל קשתות.
                var first = Math.Min(adjacency.Key, neighbor);
                var second = Math.Max(adjacency.Key, neighbor);
                if (first == second || !seenPairs.Add((first, second)))
                {
                    continue;
                }

                pairs.Add(new SolverForbiddenUnitPairRequest(first, second));
            }
        }

        return pairs;
    }

    private static IReadOnlyList<SolverParticipantRequest> BuildParticipants(InitialPlacementInput input) =>
        input.Participants
            .Select(participant => new SolverParticipantRequest(
                participant.Id.Value,
                participant.Classifications.ToDictionary(
                    kv => kv.Key.ToString(),
                    kv => kv.Value.ToString())))
            .ToList();

    private static IReadOnlyList<SolverClassificationConstraintRequest> BuildClassificationConstraints(
        InitialPlacementInput input)
    {
        var requests = new List<SolverClassificationConstraintRequest>();

        foreach (var constraint in input.Constraints)
        {
            // switch על סוג אילוץ — כל case מתרגם ל-DTO אחיד לפותר.
            switch (constraint)
            {
                case ClassificationProportionalBalanceConstraint proportional:
                    requests.Add(new SolverClassificationConstraintRequest(
                        ConstraintKind: nameof(ClassificationProportionalBalanceConstraint),
                        TargetDimension: proportional.TargetDimension.ToString(),
                        TargetLevel: null,
                        MinCountPerGroup: null,
                        MaxCountPerGroup: null,
                        AllowedLevels: proportional.DimensionLevels.Select(level => level.ToString()).ToList(),
                        MaxScaledDeviation: proportional.MaxScaledDeviation));
                    break;

                case ClassificationHomogeneousGroupConstraint homogeneous:
                    requests.Add(new SolverClassificationConstraintRequest(
                        ConstraintKind: nameof(ClassificationHomogeneousGroupConstraint),
                        TargetDimension: homogeneous.TargetDimension.ToString(),
                        TargetLevel: null,
                        MinCountPerGroup: null,
                        MaxCountPerGroup: null,
                        AllowedLevels: homogeneous.DimensionLevels.Select(level => level.ToString()).ToList(),
                        MaxScaledDeviation: null));
                    break;
            }
        }

        return requests;
    }
}
