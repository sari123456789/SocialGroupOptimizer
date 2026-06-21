namespace MyProject.API.Placement.Excel;

// עוזר לסדר זוגות ת.ז. באותו אופן תמיד
public static class ParticipantPairNormalizer
{
    // מפתח ייחודי לזוג — בלי קשר לסדר
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

        // מסדרים אלפביתית ומחברים עם |
        return string.CompareOrdinal(firstParticipantId, secondParticipantId) <= 0
            ? $"{firstParticipantId}|{secondParticipantId}"
            : $"{secondParticipantId}|{firstParticipantId}";
    }

    // מחזיר את הזוג מסודר (קטן קודם)
    public static (string FirstParticipantId, string SecondParticipantId) NormalizePair(
        string firstParticipantId,
        string secondParticipantId)
    {
        var pairKey = CreatePairKey(firstParticipantId, secondParticipantId);
        var separatorIndex = pairKey.IndexOf('|');
        return (pairKey[..separatorIndex], pairKey[(separatorIndex + 1)..]);
    }
}
