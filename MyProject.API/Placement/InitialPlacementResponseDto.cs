using MyProject.BL.Algorithm.InitialPlacement.Results;

namespace MyProject.API.Placement;

public sealed class InitialPlacementResponseDto
{
    public string Status { get; set; } = string.Empty;

    public List<PlacementGroupDto> Groups { get; set; } = new();

    public List<string> Errors { get; set; } = new();

    public static InitialPlacementResponseDto FromResult(InitialPlacementResult result)
    {
        var response = new InitialPlacementResponseDto
        {
            Status = result.Status.ToString(),
            Errors = result.Errors.ToList(),
        };

        if (result.Assignment is null)
        {
            return response;
        }

        response.Groups = result.Assignment.Groups
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
