using System.Text.Json;
using MyProject.Core.Domain.Entities;

namespace MyProject.API.Placement;

/// <summary>
/// סריאליזציה של קבוצות חלוקה ל-JSON במסד.
/// </summary>
internal static class PlacementGroupSerialization
{
    public static string SerializeGroups(Assignment assignment)
    {
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        var groups = assignment.Groups
            .OrderBy(group => group.Id.Value)
            .Select(group => new StoredPlacementGroup
            {
                GroupId = group.Id.Value,
                ParticipantIds = group.ParticipantIds
                    .Select(participantId => participantId.Value)
                    .ToList(),
            })
            .ToList();

        return JsonSerializer.Serialize(groups);
    }

    private sealed class StoredPlacementGroup
    {
        public int GroupId { get; set; }

        public List<string> ParticipantIds { get; set; } = new();
    }
}
