using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.BL.Algorithm.LocalSearch.RuntimeData;
using MyProject.BL.Algorithm.SolutionState;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Planning;

/// <summary>
/// בונה העברות שמאגדות את כל חברי אשכול מפוצל לקבוצת יעד אחת.
/// </summary>
public static class ClusterGatheringPlanner
{
    public static IReadOnlyList<MoveCandidate> BuildGatheringTransfers(
        AssignmentState state,
        ClosedFriendGroupProfile profile,
        GroupId targetGroupId)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        var transfers = new List<MoveCandidate>();

        foreach (var memberId in profile.Members)
        {
            if (!state.ParticipantToGroup.TryGetValue(memberId, out var currentGroupId))
            {
                continue;
            }

            // כבר בקבוצת היעד — אין צורך בהעברה.
            if (currentGroupId.Equals(targetGroupId))
            {
                continue;
            }

            transfers.Add(MoveCandidate.Transfer(memberId, currentGroupId, targetGroupId));
        }

        return transfers;
    }
}
