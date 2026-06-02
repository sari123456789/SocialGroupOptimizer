using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.LocalSearch.RuntimeData;

/// <summary>
/// אינדקס למשתתפים שאף אחד לא העדיף.
/// </summary>
/// <remarks>
/// <para>תפקיד: זיהוי משתתפים "לא מבוקשים" — לסינון moves ולחישוב ציון.</para>
/// <para>נקרא מ-: <see cref="RuntimeDataBuilder"/>, MoveGenerator עתידי.</para>
/// </remarks>
public sealed class UnrequestedParticipantIndex
{
    private readonly HashSet<ParticipantId> _unrequestedParticipants;

    private UnrequestedParticipantIndex(HashSet<ParticipantId> unrequestedParticipants)
    {
        _unrequestedParticipants = unrequestedParticipants;
    }

    /// <summary>
    /// בונה אינדקס משתתפים שלא קיבלו אף העדפה נכנסת.
    /// </summary>
    /// <param name="preferenceIndex">אינדקס העדפות — לבדיקת העדפות נכנסות.</param>
    /// <param name="participants">כל המשתתפים.</param>
    /// <returns>אינדקס עם HashSet של משתתפים לא מבוקשים.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="RuntimeDataBuilder.Build"/> (שלב חמישי — במקביל ל-DominantParticipantIndex).</para>
    /// </remarks>
    public static UnrequestedParticipantIndex Create(
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

        // Where — משתתפים עם 0 העדפות נכנסות; ToHashSet לבדיקת O(1).
        var unrequestedParticipants = participants
            .Where(participant => preferenceIndex.GetParticipantsWhoPrefer(participant.Id).Count == 0)
            .Select(participant => participant.Id)
            .ToHashSet();

        return new UnrequestedParticipantIndex(unrequestedParticipants);
    }

    /// <summary>
    /// מחזיר את כל המשתתפים שלא קיבלו אף העדפה — ממוין לפי מזהה.
    /// </summary>
    /// <returns>רשימת ParticipantId של משתתפים לא מבוקשים.</returns>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לסינון/עדיפות moves — אין שימוש חיצוני כרגע.</para>
    /// </remarks>
    public IReadOnlyList<ParticipantId> GetUnrequestedParticipants()
    {
        return _unrequestedParticipants
            .OrderBy(id => id.Value, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// בודק האם participantId לא קיבל אף העדפה נכנסת — O(1).
    /// </summary>
    /// <param name="participantId">מזהה המשתתף.</param>
    /// <returns>true אם אף אחד לא העדיף; אחרת false.</returns>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator ו-IAssignmentScorer עתידיים — אין שימוש חיצוני כרגע.</para>
    /// </remarks>
    public bool IsUnrequested(ParticipantId participantId)
    {
        return _unrequestedParticipants.Contains(participantId);
    }
}
