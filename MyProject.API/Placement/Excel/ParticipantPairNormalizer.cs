namespace MyProject.API.Placement.Excel;

public static class ParticipantPairNormalizer
{
    public static string CreatePairKey(string firstParticipantId, string secondParticipantId)
    {
        if (string.IsNullOrWhiteSpace(firstParticipantId))
        {
            throw new ArgumentException("First participant id is required.", nameof(firstParticipantId));
        }

        if (string.IsNullOrWhiteSpace(secondParticipantId))
        {
            throw new ArgumentException("Second participant id is required.", nameof(secondParticipantId));
        }

        return string.CompareOrdinal(firstParticipantId, secondParticipantId) <= 0
            ? $"{firstParticipantId}|{secondParticipantId}"
            : $"{secondParticipantId}|{firstParticipantId}";
    }

    public static (string FirstParticipantId, string SecondParticipantId) NormalizePair(
        string firstParticipantId,
        string secondParticipantId)
    {
        var pairKey = CreatePairKey(firstParticipantId, secondParticipantId);
        var separatorIndex = pairKey.IndexOf('|');
        return (pairKey[..separatorIndex], pairKey[(separatorIndex + 1)..]);
    }
}
