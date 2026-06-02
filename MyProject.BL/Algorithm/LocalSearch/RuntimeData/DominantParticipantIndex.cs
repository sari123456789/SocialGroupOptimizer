using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.LocalSearch.RuntimeData;

/// <summary>
/// אינדקס למשתתפים שהרבה אחרים העדיפו.
/// </summary>
/// <remarks>
/// <para>תפקיד: זיהוי משתתפים "מבוקשים" (dominant) — סף דינמי לפי ממוצע העדפות נכנסות.</para>
/// <para>נקרא מ-: <see cref="RuntimeDataBuilder"/>, MoveGenerator עתידי — לעדיפות moves שמספקים מבוקשים.</para>
/// </remarks>
public sealed class DominantParticipantIndex
{
    private readonly Dictionary<ParticipantId, int> _incomingPreferenceCountByParticipant;
    private readonly double _dominantThreshold;

    private DominantParticipantIndex(
        Dictionary<ParticipantId, int> incomingPreferenceCountByParticipant,
        double dominantThreshold)
    {
        _incomingPreferenceCountByParticipant = incomingPreferenceCountByParticipant;
        _dominantThreshold = dominantThreshold;
    }

    /// <summary>
    /// בונה אינדקס משתתפים דומיננטיים — סף = max(2, ממוצע העדפות נכנסות).
    /// </summary>
    /// <param name="preferenceIndex">אינדקס העדפות — לספירת העדפות נכנסות.</param>
    /// <param name="participants">כל המשתתפים.</param>
    /// <returns>אינדקס עם ספירות וסף דומיננטיות.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="RuntimeDataBuilder.Build"/> (שלב רביעי — אחרי FriendClusterIndex).</para>
    /// </remarks>
    public static DominantParticipantIndex Create(
        ParticipantPreferenceIndex preferenceIndex,
        IReadOnlyList<Participant> participants)
    {
        if (preferenceIndex is null)
        {
            throw new ArgumentNullException(nameof(preferenceIndex));
        }

        if (participants is null)
        {
            throw new ArgumentNullException(nameof(participants));
        }

        var incomingCounts = new Dictionary<ParticipantId, int>();

        foreach (var participant in participants)
        {
            // ספירת העדפות נכנסות — כמה משתתפים העדיפו את participant.Id.
            incomingCounts[participant.Id] = preferenceIndex
                .GetParticipantsWhoPrefer(participant.Id)
                .Count;
        }

        var averageIncomingCount = incomingCounts.Count == 0
            ? 0
            : incomingCounts.Values.Average();

        // סף דומיננטיות: המקסימום בין 2 לבין ממוצע העדפות נכנסות.
        var dominantThreshold = Math.Max(2, averageIncomingCount);

        return new DominantParticipantIndex(incomingCounts, dominantThreshold);
    }

    /// <summary>
    /// מחזיר את מספר המשתתפים שהעדיפו את participantId.
    /// </summary>
    /// <param name="participantId">מזהה המשתתף.</param>
    /// <returns>מספר העדפות נכנסות; 0 אם לא נמצא.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="IsDominant"/>, IAssignmentScorer עתידי.</para>
    /// </remarks>
    public int GetIncomingPreferenceCount(ParticipantId participantId)
    {
        return _incomingPreferenceCountByParticipant.TryGetValue(participantId, out var count)
            ? count
            : 0;
    }

    /// <summary>
    /// מחזיר את המשתתפים עם הכי הרבה העדפות נכנסות — ממוין יורד.
    /// </summary>
    /// <param name="limit">מספר התוצאות המבוקש — חייב להיות גדול מ-0.</param>
    /// <returns>רשימת מזהי משתתפים — עד limit פריטים.</returns>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לבחירת מועמדים פופולריים — אין שימוש חיצוני כרגע.</para>
    /// </remarks>
    public IReadOnlyList<ParticipantId> GetMostRequestedParticipants(int limit)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");
        }

        // OrderByDescending על ספירה — ThenBy לשבירת שוויון יציבה.
        return _incomingPreferenceCountByParticipant
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key.Value, StringComparer.Ordinal)
            .Take(limit)
            .Select(entry => entry.Key)
            .ToList();
    }

    /// <summary>
    /// בודק האם participantId הוא משתתף דומיננטי — מספר העדפות נכנסות ≥ סף.
    /// </summary>
    /// <param name="participantId">מזהה המשתתף.</param>
    /// <returns>true אם המשתתף מעל/בסף הדומיננטיות; אחרת false.</returns>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לעדיפות moves שמספקים מבוקשים — אין שימוש חיצוני כרגע.</para>
    /// </remarks>
    public bool IsDominant(ParticipantId participantId)
    {
        // השוואה לסף שנקבע ב-Create — מספר העדפות נכנסות ≥ dominantThreshold.
        return GetIncomingPreferenceCount(participantId) >= _dominantThreshold;
    }
}
