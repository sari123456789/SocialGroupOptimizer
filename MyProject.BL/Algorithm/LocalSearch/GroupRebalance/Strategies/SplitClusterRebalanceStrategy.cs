using MyProject.BL.Algorithm.InitialPlacement;
using MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Evaluation;
using MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Models;
using MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Planning;
using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.BL.Algorithm.LocalSearch.RuntimeData;
using MyProject.BL.Algorithm.SolutionState;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Strategies;

/// <summary>
/// אסטרטגיית איזון קבוצות: אוספת אשכול חברים מפוצל לקבוצת יעד אחת, עם פיצוי לשמירת גודל.
/// מופעלת כ-fallback אחרי שחיפוש מקומי נעצר ללא שיפור נוסף.
/// </summary>
public sealed class SplitClusterRebalanceStrategy
{
    private readonly GroupRebalanceEvaluator _evaluator;

    public SplitClusterRebalanceStrategy(GroupRebalanceEvaluator evaluator)
    {
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public GroupRebalanceResult? TryFindBestImprovement(
        AssignmentState currentState,
        RuntimeDataSnapshot snapshot,
        GroupRebalanceEvaluationContext context,
        MandatoryUnitMap? mandatoryUnits = null) =>
        SearchForBestImprovement(currentState, snapshot, context, mandatoryUnits).BestResult;

    public GroupRebalanceSearchOutcome SearchForBestImprovement(
        AssignmentState currentState,
        RuntimeDataSnapshot snapshot,
        GroupRebalanceEvaluationContext context,
        MandatoryUnitMap? mandatoryUnits = null)
    {
        if (currentState is null)
        {
            throw new ArgumentNullException(nameof(currentState));
        }

        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var deduplicator = new GroupRebalanceDeduplicator();
        GroupRebalanceResult? bestResult = null;
        var splitClusterCount = 0;
        var candidatesEvaluated = 0;

        foreach (var profile in snapshot.ClosedFriendGroupProfiles)
        {
            // מטפלים רק באשכולות שחבריהם מפוזרים על יותר מקבוצה אחת.
            if (!profile.IsSplitAcrossGroups)
            {
                continue;
            }

            splitClusterCount++;
            // קבוצת יעד מועדפת: זו שכבר מכילה הכי הרבה חברי האשכול.
            var targetGroups = RankTargetGroups(currentState, profile);

            foreach (var targetGroupId in targetGroups)
            {
                var gatheringMoves = ClusterGatheringPlanner.BuildGatheringTransfers(
                    currentState,
                    profile,
                    targetGroupId);

                if (gatheringMoves.Count == 0)
                {
                    continue;
                }

                var gatheringDeltas = GroupDeltaCalculator.Calculate(gatheringMoves);
                // פיצוי: מוציאים מקבוצת היעד את מי שאיבדו בקבוצות אחרות.
                var compensationPackages = CompensationPlanner.BuildCompensationPackages(
                    currentState,
                    targetGroupId,
                    profile.Members,
                    gatheringDeltas,
                    snapshot.LowContributionParticipantIdsByGroup,
                    mandatoryUnits);

                foreach (var compensationMoves in compensationPackages)
                {
                    var allMoves = CombineMoves(gatheringMoves, compensationMoves);
                    var compensationType = ResolveCompensationType(compensationMoves.Count);

                    var candidate = new GroupRebalanceCandidate(
                        profile.ClusterId,
                        profile.Members,
                        targetGroupId,
                        allMoves,
                        compensationType,
                        reason: $"Gather split cluster {profile.ClusterId} into group {targetGroupId.Value}",
                        priority: profile.DensityScore);

                    if (!deduplicator.TryRegister(candidate))
                    {
                        continue;
                    }

                    candidatesEvaluated++;
                    var result = _evaluator.Evaluate(currentState, candidate, context);

                    if (!result.IsValid || !result.IsImproving)
                    {
                        continue;
                    }

                    if (bestResult is null || result.ScoreDelta > bestResult.ScoreDelta)
                    {
                        bestResult = result;
                    }
                }
            }
        }

        return new GroupRebalanceSearchOutcome(bestResult, splitClusterCount, candidatesEvaluated);
    }

    private static IReadOnlyList<GroupId> RankTargetGroups(
        AssignmentState state,
        ClosedFriendGroupProfile profile)
    {
        var memberCountByGroup = new Dictionary<GroupId, int>();

        foreach (var memberId in profile.Members)
        {
            if (!state.ParticipantToGroup.TryGetValue(memberId, out var groupId))
            {
                continue;
            }

            memberCountByGroup.TryGetValue(groupId, out var count);
            memberCountByGroup[groupId] = count + 1;
        }

        return memberCountByGroup
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key.Value)
            .Select(entry => entry.Key)
            .ToList();
    }

    private static IReadOnlyList<MoveCandidate> CombineMoves(
        IReadOnlyList<MoveCandidate> gatheringMoves,
        IReadOnlyList<MoveCandidate> compensationMoves)
    {
        var combined = new List<MoveCandidate>(gatheringMoves.Count + compensationMoves.Count);
        combined.AddRange(gatheringMoves);
        combined.AddRange(compensationMoves);
        return combined;
    }

    private static CompensationType ResolveCompensationType(int compensationMoveCount)
    {
        if (compensationMoveCount == 0)
        {
            return CompensationType.Individuals;
        }

        return CompensationType.Mixed;
    }
}
