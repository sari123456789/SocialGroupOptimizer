using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.LocalSearch.RuntimeData;

/// <summary>
/// בונה <see cref="RuntimeDataSnapshot"/> מניתוח החלוקה הנוכחית בלבד.
/// </summary>
public static class RuntimeDataBuilder
{
    /// <summary>
    /// נקודת כניסה: בונה את כל מבני הנתונים מהחלוקה הנוכחית.
    /// </summary>
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

        var preferenceIndex = ParticipantPreferenceIndex.Create(participants);
        var mutualPreferenceIndex = MutualPreferenceIndex.Create(preferenceIndex, participants);
        var friendClusterIndex = FriendClusterIndex.Create(mutualPreferenceIndex, participants);
        var dominantParticipantIndex = DominantParticipantIndex.Create(preferenceIndex, participants);
        var unrequestedParticipantIndex = UnrequestedParticipantIndex.Create(preferenceIndex, participants);

        var participantProfiles = BuildParticipantProfiles(state, preferenceIndex);
        var isolatedParticipantIds = BuildIsolatedParticipantIds(participantProfiles);
        var nearMissParticipantIds = BuildNearMissParticipantIds(participantProfiles);
        var weakGroupIdsByWeakness = BuildWeakGroupIdsByWeakness(participantProfiles);
        var groupProfiles = BuildGroupProfiles(state);
        var lowContributionParticipantIdsByGroup = BuildLowContributionParticipantIdsByGroup(
            groupProfiles,
            participantProfiles);
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
            closedFriendGroupProfiles,
            isolatedParticipantIds,
            nearMissParticipantIds,
            weakGroupIdsByWeakness,
            lowContributionParticipantIdsByGroup);
    }

    private static Dictionary<ParticipantId, ParticipantRuntimeProfile> BuildParticipantProfiles(
        AssignmentState state,
        ParticipantPreferenceIndex preferenceIndex)
    {
        var profiles = new Dictionary<ParticipantId, ParticipantRuntimeProfile>();

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
            var isIsolated = hasPreferences && preferredInsideCount == 0;
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
    /// מזהי מבודדים — ממוינים לפי פוטנציאל שיפור (מועדפים-בחוץ יורד).
    /// </summary>
    private static IReadOnlyList<ParticipantId> BuildIsolatedParticipantIds(
        IReadOnlyDictionary<ParticipantId, ParticipantRuntimeProfile> participantProfiles)
    {
        return participantProfiles.Values
            .Where(profile => profile.IsIsolated)
            .OrderByDescending(profile => profile.PreferredOutsideCount)
            .ThenBy(profile => profile.ParticipantId.Value, StringComparer.Ordinal)
            .Select(profile => profile.ParticipantId)
            .ToList();
    }

    /// <summary>
    /// מזהי near-miss — אותו סדר מיון כמו מבודדים.
    /// </summary>
    private static IReadOnlyList<ParticipantId> BuildNearMissParticipantIds(
        IReadOnlyDictionary<ParticipantId, ParticipantRuntimeProfile> participantProfiles)
    {
        return participantProfiles.Values
            .Where(profile => profile.IsNearMiss)
            .OrderByDescending(profile => profile.PreferredOutsideCount)
            .ThenBy(profile => profile.ParticipantId.Value, StringComparer.Ordinal)
            .Select(profile => profile.ParticipantId)
            .ToList();
    }

    /// <summary>
    /// קבוצות חלשות: חולשה = סכום PreferredOutsideCount של חברי הקבוצה; רק חולשה &gt; 0.
    /// </summary>
    private static IReadOnlyList<GroupId> BuildWeakGroupIdsByWeakness(
        IReadOnlyDictionary<ParticipantId, ParticipantRuntimeProfile> participantProfiles)
    {
        var weaknessByGroup = new Dictionary<GroupId, int>();

        foreach (var profile in participantProfiles.Values)
        {
            weaknessByGroup.TryGetValue(profile.CurrentGroupId, out var current);
            weaknessByGroup[profile.CurrentGroupId] = current + profile.PreferredOutsideCount;
        }

        return weaknessByGroup
            .Where(entry => entry.Value > 0)
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key.Value)
            .Select(entry => entry.Key)
            .ToList();
    }

    /// <summary>
    /// לכל קבוצה: חברים ממוינים מתרומה נמוכה לגבוהה (מבודדים ראשונים).
    /// </summary>
    private static Dictionary<GroupId, IReadOnlyList<ParticipantId>> BuildLowContributionParticipantIdsByGroup(
        IReadOnlyDictionary<GroupId, GroupRuntimeProfile> groupProfiles,
        IReadOnlyDictionary<ParticipantId, ParticipantRuntimeProfile> participantProfiles)
    {
        var result = new Dictionary<GroupId, IReadOnlyList<ParticipantId>>();

        foreach (var (groupId, groupProfile) in groupProfiles)
        {
            if (groupProfile.Participants.Count == 0)
            {
                continue;
            }

            var sorted = groupProfile.Participants
                .Select(memberId => participantProfiles.TryGetValue(memberId, out var profile) ? profile : null)
                .Where(profile => profile is not null)
                .OrderByDescending(profile => profile!.IsIsolated)
                .ThenBy(profile => profile!.PreferredInsideCount)
                .ThenByDescending(profile => profile!.PreferredOutsideCount)
                .ThenBy(profile => profile!.ParticipantId.Value, StringComparer.Ordinal)
                .Select(profile => profile!.ParticipantId)
                .ToList();

            result[groupId] = sorted;
        }

        return result;
    }

    private static Dictionary<GroupId, GroupRuntimeProfile> BuildGroupProfiles(AssignmentState state)
    {
        var profiles = new Dictionary<GroupId, GroupRuntimeProfile>();

        foreach (var (groupId, participants) in state.GroupToParticipants)
        {
            var participantsCopy = participants.ToList();
            profiles[groupId] = new GroupRuntimeProfile(
                groupId,
                participantsCopy,
                participantsCopy.Count);
        }

        return profiles;
    }

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

    private static int CountMutualConnections(
        IReadOnlyList<ParticipantId> members,
        MutualPreferenceIndex mutualPreferenceIndex)
    {
        var connectionCount = 0;

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

    private static double ComputeDensityScore(int memberCount, int mutualConnectionCount)
    {
        if (memberCount < 2)
        {
            return 0;
        }

        var maxPossibleConnections = memberCount * (memberCount - 1) / 2.0;
        return mutualConnectionCount / maxPossibleConnections;
    }

    private static bool IsPreferredInSameGroup(
        AssignmentState state,
        ParticipantId participantId,
        GroupId currentGroupId,
        ParticipantId preferredParticipantId)
    {
        return state.ParticipantToGroup.TryGetValue(preferredParticipantId, out var preferredGroupId)
            && preferredGroupId == currentGroupId;
    }
}
