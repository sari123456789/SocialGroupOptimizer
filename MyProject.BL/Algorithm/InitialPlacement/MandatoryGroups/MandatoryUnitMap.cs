using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// מיפוי בין משתתף ליחידת החובה שלו.
/// </summary>
public sealed class MandatoryUnitMap
{
    public MandatoryUnitMap(
        IReadOnlyDictionary<ParticipantId, int> unitIdByParticipant,
        IReadOnlyDictionary<int, List<ParticipantId>> units)
    {
        UnitIdByParticipant = unitIdByParticipant;
        Units = units;
    }

    public IReadOnlyDictionary<int, List<ParticipantId>> Units { get; }

    private IReadOnlyDictionary<ParticipantId, int> UnitIdByParticipant { get; }

    public bool TryGetUnitId(ParticipantId participantId, out int unitId)
    {
        return UnitIdByParticipant.TryGetValue(participantId, out unitId);
    }
}
