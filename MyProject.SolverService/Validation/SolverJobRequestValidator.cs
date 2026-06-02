using MyProject.SolverService.Contracts;

namespace MyProject.SolverService.Validation;

public static class SolverJobRequestValidator
{
    private const int MinTimeoutMs = 1;
    private const int MaxTimeoutMs = 300_000;

    public static List<string> Validate(SolverJobRequest? request)
    {
        var errors = new List<string>();
        if (request is null)
        {
            errors.Add("Request body is required.");
            return errors;
        }

        if (request.TimeoutMs < MinTimeoutMs || request.TimeoutMs > MaxTimeoutMs)
        {
            errors.Add($"timeoutMs must be between {MinTimeoutMs} and {MaxTimeoutMs}.");
        }

        if (request.PlacementUnits.Count == 0)
        {
            errors.Add("placementUnits must not be empty.");
        }

        if (request.Groups.Count == 0)
        {
            errors.Add("groups must not be empty.");
        }

        var unitIds = new HashSet<int>();
        foreach (var unit in request.PlacementUnits)
        {
            if (!unitIds.Add(unit.UnitId))
            {
                errors.Add($"Duplicate unitId {unit.UnitId}.");
            }

            if (unit.ParticipantIds.Count == 0)
            {
                errors.Add($"Placement unit {unit.UnitId} is empty.");
            }

            if (unit.Kind == PlacementUnitKindWire.MandatoryUnit && unit.ParticipantIds.Count == 1)
            {
                errors.Add($"Placement unit {unit.UnitId} is MandatoryUnit but has only one participant.");
            }

            if (unit.ParticipantIds.Distinct(StringComparer.Ordinal).Count() != unit.ParticipantIds.Count)
            {
                errors.Add($"Placement unit {unit.UnitId} contains duplicate participant ids.");
            }
        }

        var groupIds = new HashSet<int>();
        var totalMaxCapacity = 0;
        var totalParticipants = request.PlacementUnits.Sum(u => u.ParticipantIds.Count);

        foreach (var group in request.Groups)
        {
            if (!groupIds.Add(group.GroupId))
            {
                errors.Add($"Duplicate groupId {group.GroupId}.");
            }

            if (group.MinSize < 0)
            {
                errors.Add($"Group {group.GroupId} minSize cannot be negative.");
            }

            if (group.MinSize > group.MaxSize)
            {
                errors.Add($"Group {group.GroupId} minSize cannot exceed maxSize.");
            }

            totalMaxCapacity += group.MaxSize;
        }

        if (totalMaxCapacity < totalParticipants)
        {
            errors.Add(
                $"Total group max capacity ({totalMaxCapacity}) is less than participant count ({totalParticipants}).");
        }

        var coveredParticipants = new HashSet<string>(StringComparer.Ordinal);
        foreach (var unit in request.PlacementUnits)
        {
            foreach (var participantId in unit.ParticipantIds)
            {
                if (string.IsNullOrWhiteSpace(participantId))
                {
                    errors.Add($"Placement unit {unit.UnitId} contains an empty participant id.");
                    continue;
                }

                if (!coveredParticipants.Add(participantId))
                {
                    errors.Add($"Participant '{participantId}' appears in more than one placement unit.");
                }
            }
        }

        foreach (var pair in request.ForbiddenUnitPairs)
        {
            if (!unitIds.Contains(pair.FirstUnitId))
            {
                errors.Add($"Forbidden pair references unknown unit {pair.FirstUnitId}.");
            }

            if (!unitIds.Contains(pair.SecondUnitId))
            {
                errors.Add($"Forbidden pair references unknown unit {pair.SecondUnitId}.");
            }
        }

        return errors;
    }
}
