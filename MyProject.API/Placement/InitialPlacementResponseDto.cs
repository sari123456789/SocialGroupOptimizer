using MyProject.Core.Domain.Entities;
using MyProject.BL.Algorithm.InitialPlacement.Results;

namespace MyProject.API.Placement;

public sealed class InitialPlacementResponseDto
{
    public string Status { get; set; } = string.Empty;

    public List<PlacementGroupDto> Groups { get; set; } = new();

    public List<string> Errors { get; set; } = new();

    public static InitialPlacementResponseDto FromResult(InitialPlacementResult result) =>
        FromResult(result, result.Assignment);

    /// <summary>
    /// בונה DTO — status/errors מ-result, קבוצות מ-assignmentOverride אם קיים.
    /// </summary>
    public static InitialPlacementResponseDto FromResult(
        InitialPlacementResult result,
        Assignment? assignmentOverride)
    {
        var response = new InitialPlacementResponseDto
        {
            Status = result.Status.ToString(),
            Errors = result.Errors.ToList(),
        };

        var assignment = assignmentOverride ?? result.Assignment;
        if (assignment is null)
        {
            return response;
        }

        response.Groups = assignment.Groups
            .OrderBy(group => group.Id.Value)
            .Select(group => new PlacementGroupDto
            {
                GroupId = group.Id.Value,
                ParticipantIds = group.ParticipantIds
                    .Select(participantId => participantId.Value)
                    .ToList(),
            })
            .ToList();

        return response;
    }
}

public sealed class PlacementGroupDto
{
    public int GroupId { get; set; }

    public List<string> ParticipantIds { get; set; } = new();
}
