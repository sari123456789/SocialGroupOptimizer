using MyProject.Core.Domain.ValueObjects;
using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.BL.Algorithm.LocalSearch.RuntimeData;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.LocalSearch.Generation.Strategies;

/// <summary>
/// תפקיד המחלקה: אסטרטגיית קבוצות חלשות — מזהה קבוצות עם סכום גבוה של העדפות לא ממומשות.
/// המחלקה משתתפת בשלב יצירת מועמדים — מנסה לשפר קבוצות בעלות חולשה גבוהה.
/// </summary>
public sealed class LowScoreGroupStrategy : IMoveCandidateStrategy
{
    private const int MaxMembersPerGroup = 3;
    private const double WeaknessWeight = 0.01;

    /// <inheritdoc />
    public CandidateSource Source => CandidateSource.LowScoreGroup;

    /// <inheritdoc />
    public bool IsApplicable(RuntimeDataSnapshot snapshot, MoveGenerationContext context)
    {
        if (snapshot is null || context is null)
        {
            return false;
        }

        return snapshot.WeakGroupIdsByWeakness.Count > 0;
    }

    /// <inheritdoc />
    public IReadOnlyList<CandidateProposal> Generate(
        AssignmentState state,
        RuntimeDataSnapshot snapshot,
        MoveGenerationContext context)
    {
        if (state is null || snapshot is null || context is null)
        {
            return Array.Empty<CandidateProposal>();
        }

        var proposals = new List<CandidateProposal>();

        foreach (var groupId in snapshot.WeakGroupIdsByWeakness)
        {
            var weakness = ComputeGroupWeakness(groupId, snapshot);
            AddProposalsForWeakGroup(groupId, weakness, snapshot, proposals);
        }

        return proposals;
    }

    /// <summary>
    /// חולשת קבוצה = סכום PreferredOutsideCount של חבריה (מדד עקבי עם הבנייה ב-RuntimeDataBuilder).
    /// </summary>
    private static int ComputeGroupWeakness(GroupId groupId, RuntimeDataSnapshot snapshot)
    {
        if (!snapshot.GroupProfiles.TryGetValue(groupId, out var groupProfile))
        {
            return 0;
        }

        var weakness = 0;

        foreach (var memberId in groupProfile.Participants)
        {
            if (snapshot.ParticipantProfiles.TryGetValue(memberId, out var profile))
            {
                weakness += profile.PreferredOutsideCount;
            }
        }

        return weakness;
    }

    private static void AddProposalsForWeakGroup(
        GroupId groupId,
        int weakness,
        RuntimeDataSnapshot snapshot,
        List<CandidateProposal> proposals)
    {
        if (!snapshot.GroupProfiles.TryGetValue(groupId, out var groupProfile))
        {
            return;
        }

        var unsatisfiedMembers = groupProfile.Participants
            .Select(memberId => snapshot.ParticipantProfiles.TryGetValue(memberId, out var profile) ? profile : null)
            .Where(profile => profile is not null && profile.PreferredOutsideCount > 0)
            .OrderByDescending(profile => profile!.PreferredOutsideCount)
            .ThenBy(profile => profile!.ParticipantId.Value, StringComparer.Ordinal)
            .Take(MaxMembersPerGroup);

        foreach (var member in unsatisfiedMembers)
        {
            AddProposalForMember(member!, groupId, weakness, snapshot, proposals);
        }
    }

    private static void AddProposalForMember(
        ParticipantRuntimeProfile member,
        GroupId sourceGroupId,
        int weakness,
        RuntimeDataSnapshot snapshot,
        List<CandidateProposal> proposals)
    {
        var preferredIds = snapshot.PreferenceIndex
            .GetPreferredByParticipant(member.ParticipantId)
            .Select(preference => preference.PreferredParticipantId)
            .ToHashSet();

        if (preferredIds.Count == 0)
        {
            return;
        }

        GroupId? bestTargetGroup = null;
        var bestCount = 0;

        foreach (var preferredId in preferredIds)
        {
            if (!snapshot.ParticipantProfiles.TryGetValue(preferredId, out var preferredProfile))
            {
                continue;
            }

            var candidateGroupId = preferredProfile.CurrentGroupId;
            if (candidateGroupId.Equals(sourceGroupId))
            {
                continue;
            }

            var count = CountPreferredInGroup(preferredIds, candidateGroupId, snapshot);
            if (count > bestCount ||
                (count == bestCount && bestTargetGroup is not null && candidateGroupId.Value < bestTargetGroup.Value.Value))
            {
                bestCount = count;
                bestTargetGroup = candidateGroupId;
            }
        }

        if (bestTargetGroup is null || bestCount == 0)
        {
            return;
        }

        var swapPartner = SwapPartnerSelector.SelectLowestContributionMember(
            bestTargetGroup.Value,
            snapshot,
            preferredIds);

        if (swapPartner is null)
        {
            return;
        }

        var move = MoveCandidate.Swap(
            member.ParticipantId,
            swapPartner.Value,
            sourceGroupId,
            bestTargetGroup.Value);

        var priority = bestCount + (WeaknessWeight * weakness);
        var reason = $"Weak group {sourceGroupId.Value} (weakness {weakness}): member {member.ParticipantId.Value} moved toward {bestCount} preferred peers in group {bestTargetGroup.Value.Value}.";

        proposals.Add(new CandidateProposal(move, CandidateSource.LowScoreGroup, reason, priority));
    }

    private static int CountPreferredInGroup(
        HashSet<ParticipantId> preferredIds,
        GroupId groupId,
        RuntimeDataSnapshot snapshot)
    {
        var count = 0;

        foreach (var preferredId in preferredIds)
        {
            if (snapshot.ParticipantProfiles.TryGetValue(preferredId, out var preferredProfile)
                && preferredProfile.CurrentGroupId.Equals(groupId))
            {
                count++;
            }
        }

        return count;
    }
}
