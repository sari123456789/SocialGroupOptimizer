using MyProject.BL.Algorithm.InitialPlacement;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;
using InitialPlacementExecutionContext = MyProject.BL.Algorithm.InitialPlacement.Runtime.ExecutionContext;

namespace MyProject.API.Placement;

public static class InitialPlacementDtoMapper
{
    private static readonly ClassificationDimensionCode DefaultDimension = new("level");
    private static readonly ClassificationLevelCode DefaultLevel = new("default");

    public static InitialPlacementInput ToInput(InitialPlacementRequestDto request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        ValidateRequest(request);

        var participantIds = request.Participants
            .Select(id => id.Trim())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (participantIds.Count != request.Participants.Count)
        {
            throw new ArgumentException("Participants must be unique and non-empty.");
        }

        var knownParticipants = participantIds
            .Select(id => new ParticipantId(id))
            .ToHashSet();

        var participants = participantIds
            .Select(id => new Participant(
                new ParticipantId(id),
                new Dictionary<ClassificationDimensionCode, ClassificationLevelCode>
                {
                    [DefaultDimension] = DefaultLevel,
                },
                Array.Empty<Preference>()))
            .ToList();

        var constraints = new List<IConstraint>
        {
            new GroupCountConstraint(request.GroupCount, request.GroupCount),
        };

        for (var groupId = 1; groupId <= request.GroupCount; groupId++)
        {
            constraints.Add(new GroupSizeConstraint(
                new GroupId(groupId),
                request.MinGroupSize,
                new GroupCapacity(request.MaxGroupSize)));
        }

        foreach (var pair in request.MandatoryPairs)
        {
            constraints.Add(new MandatoryPairConstraint(
                ResolveParticipant(pair.FirstParticipantId, knownParticipants, "mandatory pair"),
                ResolveParticipant(pair.SecondParticipantId, knownParticipants, "mandatory pair")));
        }

        foreach (var pair in request.ForbiddenPairs)
        {
            constraints.Add(new ForbiddenPairConstraint(
                ResolveParticipant(pair.FirstParticipantId, knownParticipants, "forbidden pair"),
                ResolveParticipant(pair.SecondParticipantId, knownParticipants, "forbidden pair")));
        }

        var executionContext = new InitialPlacementExecutionContext(AlgorithmRunId.New(), Environment.TickCount);
        return new InitialPlacementInput(executionContext, participants, constraints);
    }

    private static void ValidateRequest(InitialPlacementRequestDto request)
    {
        if (request.Participants.Count == 0)
        {
            throw new ArgumentException("At least one participant is required.");
        }

        if (request.GroupCount <= 0)
        {
            throw new ArgumentException("Group count must be greater than zero.");
        }

        if (request.MinGroupSize <= 0)
        {
            throw new ArgumentException("Min group size must be greater than zero.");
        }

        if (request.MaxGroupSize < request.MinGroupSize)
        {
            throw new ArgumentException("Max group size cannot be less than min group size.");
        }
    }

    private static ParticipantId ResolveParticipant(
        string participantId,
        IReadOnlySet<ParticipantId> knownParticipants,
        string pairKind)
    {
        if (string.IsNullOrWhiteSpace(participantId))
        {
            throw new ArgumentException($"A {pairKind} contains an empty participant id.");
        }

        var id = new ParticipantId(participantId.Trim());
        if (!knownParticipants.Contains(id))
        {
            throw new ArgumentException($"Participant '{id}' in {pairKind} is not listed in participants.");
        }

        return id;
    }
}
