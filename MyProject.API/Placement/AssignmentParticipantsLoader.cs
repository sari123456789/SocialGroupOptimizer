using Microsoft.EntityFrameworkCore;
using MyProject.Data;

namespace MyProject.API.Placement;

public sealed class AssignmentParticipantsLoader
{
    private readonly ApplicationDbContext _db;
    private readonly AssignmentPlacementLoader _assignmentLoader;

    public AssignmentParticipantsLoader(
        ApplicationDbContext db,
        AssignmentPlacementLoader assignmentLoader)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _assignmentLoader = assignmentLoader ?? throw new ArgumentNullException(nameof(assignmentLoader));
    }

    public async Task<AssignmentParticipantsDto?> GetParticipantsAsync(
        int assignmentId,
        int managerId,
        CancellationToken cancellationToken = default)
    {
        var belongsToManager = await _assignmentLoader.AssignmentBelongsToManagerAsync(
            assignmentId,
            managerId,
            cancellationToken);

        if (!belongsToManager)
        {
            return null;
        }

        var assignment = await _db.Assignments
            .AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.AssignmentId == assignmentId, cancellationToken);

        if (assignment is null)
        {
            return null;
        }

        var participantAssignments = await _db.ParticipantAssignments
            .AsNoTracking()
            .Where(entry => entry.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        if (participantAssignments.Count == 0)
        {
            return new AssignmentParticipantsDto
            {
                AssignmentId = assignment.AssignmentId,
                AssignmentName = assignment.AssignmentName,
            };
        }

        var participantAssignmentIds = participantAssignments
            .Select(entry => entry.ParticipantAssignmentId)
            .ToList();

        var dbParticipantIds = participantAssignments
            .Select(entry => entry.ParticipantId)
            .Distinct()
            .ToList();

        var participants = await _db.Participants
            .AsNoTracking()
            .Where(entry => dbParticipantIds.Contains(entry.ParticipantId))
            .ToDictionaryAsync(entry => entry.ParticipantId, cancellationToken);

        var classificationRows = await _db.ParticipantClassifications
            .AsNoTracking()
            .Where(entry => participantAssignmentIds.Contains(entry.ParticipantAssignmentId))
            .ToListAsync(cancellationToken);

        var dimensions = await _db.ClassificationDimensions
            .AsNoTracking()
            .ToDictionaryAsync(entry => entry.ClassificationDimensionId, cancellationToken);

        var levels = await _db.ClassificationLevels
            .AsNoTracking()
            .ToDictionaryAsync(entry => entry.ClassificationLevelId, cancellationToken);

        var classificationsByParticipantAssignmentId = classificationRows
            .GroupBy(entry => entry.ParticipantAssignmentId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

        var participantItems = new List<ParticipantListItemDto>();

        foreach (var participantAssignment in participantAssignments)
        {
            if (!participants.TryGetValue(participantAssignment.ParticipantId, out var participant))
            {
                continue;
            }

            var classifications = new Dictionary<string, string>(StringComparer.Ordinal);

            if (classificationsByParticipantAssignmentId.TryGetValue(
                    participantAssignment.ParticipantAssignmentId,
                    out var rows))
            {
                foreach (var row in rows)
                {
                    if (!dimensions.TryGetValue(row.ClassificationDimensionId, out var dimension))
                    {
                        continue;
                    }

                    if (!levels.TryGetValue(row.ClassificationLevelId, out var level))
                    {
                        continue;
                    }

                    classifications[dimension.DimensionCode] = level.LevelCode;
                }
            }

            participantItems.Add(new ParticipantListItemDto
            {
                ParticipantId = participant.IsraeliIdentityNumber,
                DisplayName = participant.ParticipantName,
                Classifications = classifications,
            });
        }

        participantItems = participantItems
            .OrderBy(entry => entry.DisplayName ?? entry.ParticipantId, StringComparer.Ordinal)
            .ToList();

        var classificationGroups = participantItems
            .SelectMany(entry => entry.Classifications.Select(classification => new
            {
                classification.Key,
                classification.Value,
            }))
            .GroupBy(entry => new { entry.Key, entry.Value })
            .Select(group => new ClassificationGroupDto
            {
                DimensionCode = group.Key.Key,
                LevelCode = group.Key.Value,
                ParticipantCount = group.Count(),
            })
            .OrderBy(entry => entry.DimensionCode, StringComparer.Ordinal)
            .ThenBy(entry => entry.LevelCode, StringComparer.Ordinal)
            .ToList();

        return new AssignmentParticipantsDto
        {
            AssignmentId = assignment.AssignmentId,
            AssignmentName = assignment.AssignmentName,
            ClassificationGroups = classificationGroups,
            Participants = participantItems,
        };
    }
}
