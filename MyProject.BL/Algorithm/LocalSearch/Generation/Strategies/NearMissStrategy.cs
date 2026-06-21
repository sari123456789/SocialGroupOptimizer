using MyProject.Core.Domain.ValueObjects;
using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.BL.Algorithm.LocalSearch.RuntimeData;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.LocalSearch.Generation.Strategies;

/// <summary>
/// תפקיד המחלקה: אסטרטגיית כמעט-שיפור — משתתף עם חלק מהעדפות בקבוצה וחלק בחוץ.
/// המחלקה משתתפת בשלב יצירת מועמדים — מנסה להשלים מימוש העדפות.
/// </summary>
public sealed class NearMissStrategy : IMoveCandidateStrategy
{
    private const double MutualBonus = 2.0;

    /// <inheritdoc />
    public CandidateSource Source => CandidateSource.NearMiss;

    /// <inheritdoc />
    public bool IsApplicable(RuntimeDataSnapshot snapshot, MoveGenerationContext context)
    {
        if (snapshot is null || context is null)
        {
            return false;
        }

        return snapshot.NearMissParticipantIds.Count > 0;
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

        foreach (var nearMissId in snapshot.NearMissParticipantIds)
        {
            if (!snapshot.ParticipantProfiles.TryGetValue(nearMissId, out var nearMiss))
            {
                continue;
            }

            AddProposalsForNearMiss(nearMiss, snapshot, proposals);
        }

        return proposals;
    }

    private static void AddProposalsForNearMiss(
        ParticipantRuntimeProfile nearMiss,
        RuntimeDataSnapshot snapshot,
        List<CandidateProposal> proposals)
    {
        var preferences = snapshot.PreferenceIndex.GetPreferredByParticipant(nearMiss.ParticipantId);

        var insidePreferredIds = new HashSet<ParticipantId> { nearMiss.ParticipantId };

        foreach (var preference in preferences)
        {
            var preferredId = preference.PreferredParticipantId;
            if (snapshot.ParticipantProfiles.TryGetValue(preferredId, out var preferredProfile)
                && preferredProfile.CurrentGroupId.Equals(nearMiss.CurrentGroupId))
            {
                insidePreferredIds.Add(preferredId);
            }
        }

        foreach (var preference in preferences)
        {
            var preferredId = preference.PreferredParticipantId;

            if (!snapshot.ParticipantProfiles.TryGetValue(preferredId, out var preferredProfile))
            {
                continue;
            }

            var preferredGroupId = preferredProfile.CurrentGroupId;

            if (preferredGroupId.Equals(nearMiss.CurrentGroupId))
            {
                continue;
            }

            var swapPartner = SwapPartnerSelector.SelectLowestContributionMember(
                nearMiss.CurrentGroupId,
                snapshot,
                insidePreferredIds);

            if (swapPartner is null)
            {
                continue;
            }

            var move = MoveCandidate.Swap(
                preferredId,
                swapPartner.Value,
                preferredGroupId,
                nearMiss.CurrentGroupId);

            var isMutual = snapshot.MutualPreferenceIndex.AreMutual(nearMiss.ParticipantId, preferredId);

            var rankValue = 1.0 / Math.Max(1, preference.Rank);
            var priority = (isMutual ? MutualBonus : 0.0) + rankValue;

            var reason = $"Near-miss {nearMiss.ParticipantId.Value} pulls preferred {preferredId.Value} (rank {preference.Rank}{(isMutual ? ", mutual" : string.Empty)}) into group {nearMiss.CurrentGroupId.Value}.";

            proposals.Add(new CandidateProposal(move, CandidateSource.NearMiss, reason, priority));
        }
    }
}
