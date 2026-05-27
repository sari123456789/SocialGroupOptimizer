using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// מפתח אחיד לזוג משתתפים, ללא תלות בסדר.
/// </summary>
public readonly record struct ParticipantPairKey(string FirstParticipantId, string SecondParticipantId)
{
    public static ParticipantPairKey Create(ParticipantId firstParticipantId, ParticipantId secondParticipantId)
    {
        var first = firstParticipantId.Value;
        var second = secondParticipantId.Value;

        // תמיד שומרים את המזהה הקטן יותר ראשון, כדי להשוות זוגות בצורה עקבית.
        return string.CompareOrdinal(first, second) <= 0
            ? new ParticipantPairKey(first, second)
            : new ParticipantPairKey(second, first);
    }
}
