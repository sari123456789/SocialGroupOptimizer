using MyProject.Core.Domain.ValueObjects;
using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.BL.Algorithm.LocalSearch.RuntimeData;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.LocalSearch.Generation.Strategies;

/// <summary>
/// אסטרטגיית תרומה נמוכה — מחליף משתתף שתורם מעט עם מבקש מבחוץ לקבוצה.
/// </summary>
public sealed class LowContributionStrategy : IMoveCandidateStrategy
{
    /// <inheritdoc />
    public CandidateSource Source => CandidateSource.LowContribution;

    /// <inheritdoc />
    public bool IsApplicable(RuntimeDataSnapshot snapshot, MoveGenerationContext context)
    {
        if (snapshot is null || context is null)
        {
            return false;
        }

        return snapshot.LowContributionParticipantIdsByGroup.Count > 0;
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

        foreach (var (groupId, participantIds) in snapshot.LowContributionParticipantIdsByGroup)
        {
            if (!snapshot.GroupProfiles.TryGetValue(groupId, out var groupProfile))
            {
                continue;
            }

            foreach (var participantId in participantIds)
            {
                AddProposalForLowContributor(participantId, groupId, groupProfile, snapshot, proposals);
            }
        }

        return proposals;
    }

    /// <summary>
    /// מוסיף הצעת החלפה עבור משתתף בעל תרומה נמוכה — מול המבקש הטוב ביותר מבחוץ.
    /// </summary>
    private static void AddProposalForLowContributor(
        ParticipantId contributorId,
        GroupId groupId,
        GroupRuntimeProfile groupProfile,
        RuntimeDataSnapshot snapshot,
        List<CandidateProposal> proposals)
    {
        var satisfiedPreferredByWanter = CountExternalWantersGain(contributorId, groupId, groupProfile, snapshot);
        if (satisfiedPreferredByWanter.Count == 0)
        {
            return;
        }

        var bestWanter = satisfiedPreferredByWanter
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key.Value, StringComparer.Ordinal)
            .First();

        if (!snapshot.ParticipantProfiles.TryGetValue(bestWanter.Key, out var wanterProfile))
        {
            return;
        }

        var move = MoveCandidate.Swap(
            contributorId,
            bestWanter.Key,
            groupId,
            wanterProfile.CurrentGroupId);

        var priority = bestWanter.Value;
        var reason = $"Low-contribution {contributorId.Value} swapped out for {bestWanter.Key.Value}, who gains {bestWanter.Value} preferred peers in group {groupId.Value}.";

        proposals.Add(new CandidateProposal(move, CandidateSource.LowContribution, reason, priority));
    }

    /// <summary>
    /// סופר לכל מבקש-מבחוץ כמה מחברי הקבוצה הוא מעדיף.
    /// </summary>
    private static Dictionary<ParticipantId, int> CountExternalWantersGain(
        ParticipantId contributorId,
        GroupId groupId,
        GroupRuntimeProfile groupProfile,
        RuntimeDataSnapshot snapshot)
    {
        var gainByWanter = new Dictionary<ParticipantId, int>();

        foreach (var memberId in groupProfile.Participants)
        {
            if (memberId.Equals(contributorId))
            {
                continue;
            }

            foreach (var wanterId in snapshot.PreferenceIndex.GetParticipantsWhoPrefer(memberId))
            {
                if (!snapshot.ParticipantProfiles.TryGetValue(wanterId, out var wanterProfile))
                {
                    continue;
                }

                if (wanterProfile.CurrentGroupId.Equals(groupId))
                {
                    continue;
                }

                gainByWanter.TryGetValue(wanterId, out var current);
                gainByWanter[wanterId] = current + 1;
            }
        }

        return gainByWanter;
    }
}
