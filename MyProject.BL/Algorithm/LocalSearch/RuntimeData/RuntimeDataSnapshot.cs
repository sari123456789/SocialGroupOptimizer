using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.LocalSearch.RuntimeData;

/// <summary>
/// snapshot read-only של נתוני ריצה — לא משנה את <see cref="SolutionState.AssignmentState"/>.
/// </summary>
/// <remarks>
/// <para>תפקיד: אגרגציה של אינדקסים, פרופילים ורשימות מוכנות ליצירת מועמדים.</para>
/// <para>נקרא מ-: <see cref="RuntimeDataBuilder.Build"/> (יצירה), Generation.Strategies.</para>
/// </remarks>
public sealed class RuntimeDataSnapshot
{
    /// <summary>
    /// יוצר snapshot read-only של כל נתוני הריצה.
    /// </summary>
    public RuntimeDataSnapshot(
        ParticipantPreferenceIndex preferenceIndex,
        MutualPreferenceIndex mutualPreferenceIndex,
        FriendClusterIndex friendClusterIndex,
        DominantParticipantIndex dominantParticipantIndex,
        UnrequestedParticipantIndex unrequestedParticipantIndex,
        IReadOnlyDictionary<ParticipantId, ParticipantRuntimeProfile> participantProfiles,
        IReadOnlyDictionary<GroupId, GroupRuntimeProfile> groupProfiles,
        IReadOnlyList<ClosedFriendGroupProfile> closedFriendGroupProfiles,
        IReadOnlyList<ParticipantId> isolatedParticipantIds,
        IReadOnlyList<ParticipantId> nearMissParticipantIds,
        IReadOnlyList<GroupId> weakGroupIdsByWeakness,
        IReadOnlyDictionary<GroupId, IReadOnlyList<ParticipantId>> lowContributionParticipantIdsByGroup)
    {
        PreferenceIndex = preferenceIndex ?? throw new ArgumentNullException(nameof(preferenceIndex));
        MutualPreferenceIndex = mutualPreferenceIndex ?? throw new ArgumentNullException(nameof(mutualPreferenceIndex));
        FriendClusterIndex = friendClusterIndex ?? throw new ArgumentNullException(nameof(friendClusterIndex));
        DominantParticipantIndex = dominantParticipantIndex ?? throw new ArgumentNullException(nameof(dominantParticipantIndex));
        UnrequestedParticipantIndex = unrequestedParticipantIndex ?? throw new ArgumentNullException(nameof(unrequestedParticipantIndex));
        ParticipantProfiles = participantProfiles ?? throw new ArgumentNullException(nameof(participantProfiles));
        GroupProfiles = groupProfiles ?? throw new ArgumentNullException(nameof(groupProfiles));
        ClosedFriendGroupProfiles = closedFriendGroupProfiles ?? throw new ArgumentNullException(nameof(closedFriendGroupProfiles));
        IsolatedParticipantIds = isolatedParticipantIds ?? throw new ArgumentNullException(nameof(isolatedParticipantIds));
        NearMissParticipantIds = nearMissParticipantIds ?? throw new ArgumentNullException(nameof(nearMissParticipantIds));
        WeakGroupIdsByWeakness = weakGroupIdsByWeakness ?? throw new ArgumentNullException(nameof(weakGroupIdsByWeakness));
        LowContributionParticipantIdsByGroup = lowContributionParticipantIdsByGroup
            ?? throw new ArgumentNullException(nameof(lowContributionParticipantIdsByGroup));
    }

    public ParticipantPreferenceIndex PreferenceIndex { get; }

    public MutualPreferenceIndex MutualPreferenceIndex { get; }

    public FriendClusterIndex FriendClusterIndex { get; }

    public DominantParticipantIndex DominantParticipantIndex { get; }

    public UnrequestedParticipantIndex UnrequestedParticipantIndex { get; }

    public IReadOnlyDictionary<ParticipantId, ParticipantRuntimeProfile> ParticipantProfiles { get; }

    public IReadOnlyDictionary<GroupId, GroupRuntimeProfile> GroupProfiles { get; }

    public IReadOnlyList<ClosedFriendGroupProfile> ClosedFriendGroupProfiles { get; }

    /// <summary>מזהי משתתפים מבודדים — ממוינים לפי מועדפים-בחוץ יורד ואז מזהה.</summary>
    public IReadOnlyList<ParticipantId> IsolatedParticipantIds { get; }

    /// <summary>מזהי משתתפים במצב כמעט-שיפור — ממוינים כמו מבודדים.</summary>
    public IReadOnlyList<ParticipantId> NearMissParticipantIds { get; }

    /// <summary>קבוצות חלשות — ממוינות לפי חולשה יורדת (סכום מועדפים-בחוץ של חברים).</summary>
    public IReadOnlyList<GroupId> WeakGroupIdsByWeakness { get; }

    /// <summary>משתתפים בעלי תרומה נמוכה לפי קבוצה — ממוינים ליצירת מועמדי החלפה.</summary>
    public IReadOnlyDictionary<GroupId, IReadOnlyList<ParticipantId>> LowContributionParticipantIdsByGroup { get; }
}
