using MyProject.BL.Algorithm.InitialPlacement; // MandatoryUnitMap
using MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Models;
using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.BL.Algorithm.SolutionState;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Planning;

/// <summary>
/// בונה חבילות פיצוי: מעביר משתתפים מקבוצת היעד לקבוצות שאיבדו חברים באיסוף האשכול.
/// שומר על גודל קבוצות — לכל יוצא יש יעד מתאים.
/// </summary>
public static class CompensationPlanner
{
    private const int DefaultMaxPackages = 4;

    public static IReadOnlyList<IReadOnlyList<MoveCandidate>> BuildCompensationPackages(
        AssignmentState state,
        GroupId targetGroupId,
        IReadOnlyList<ParticipantId> anchorClusterMembers,
        IReadOnlyList<GroupDelta> gatheringDeltas,
        IReadOnlyDictionary<GroupId, IReadOnlyList<ParticipantId>>? lowContributionByGroup,
        MandatoryUnitMap? mandatoryUnits,
        int maxPackages = DefaultMaxPackages)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (anchorClusterMembers is null)
        {
            throw new ArgumentNullException(nameof(anchorClusterMembers));
        }

        if (gatheringDeltas is null)
        {
            throw new ArgumentNullException(nameof(gatheringDeltas));
        }

        var anchorSet = anchorClusterMembers.ToHashSet();
        // קבוצות שאיבדו משתתפים באיסוף — צריכות לקבל פיצוי מאותו מספר.
        var recipientNeeds = gatheringDeltas
            .Where(delta => delta.Delta < 0 && !delta.GroupId.Equals(targetGroupId))
            .OrderBy(delta => delta.GroupId.Value)
            .Select(delta => (GroupId: delta.GroupId, Count: -delta.Delta))
            .ToList();

        if (recipientNeeds.Count == 0)
        {
            return Array.Empty<IReadOnlyList<MoveCandidate>>();
        }

        var totalNeeded = recipientNeeds.Sum(need => need.Count);
        // תורמים אפשריים: חברי קבוצת היעד שאינם חלק מהאשכול ולא יפצלו יחידת חובה.
        var availableDonors = GetAvailableDonors(state, targetGroupId, anchorSet, mandatoryUnits);

        if (availableDonors.Count < totalNeeded)
        {
            return Array.Empty<IReadOnlyList<MoveCandidate>>();
        }

        // מנסים כמה סדרי בחירה של תורמים — מעדיפים תחילה משתתפים עם תרומה חברתית נמוכה.
        var donorOrderings = BuildDonorOrderings(availableDonors, targetGroupId, lowContributionByGroup, maxPackages);
        var packages = new List<IReadOnlyList<MoveCandidate>>();

        foreach (var donorOrdering in donorOrderings)
        {
            var package = BuildPackageFromOrdering(donorOrdering, targetGroupId, recipientNeeds);
            if (package.Count == totalNeeded)
            {
                packages.Add(package);
            }
        }

        return packages;
    }

    private static List<ParticipantId> GetAvailableDonors(
        AssignmentState state,
        GroupId targetGroupId,
        HashSet<ParticipantId> anchorSet,
        MandatoryUnitMap? mandatoryUnits)
    {
        if (!state.GroupToParticipants.TryGetValue(targetGroupId, out var targetMembers))
        {
            return new List<ParticipantId>();
        }

        var donors = new List<ParticipantId>();

        foreach (var participantId in targetMembers)
        {
            if (anchorSet.Contains(participantId))
            {
                continue;
            }

            if (WouldSplitMandatoryUnit(participantId, mandatoryUnits))
            {
                // לא מוציאים משתתף שיפריד יחידת חובה (זוג MustLink וכדומה).
                continue;
            }

            donors.Add(participantId);
        }

        return donors;
    }

    private static bool WouldSplitMandatoryUnit(ParticipantId participantId, MandatoryUnitMap? mandatoryUnits)
    {
        if (mandatoryUnits is null)
        {
            return false;
        }

        if (!mandatoryUnits.TryGetUnitId(participantId, out var unitId))
        {
            return false;
        }

        return mandatoryUnits.Units.TryGetValue(unitId, out var members) && members.Count > 1;
    }

    private static List<List<ParticipantId>> BuildDonorOrderings(
        IReadOnlyList<ParticipantId> availableDonors,
        GroupId targetGroupId,
        IReadOnlyDictionary<GroupId, IReadOnlyList<ParticipantId>>? lowContributionByGroup,
        int maxPackages)
    {
        var orderings = new List<List<ParticipantId>>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void TryAdd(IEnumerable<ParticipantId> ordering)
        {
            var list = ordering.ToList();
            var key = string.Join("|", list.Select(id => id.Value));
            if (seen.Add(key))
            {
                orderings.Add(list);
            }
        }

        TryAdd(OrderByLowContributionFirst(availableDonors, targetGroupId, lowContributionByGroup));
        TryAdd(OrderByLowContributionFirst(availableDonors, targetGroupId, lowContributionByGroup).AsEnumerable().Reverse());
        TryAdd(availableDonors.OrderBy(id => id.Value, StringComparer.Ordinal));
        TryAdd(availableDonors.OrderByDescending(id => id.Value, StringComparer.Ordinal));

        return orderings.Take(maxPackages).ToList();
    }

    private static IEnumerable<ParticipantId> OrderByLowContributionFirst(
        IReadOnlyList<ParticipantId> availableDonors,
        GroupId targetGroupId,
        IReadOnlyDictionary<GroupId, IReadOnlyList<ParticipantId>>? lowContributionByGroup)
    {
        if (lowContributionByGroup is null
            || !lowContributionByGroup.TryGetValue(targetGroupId, out var rankedLowContribution))
        {
            return availableDonors.OrderBy(id => id.Value, StringComparer.Ordinal);
        }

        var rank = new Dictionary<ParticipantId, int>();
        for (var index = 0; index < rankedLowContribution.Count; index++)
        {
            rank.TryAdd(rankedLowContribution[index], index);
        }

        return availableDonors
            .OrderBy(id => rank.TryGetValue(id, out var position) ? position : int.MaxValue)
            .ThenBy(id => id.Value, StringComparer.Ordinal);
    }

    private static List<MoveCandidate> BuildPackageFromOrdering(
        IReadOnlyList<ParticipantId> donorOrdering,
        GroupId targetGroupId,
        IReadOnlyList<(GroupId GroupId, int Count)> recipientNeeds)
    {
        var moves = new List<MoveCandidate>();
        var donorIndex = 0;

        foreach (var (destinationGroupId, count) in recipientNeeds)
        {
            for (var i = 0; i < count; i++)
            {
                if (donorIndex >= donorOrdering.Count)
                {
                    return moves;
                }

                var donorId = donorOrdering[donorIndex++];
                moves.Add(MoveCandidate.Transfer(donorId, targetGroupId, destinationGroupId));
            }
        }

        return moves;
    }
}