using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.LocalSearch.RuntimeData;

/// <summary>
/// בונה <see cref="RuntimeDataSnapshot"/> מניתוח החלוקה הנוכחית בלבד.
/// </summary>
/// <remarks>
/// <para>תפקיד: הרכבת אינדקסים סטטיים ופרופילים דינמיים — cache לחיפוש מקומי; לא בודק אילוצים ולא מבצע moves.</para>
/// <para>נקרא מ-: מתזמר Local Search עתידי — לפני כל איטרציה/הערכת moves; אין שימוש חיצוני כרגע.</para>
/// <list type="bullet">
///   <item><description>אינדקסים — העדפות, הדדיות, קלאסטרים</description></item>
///   <item><description>פרופילים — לכל משתתף/קבוצה/קלאסטר חברים</description></item>
/// </list>
/// </remarks>
public static class RuntimeDataBuilder
{
    /// <summary>
    /// נקודת כניסה: בונה את כל מבני הנתונים מהחלוקה הנוכחית.
    /// </summary>
    /// <param name="state">מצב החלוקה הנוכחי — מקור לפרופילים דינמיים.</param>
    /// <param name="participants">כל המשתתפים עם העדפות — מקור לאינדקסים סטטיים.</param>
    /// <returns>RuntimeDataSnapshot read-only עם כל האינדקסים והפרופילים.</returns>
    /// <remarks>
    /// <para>נקרא מ-: מתזמר Local Search עתידי — אין קריאות חיצוניות כרגע.</para>
    /// </remarks>
    public static RuntimeDataSnapshot Build(
        AssignmentState state,
        IReadOnlyList<Participant> participants)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (participants is null)
        {
            throw new ArgumentNullException(nameof(participants));
        }

        // ===== שלב 1: בניית אינדקסים גלובליים =====
        // הסדר חשוב: Mutual תלוי ב-Preference; FriendCluster תלוי ב-Mutual.
        var preferenceIndex = ParticipantPreferenceIndex.Create(participants);
        var mutualPreferenceIndex = MutualPreferenceIndex.Create(preferenceIndex, participants);
        var friendClusterIndex = FriendClusterIndex.Create(mutualPreferenceIndex, participants);
        var dominantParticipantIndex = DominantParticipantIndex.Create(preferenceIndex, participants);
        var unrequestedParticipantIndex = UnrequestedParticipantIndex.Create(preferenceIndex, participants);

        // ===== שלב 2: פרופילים לפי מצב השיבוץ הנוכחי =====
        var participantProfiles = BuildParticipantProfiles(state, preferenceIndex);
        var groupProfiles = BuildGroupProfiles(state);
        var closedFriendGroupProfiles = BuildClosedFriendGroupProfiles(
            state,
            mutualPreferenceIndex,
            friendClusterIndex);

        return new RuntimeDataSnapshot(
            preferenceIndex,
            mutualPreferenceIndex,
            friendClusterIndex,
            dominantParticipantIndex,
            unrequestedParticipantIndex,
            participantProfiles,
            groupProfiles,
            closedFriendGroupProfiles);
    }

    /// <summary>
    /// לכל משתתף: כמה העדפות בתוך הקבוצה / מחוץ לה, והאם מבודד או near-miss.
    /// </summary>
    /// <param name="state">מצב החלוקה הנוכחי.</param>
    /// <param name="preferenceIndex">אינדקס העדפות לשאילתות.</param>
    /// <returns>מילון ParticipantId → ParticipantRuntimeProfile.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="Build"/> (שלב 2 — פרופילי משתתפים).</para>
    /// </remarks>
    private static Dictionary<ParticipantId, ParticipantRuntimeProfile> BuildParticipantProfiles(
        AssignmentState state,
        ParticipantPreferenceIndex preferenceIndex)
    {
        var profiles = new Dictionary<ParticipantId, ParticipantRuntimeProfile>();

        // deconstruction: (participantId, currentGroupId) — מפרק זוג key-value מהמילון.
        foreach (var (participantId, currentGroupId) in state.ParticipantToGroup)
        {
            var preferences = preferenceIndex.GetPreferredByParticipant(participantId);
            var preferredInsideCount = 0;
            var preferredOutsideCount = 0;

            foreach (var preference in preferences)
            {
                if (IsPreferredInSameGroup(state, participantId, currentGroupId, preference.PreferredParticipantId))
                {
                    preferredInsideCount++;
                }
                else
                {
                    preferredOutsideCount++;
                }
            }

            var hasPreferences = preferences.Count > 0;
            // מבודד = יש העדפות אבל אף אחת לא בקבוצה שלו.
            var isIsolated = hasPreferences && preferredInsideCount == 0;
            // near-miss = חלק בפנים, חלק בחוץ — פוטנציאל לשיפור ב-swap.
            var isNearMiss = preferredInsideCount > 0 && preferredOutsideCount > 0;

            profiles[participantId] = new ParticipantRuntimeProfile(
                participantId,
                currentGroupId,
                preferredInsideCount,
                preferredOutsideCount,
                isIsolated,
                isNearMiss);
        }

        return profiles;
    }

    /// <summary>
    /// בונה פרופיל runtime לכל קבוצה — חברים וגודל.
    /// </summary>
    /// <param name="state">מצב החלוקה הנוכחי.</param>
    /// <returns>מילון GroupId → GroupRuntimeProfile.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="Build"/> (שלב 2 — פרופילי קבוצות).</para>
    /// </remarks>
    private static Dictionary<GroupId, GroupRuntimeProfile> BuildGroupProfiles(AssignmentState state)
    {
        var profiles = new Dictionary<GroupId, GroupRuntimeProfile>();

        foreach (var (groupId, participants) in state.GroupToParticipants)
        {
            // ToList() — עותק כדי שלא ישתנה הרשימה המקורית ב-state.
            var participantsCopy = participants.ToList();
            profiles[groupId] = new GroupRuntimeProfile(
                groupId,
                participantsCopy,
                participantsCopy.Count);
        }

        return profiles;
    }

    /// <summary>
    /// קלאסטרים של חברים הדדיים — האם מפוצלים בין קבוצות.
    /// </summary>
    /// <param name="state">מצב החלוקה הנוכחי.</param>
    /// <param name="mutualPreferenceIndex">אינדקס הדדיות — לספירת קשרים.</param>
    /// <param name="friendClusterIndex">אינדקס קלאסטרים — לרשימת חברים.</param>
    /// <returns>רשימת ClosedFriendGroupProfile — רק קלאסטרים עם ≥2 חברים.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="Build"/> (שלב 2 — פרופילי קלאסטרים).</para>
    /// </remarks>
    private static List<ClosedFriendGroupProfile> BuildClosedFriendGroupProfiles(
        AssignmentState state,
        MutualPreferenceIndex mutualPreferenceIndex,
        FriendClusterIndex friendClusterIndex)
    {
        var profiles = new List<ClosedFriendGroupProfile>();

        foreach (var cluster in friendClusterIndex.GetClusters())
        {
            if (cluster.Count < 2)
            {
                continue;
            }

            var clusterId = friendClusterIndex.GetClusterId(cluster[0]);
            var mutualConnectionCount = CountMutualConnections(cluster, mutualPreferenceIndex);
            // densityScore — 0..1, כמה "צפוף" הקלאסטר מבחינת קשרים הדדיים.
            var densityScore = ComputeDensityScore(cluster.Count, mutualConnectionCount);
            var currentGroupIds = cluster
                .Select(memberId => state.ParticipantToGroup[memberId])
                .Distinct()
                .OrderBy(groupId => groupId.Value)
                .ToList();

            profiles.Add(new ClosedFriendGroupProfile(
                clusterId,
                cluster,
                mutualConnectionCount,
                densityScore,
                currentGroupIds,
                isSplitAcrossGroups: currentGroupIds.Count > 1));
        }

        return profiles
            .OrderBy(profile => profile.ClusterId)
            .ToList();
    }

    /// <summary>
    /// סופר זוגות עם העדפה הדדית בתוך הקלאסטר (משולשים לא נספרים כפול).
    /// </summary>
    /// <param name="members">חברי הקלאסטר.</param>
    /// <param name="mutualPreferenceIndex">אינדקס הדדיות — לבדיקת AreMutual.</param>
    /// <returns>מספר קשרים הדדיים ייחודיים בקלאסטר.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="BuildClosedFriendGroupProfiles"/>.</para>
    /// </remarks>
    private static int CountMutualConnections(
        IReadOnlyList<ParticipantId> members,
        MutualPreferenceIndex mutualPreferenceIndex)
    {
        var connectionCount = 0;

        // לולאה כפולה — כל זוג (i,j) פעם אחת, i &lt; j.
        for (var i = 0; i < members.Count; i++)
        {
            for (var j = i + 1; j < members.Count; j++)
            {
                if (mutualPreferenceIndex.AreMutual(members[i], members[j]))
                {
                    connectionCount++;
                }
            }
        }

        return connectionCount;
    }

    /// <summary>
    /// צפיפות = קשרים_קיימים / קשרים_אפשריים. בגרף מלא: n*(n-1)/2.
    /// </summary>
    /// <param name="memberCount">מספר חברי הקלאסטר.</param>
    /// <param name="mutualConnectionCount">מספר קשרים הדדיים קיימים.</param>
    /// <returns>צפיפות בין 0 ל-1; 0 אם memberCount &lt; 2.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="BuildClosedFriendGroupProfiles"/>.</para>
    /// </remarks>
    private static double ComputeDensityScore(int memberCount, int mutualConnectionCount)
    {
        if (memberCount < 2)
        {
            return 0;
        }

        var maxPossibleConnections = memberCount * (memberCount - 1) / 2.0;
        return mutualConnectionCount / maxPossibleConnections;
    }

    /// <summary>
    /// בודק האם preferredParticipantId נמצא באותה קבוצה כמו participantId.
    /// </summary>
    /// <param name="state">מצב החלוקה הנוכחי.</param>
    /// <param name="participantId">המשתתף שבודקים את ההעדפות שלו.</param>
    /// <param name="currentGroupId">קבוצת המשתתף.</param>
    /// <param name="preferredParticipantId">המועדף לבדיקה.</param>
    /// <returns>true אם המועדף משובץ באותה קבוצה; false אם לא משובץ או בקבוצה אחרת.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="BuildParticipantProfiles"/>.</para>
    /// </remarks>
    private static bool IsPreferredInSameGroup(
        AssignmentState state,
        ParticipantId participantId,
        GroupId currentGroupId,
        ParticipantId preferredParticipantId)
    {
        // TryGetValue — בטוח: אם המועדף לא משובץ, false.
        return state.ParticipantToGroup.TryGetValue(preferredParticipantId, out var preferredGroupId)
            && preferredGroupId == currentGroupId;
    }
}
