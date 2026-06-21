using MyProject.Core.Domain.ValueObjects;
using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.BL.Algorithm.LocalSearch.RuntimeData;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.LocalSearch.Generation.Strategies;

/// <summary>
/// תפקיד המחלקה: אסטרטגיית משתתפים מבודדים — מזהה מי בלי אף העדפה ממומשת בקבוצה.
/// המחלקה משתתפת בשלב יצירת מועמדים — מציעה Swap — החלפה — לכיוון קבוצות עם העדפות.
/// </summary>
public sealed class IsolatedParticipantStrategy : IMoveCandidateStrategy
{
    /// <inheritdoc />
    public CandidateSource Source => CandidateSource.IsolatedParticipant;

    /// <inheritdoc />
    public bool IsApplicable(RuntimeDataSnapshot snapshot, MoveGenerationContext context)
    {
        if (snapshot is null || context is null)
        {
            return false;
        }

        return snapshot.IsolatedParticipantIds.Count > 0;
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

        foreach (var isolatedId in snapshot.IsolatedParticipantIds)
        {
            if (!snapshot.ParticipantProfiles.TryGetValue(isolatedId, out var isolated))
            {
                continue;
            }

            AddProposalsForIsolated(isolated, snapshot, proposals);
        }

        return proposals;
    }

    private static void AddProposalsForIsolated(
        ParticipantRuntimeProfile isolated,
        RuntimeDataSnapshot snapshot,
        List<CandidateProposal> proposals)
    {
        var preferredIds = snapshot.PreferenceIndex
            .GetPreferredByParticipant(isolated.ParticipantId)
            .Select(preference => preference.PreferredParticipantId)
            .ToHashSet();

        if (preferredIds.Count == 0)
        {
            return;
        }

        var preferredCountByTargetGroup = CountPreferredByTargetGroup(isolated, preferredIds, snapshot);

        foreach (var (targetGroupId, preferredCount) in preferredCountByTargetGroup)
        {
            var swapPartner = SelectSwapPartner(targetGroupId, preferredIds, snapshot);
            if (swapPartner is null)
            {
                continue;
            }

            var move = MoveCandidate.Swap(
                isolated.ParticipantId,
                swapPartner.Value,
                isolated.CurrentGroupId,
                targetGroupId);

            var priority = preferredCount;
            var reason = $"Isolated participant {isolated.ParticipantId.Value} moved toward {preferredCount} preferred peers in group {targetGroupId.Value}.";

            proposals.Add(new CandidateProposal(move, CandidateSource.IsolatedParticipant, reason, priority));
        }
    }

    private static Dictionary<GroupId, int> CountPreferredByTargetGroup(
        ParticipantRuntimeProfile isolated,
        HashSet<ParticipantId> preferredIds,
        RuntimeDataSnapshot snapshot)
    {
        var counts = new Dictionary<GroupId, int>();

        foreach (var preferredId in preferredIds)
        {
            if (!snapshot.ParticipantProfiles.TryGetValue(preferredId, out var preferredProfile))
            {
                continue;
            }

            var preferredGroupId = preferredProfile.CurrentGroupId;

            if (preferredGroupId.Equals(isolated.CurrentGroupId))
            {
                continue;
            }

            counts.TryGetValue(preferredGroupId, out var current);
            counts[preferredGroupId] = current + 1;
        }

        return counts;
    }

    private static ParticipantId? SelectSwapPartner(
        GroupId targetGroupId,
        HashSet<ParticipantId> preferredIds,
        RuntimeDataSnapshot snapshot)
    {
        if (!snapshot.GroupProfiles.TryGetValue(targetGroupId, out var targetGroup))
        {
            return null;
        }

        ParticipantId? bestPartner = null;
        var bestInsideCount = int.MaxValue;

        foreach (var memberId in targetGroup.Participants)
        {
            if (preferredIds.Contains(memberId))
            {
                continue;
            }

            var insideCount = snapshot.ParticipantProfiles.TryGetValue(memberId, out var memberProfile)
                ? memberProfile.PreferredInsideCount
                : 0;

            if (insideCount < bestInsideCount ||
                (insideCount == bestInsideCount &&
                 bestPartner is not null &&
                 string.CompareOrdinal(memberId.Value, bestPartner.Value.Value) < 0))
            {
                bestInsideCount = insideCount;
                bestPartner = memberId;
            }
        }

        return bestPartner;
    }
}
