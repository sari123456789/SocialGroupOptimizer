using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.LocalSearch.RuntimeData;

/// <summary>
/// אינדקס מהיר להעדפות משתתפים — סטטי, לא תלוי בחלוקה נוכחית.
/// </summary>
/// <remarks>
/// <para>תפקיד: ארבעה מבני lookup — מי העדיף את מי, HashSet ל-O(1), מי העדיף אותי, דירוג (Rank) לכל זוג.</para>
/// <para>נקרא מ-: <see cref="RuntimeDataBuilder"/>,
/// <see cref="MutualPreferenceIndex.Create"/>,
/// <see cref="DominantParticipantIndex.Create"/>,
/// <see cref="UnrequestedParticipantIndex.Create"/>.</para>
/// </remarks>
public sealed class ParticipantPreferenceIndex
{
    private readonly Dictionary<ParticipantId, IReadOnlyList<Preference>> _preferencesByParticipant;
    private readonly Dictionary<ParticipantId, HashSet<ParticipantId>> _preferredParticipantIdsByParticipant;
    private readonly Dictionary<ParticipantId, List<ParticipantId>> _participantsWhoPrefer;
    private readonly Dictionary<ParticipantId, Dictionary<ParticipantId, int>> _rankByParticipantPair;

    private ParticipantPreferenceIndex(
        Dictionary<ParticipantId, IReadOnlyList<Preference>> preferencesByParticipant,
        Dictionary<ParticipantId, HashSet<ParticipantId>> preferredParticipantIdsByParticipant,
        Dictionary<ParticipantId, List<ParticipantId>> participantsWhoPrefer,
        Dictionary<ParticipantId, Dictionary<ParticipantId, int>> rankByParticipantPair)
    {
        _preferencesByParticipant = preferencesByParticipant;
        _preferredParticipantIdsByParticipant = preferredParticipantIdsByParticipant;
        _participantsWhoPrefer = participantsWhoPrefer;
        _rankByParticipantPair = rankByParticipantPair;
    }

    /// <summary>
    /// בונה אינדקס העדפות מרשימת משתתפים — ממוין לפי Rank.
    /// </summary>
    /// <param name="participants">כל המשתתפים עם רשימות ההעדפות שלהם.</param>
    /// <returns>אינדקס מוכן לשאילתות O(1).</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="RuntimeDataBuilder.Build"/> (שלב ראשון בבניית snapshot).</para>
    /// </remarks>
    public static ParticipantPreferenceIndex Create(IReadOnlyList<Participant> participants)
    {
        if (participants is null)
        {
            throw new ArgumentNullException(nameof(participants));
        }

        // מילונים לחיפוש O(1) במהלך Local Search (אלפי קריאות).
        var preferencesByParticipant = new Dictionary<ParticipantId, IReadOnlyList<Preference>>();// משתתף רשימת העדפות ממוינת לפי Rank
        var preferredParticipantIdsByParticipant = new Dictionary<ParticipantId, HashSet<ParticipantId>>();//מילון לשמירת מזהי משתתפים שהמשתתף העדיף (לבדיקה מהירה אם משתתף מסוים נמצא ברשימת ההעדפות)
        var participantsWhoPrefer = new Dictionary<ParticipantId, List<ParticipantId>>();// משתתף רשימת משתתפים שהעדיפו אותו (כיוון הפוך)
        var rankByParticipantPair = new Dictionary<ParticipantId, Dictionary<ParticipantId, int>>();//מילון לשמירת דירוג (Rank) של העדפה בין זוג משתתפים (מי העדיף את מי ובאיזה סדר)

        foreach (var participant in participants)
        {
            // OrderBy Rank — 1 = העדפה ראשונה, 2 = שנייה...
            var sortedPreferences = participant.Preferences
                .OrderBy(preference => preference.Rank)
                .ToList();

            preferencesByParticipant[participant.Id] = sortedPreferences;

            // ToHashSet — מאפשר Contains מהיר ב-IsPreferredBy.
            var preferredIds = sortedPreferences
                .Select(preference => preference.PreferredParticipantId)
                .ToHashSet();

            preferredParticipantIdsByParticipant[participant.Id] = preferredIds;

            var ranks = new Dictionary<ParticipantId, int>(sortedPreferences.Count);
            foreach (var preference in sortedPreferences)
            {
                ranks[preference.PreferredParticipantId] = preference.Rank;

                // participantsWhoPrefer — "מי ביקש אותי" (כיוון הפוך).
                if (!participantsWhoPrefer.TryGetValue(preference.PreferredParticipantId, out var preferrers))
                {
                    preferrers = new List<ParticipantId>();
                    participantsWhoPrefer[preference.PreferredParticipantId] = preferrers;
                }

                preferrers.Add(participant.Id);
            }

            rankByParticipantPair[participant.Id] = ranks;
        }

        return new ParticipantPreferenceIndex(
            preferencesByParticipant,
            preferredParticipantIdsByParticipant,
            participantsWhoPrefer,
            rankByParticipantPair);
    }

    /// <summary>
    /// מחזיר את רשימת ההעדפות של משתתף — ממוינת לפי Rank.
    /// </summary>
    /// <param name="participantId">מזהה המשתתף.</param>
    /// <returns>רשימת Preference; ריקה אם אין העדפות.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="RuntimeDataBuilder"/> (BuildParticipantProfiles),
    /// <see cref="MutualPreferenceIndex.Create"/>.</para>
    /// </remarks>
    public IReadOnlyList<Preference> GetPreferredByParticipant(ParticipantId participantId)
    {
        if (_preferencesByParticipant.TryGetValue(participantId, out var preferences))
        {
            return preferences;
        }

        return Array.Empty<Preference>();
    }

    /// <summary>
    /// מחזיר את רשימת המשתתפים שהעדיפו את participantId (כיוון הפוך).
    /// </summary>
    /// <param name="participantId">מזהה המשתתף המבוקש.</param>
    /// <returns>רשימת מזהי משתתפים; ריקה אם אף אחד לא העדיף.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="DominantParticipantIndex.Create"/>,
    /// <see cref="UnrequestedParticipantIndex.Create"/>.</para>
    /// </remarks>
    public IReadOnlyList<ParticipantId> GetParticipantsWhoPrefer(ParticipantId participantId)
    {
        if (_participantsWhoPrefer.TryGetValue(participantId, out var preferrers))
        {
            return preferrers;
        }

        return Array.Empty<ParticipantId>();
    }

    /// <summary>
    /// בודק האם participantId העדיף את preferredParticipantId — O(1).
    /// </summary>
    /// <param name="participantId">המשתתף שבודקים את ההעדפה שלו.</param>
    /// <param name="preferredParticipantId">המועדף.</param>
    /// <returns>true אם קיימת העדפה; אחרת false.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="MutualPreferenceIndex.Create"/> (זיהוי הדדיות).</para>
    /// </remarks>
    public bool IsPreferredBy(ParticipantId participantId, ParticipantId preferredParticipantId)
    {
        return _preferredParticipantIdsByParticipant.TryGetValue(participantId, out var preferredIds)
            && preferredIds.Contains(preferredParticipantId);
    }

    /// <summary>
    /// מחזיר את דירוג ההעדפה (Rank) של preferredParticipantId אצל participantId.
    /// </summary>
    /// <param name="participantId">המשתתף שבודקים את ההעדפות שלו.</param>
    /// <param name="preferredParticipantId">המועדף.</param>
    /// <returns>Rank (1 = ראשון); null אם אין העדפה.</returns>
    /// <remarks>
    /// <para>נקרא מ-: IAssignmentScorer עתידי — לחישוב ציון לפי דירוג — אין שימוש חיצוני כרגע.</para>
    /// </remarks>
    public int? GetPreferenceRank(ParticipantId participantId, ParticipantId preferredParticipantId)
    {
        if (!_rankByParticipantPair.TryGetValue(participantId, out var ranks))
        {
            return null;
        }

        return ranks.TryGetValue(preferredParticipantId, out var rank)
            ? rank
            : null;
    }
}
