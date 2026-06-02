using MyProject.BL.Algorithm.InitialPlacement.Solver.Models;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.InitialPlacement.Solver;

/// <summary>
/// תפקיד: ממפה תשובת פותר (שיוך יחידות/משתתפים לקבוצות) לישות <see cref="Assignment"/> פנימית.
/// </summary>
/// <remarks>נוצר ע"י <see cref="ExternalSolverFallback"/>.</remarks>
public sealed class SolverResultMapper
{
    /// <summary>
    /// תפקיד: ממפה תשובת פותר ומחזיר מעטפת תוצאה.
    /// </summary>
    /// <param name="request">הבקשה המקורית — לולידation מול התשובה.</param>
    /// <param name="response">תשובת הפותר.</param>
    /// <returns>מעטפת הצלחה/כישלון.</returns>
    /// <remarks>אין שימושים בייצור — <see cref="TryMapAssignment"/> נקרא ישירות.</remarks>
    public SolverResultMapResult Map(SolverRequest request, SolverResponse response)
    {
        TryMapAssignment(request, response, out var assignment, out var errors);
        // assignment is not null — בדיקת null-forgiving אחרי TryMapAssignment.
        return assignment is not null
            ? SolverResultMapResult.Success(assignment)
            : SolverResultMapResult.Failure(errors);
    }

    /// <summary>
    /// תפקיד: ממפה תשובת פותר לחלוקה; תומך גם בשיוך לפי משתתפים (תאימות לאחור).
    /// </summary>
    /// <param name="request">הבקשה המקורית.</param>
    /// <param name="response">תשובת הפותר.</param>
    /// <param name="assignment">החלוקה הממופה; null בכישלון.</param>
    /// <param name="errors">שגיאות מיפוי; ריקה בהצלחה.</param>
    /// <returns>true אם המיפוי הצליח.</returns>
    /// <remarks>נקרא מ- <see cref="ExternalSolverFallback.TrySolve"/>.</remarks>
    public bool TryMapAssignment(
        SolverRequest request,
        SolverResponse response,
        out Assignment? assignment,
        out IReadOnlyList<string> errors)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        assignment = null;
        var errorList = new List<string>();

        if (response.Status != SolverResponseStatus.Success)
        {
            errorList.Add("Solver response status is not Success.");
            if (response.Errors.Count > 0)
            {
                errorList.AddRange(response.Errors);
            }

            errors = errorList;
            return false;
        }

        if (request.PlacementUnits.Count == 0)
        {
            errors = new[] { "Solver request does not include placement units." };
            return false;
        }

        // validGroupIds — קבוצות מותרות לפי הבקשה המקורית.
        var validGroupIds = request.Groups.Select(group => group.GroupId).ToHashSet();
        if (validGroupIds.Count == 0)
        {
            errors = new[] { "Solver request does not include target groups." };
            return false;
        }

        // מנסים לקבל שיוך יחידה→קבוצה; אם אין — נגזר ממשתתפים (תאימות לאחור).
        if (!TryResolveUnitGroupAssignments(request, response, out var unitGroupAssignments, errorList))
        {
            errors = errorList;
            return false;
        }

        if (unitGroupAssignments.Count != request.PlacementUnits.Count)
        {
            errorList.Add(
                $"Solver response assigned {unitGroupAssignments.Count} units, but {request.PlacementUnits.Count} were expected.");
        }

        var assignedUnits = new HashSet<int>();
        // groupedParticipants — אוסף משתתפים לפי groupId לפני בניית Assignment.
        var groupedParticipants = new Dictionary<int, List<ParticipantId>>();
        var coveredParticipants = new HashSet<string>(StringComparer.Ordinal);

        foreach (var unit in request.PlacementUnits)
        {
            if (!unitGroupAssignments.TryGetValue(unit.UnitId, out var groupId))
            {
                errorList.Add($"Placement unit {unit.UnitId} was not assigned to any group.");
                continue;
            }

            if (!assignedUnits.Add(unit.UnitId))
            {
                errorList.Add($"Placement unit {unit.UnitId} appears more than once in solver response.");
                continue;
            }

            if (!validGroupIds.Contains(groupId))
            {
                errorList.Add($"Placement unit {unit.UnitId} was assigned to unknown group {groupId}.");
                continue;
            }

            if (!groupedParticipants.TryGetValue(groupId, out var groupMembers))
            {
                groupMembers = new List<ParticipantId>();
                groupedParticipants[groupId] = groupMembers;
            }

            foreach (var participantId in unit.ParticipantIds)
            {
                if (!coveredParticipants.Add(participantId))
                {
                    errorList.Add($"Participant '{participantId}' appears more than once in solver response.");
                    continue;
                }

                groupMembers.Add(new ParticipantId(participantId));
            }
        }

        var expectedParticipants = request.PlacementUnits
            .SelectMany(unit => unit.ParticipantIds)
            .ToHashSet(StringComparer.Ordinal);

        // Except — משתתפים שציפינו לראות אך לא הופיעו בתשובה.
        foreach (var missingParticipant in expectedParticipants.Except(coveredParticipants))
        {
            errorList.Add($"Participant '{missingParticipant}' is missing from solver response.");
        }

        if (errorList.Count > 0)
        {
            errors = errorList;
            return false;
        }

        try
        {
            // OrderBy על groupId — סדר יציב לקבוצות ב-Assignment.
            var groups = groupedParticipants
                .OrderBy(entry => entry.Key)
                .Select(entry => new Group(new GroupId(entry.Key), entry.Value))
                .ToList();

            assignment = new Assignment(groups);
            errors = Array.Empty<string>();
            return true;
        }
        catch (Exception ex)
        {
            errors = new[] { $"Failed to map solver response to assignment: {ex.Message}" };
            return false;
        }
    }

    private static bool TryResolveUnitGroupAssignments(
        SolverRequest request,
        SolverResponse response,
        out IReadOnlyDictionary<int, int> unitGroupAssignments,
        List<string> errors)
    {
        // נתיב מועדף: שיוך ישיר יחידה→קבוצה מהפותר.
        if (response.UnitGroupAssignments.Count > 0)
        {
            unitGroupAssignments = response.UnitGroupAssignments;
            return true;
        }

        if (response.ParticipantGroupAssignments.Count == 0)
        {
            errors.Add("Solver response does not include unit or participant assignments.");
            unitGroupAssignments = new Dictionary<int, int>();
            return false;
        }

        // נתיב גיבוי: כל חברי יחידה חייבים לקבל אותה קבוצה.
        var derived = new Dictionary<int, int>();

        foreach (var unit in request.PlacementUnits)
        {
            int? groupId = null;

            foreach (var participantId in unit.ParticipantIds)
            {
                if (!response.ParticipantGroupAssignments.TryGetValue(participantId, out var participantGroupId))
                {
                    errors.Add($"Participant '{participantId}' in unit {unit.UnitId} is not assigned.");
                    continue;
                }

                if (groupId is null)
                {
                    groupId = participantGroupId;
                    continue;
                }

                // יחידת חובה שפוצלה — סתירה למשמעות "יחידה אטומית".
                if (groupId.Value != participantGroupId)
                {
                    errors.Add(
                        $"Placement unit {unit.UnitId} was split across groups {groupId.Value} and {participantGroupId}.");
                }
            }

            if (groupId.HasValue)
            {
                derived[unit.UnitId] = groupId.Value;
            }
        }

        if (errors.Count > 0)
        {
            unitGroupAssignments = new Dictionary<int, int>();
            return false;
        }

        unitGroupAssignments = derived;
        return true;
    }
}
