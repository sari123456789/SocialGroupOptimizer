using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.LocalSearch.RuntimeData;

/// <summary>
/// אינדקס להעדפות הדדיות — A העדיף את B וגם B העדיף את A.
/// </summary>
/// <remarks>
/// <para>תפקיד: גרף קשרים הדדיים — AreMutual ב-O(1) דרך HashSet; בסיס ל-FriendClusterIndex.</para>
/// <para>נקרא מ-: <see cref="RuntimeDataBuilder"/>,
/// <see cref="FriendClusterIndex.Create"/>,
/// <see cref="RuntimeDataBuilder"/> (CountMutualConnections).</para>
/// </remarks>
public sealed class MutualPreferenceIndex
{
    private readonly Dictionary<ParticipantId, HashSet<ParticipantId>> _mutualPreferencesByParticipant;

    private MutualPreferenceIndex(
        Dictionary<ParticipantId, HashSet<ParticipantId>> mutualPreferencesByParticipant)
    {
        _mutualPreferencesByParticipant = mutualPreferencesByParticipant;
    }

    /// <summary>
    /// בונה אינדקס הדדיות על בסיס ParticipantPreferenceIndex — ללא סריקה חוזרת של Preferences.
    /// </summary>
    /// <param name="preferenceIndex">אינדקס העדפות מוכן.</param>
    /// <param name="participants">כל המשתתפים — ליצירת מפתחות לכל משתתף.</param>
    /// <returns>אינדקס קשרים הדדיים דו-כיווני.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="RuntimeDataBuilder.Build"/> (שלב שני — אחרי PreferenceIndex).</para>
    /// </remarks>
    public static MutualPreferenceIndex Create(
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

        var mutualPreferencesByParticipant = new Dictionary<ParticipantId, HashSet<ParticipantId>>();

        foreach (var participant in participants)
        {
            mutualPreferencesByParticipant[participant.Id] = new HashSet<ParticipantId>();
        }

        foreach (var participant in participants)
        {
            foreach (var preference in preferenceIndex.GetPreferredByParticipant(participant.Id))
            {
                var preferredId = preference.PreferredParticipantId;

                // הדדיות: A→B וגם B→A (בדיקה דרך IsPreferredBy — כיוון הפוך).
                if (preferenceIndex.IsPreferredBy(preferredId, participant.Id))
                {
                    // שמירה דו-כיוונית — AreMutual(A,B) לא תלוי בסדר.
                    mutualPreferencesByParticipant[participant.Id].Add(preferredId);
                    mutualPreferencesByParticipant[preferredId].Add(participant.Id);
                }
            }
        }

        return new MutualPreferenceIndex(mutualPreferencesByParticipant);
    }

    /// <summary>
    /// בודק האם שני משתתפים מעדיפים זה את זה — O(1).
    /// </summary>
    /// <param name="participantA">משתתף ראשון.</param>
    /// <param name="participantB">משתתף שני.</param>
    /// <returns>true אם קיים קשר הדדי; אחרת false.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="RuntimeDataBuilder"/> (CountMutualConnections),
    /// MoveGenerator עתידי.</para>
    /// </remarks>
    public bool AreMutual(ParticipantId participantA, ParticipantId participantB)
    {
        return _mutualPreferencesByParticipant.TryGetValue(participantA, out var mutualIds)
            && mutualIds.Contains(participantB);
    }

    /// <summary>
    /// מחזיר את כל המשתתפים עם קשר הדדי ל-participantId — ממוין לפי מזהה.
    /// </summary>
    /// <param name="participantId">מזהה המשתתף.</param>
    /// <returns>רשימת מזהי משתתפים עם העדפה הדדית; ריקה אם אין.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="FriendClusterIndex"/> (BuildAdjacency).</para>
    /// </remarks>
    public IReadOnlyList<ParticipantId> GetMutualPreferences(ParticipantId participantId)
    {
        if (_mutualPreferencesByParticipant.TryGetValue(participantId, out var mutualIds))
        {
            return mutualIds
                .OrderBy(id => id.Value, StringComparer.Ordinal)
                .ToList();
        }

        return Array.Empty<ParticipantId>();
    }
}
