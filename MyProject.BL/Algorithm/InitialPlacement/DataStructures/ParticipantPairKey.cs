using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// תפקיד: מפתח אחיד לזוג משתתפים — מנרמל סדר כך ששני משתתפים תמיד מיוצגים באותו מפתח.
/// </summary>
/// <param name="FirstParticipantId">מזהה המשתתף הראשון (הקטן לקסיקוגרפית).</param>
/// <param name="SecondParticipantId">מזהה המשתתף השני (הגדול לקסיקוגרפית).</param>
/// <remarks>
/// משמש להשוואת זוגות חובה/איסור בלי תלות בסדר הופעת המשתתפים.
/// </remarks>
public readonly record struct ParticipantPairKey(string FirstParticipantId, string SecondParticipantId)
{
    /// <summary>
    /// תפקיד: יוצר מפתח מנורמל משני מזהי משתתפים.
    /// </summary>
    /// <param name="firstParticipantId">משתתף ראשון.</param>
    /// <param name="secondParticipantId">משתתף שני.</param>
    /// <returns>מפתח שבו המזהה הקטן יותר תמיד ראשון.</returns>
    /// <remarks>נקרא מ- <see cref="FeasibilityPreChecker"/> — לזיהוי סתירות בין זוגות חובה ואיסור.</remarks>
    public static ParticipantPairKey Create(ParticipantId firstParticipantId, ParticipantId secondParticipantId)
    {
        var first = firstParticipantId.Value;
        var second = secondParticipantId.Value;

        // CompareOrdinal — סדר לקסיקוגרפי על מחרוזת ת.ז.; הקטן תמיד First.
        return string.CompareOrdinal(first, second) <= 0
            ? new ParticipantPairKey(first, second)
            : new ParticipantPairKey(second, first);
    }
}
