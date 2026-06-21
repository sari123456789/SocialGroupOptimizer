using MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Models;
using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Planning;

/// <summary>
/// מחשב לכל קבוצה את השינוי הנטו במספר המשתתפים מרצף העברות.
/// </summary>
public static class GroupDeltaCalculator
{
    public static IReadOnlyList<GroupDelta> Calculate(IReadOnlyList<MoveCandidate> transferMoves)
    {
        if (transferMoves is null)
        {
            throw new ArgumentNullException(nameof(transferMoves));
        }

        var deltaByGroup = new Dictionary<GroupId, int>();

        foreach (var move in transferMoves)
        {
            if (move.MoveType != MoveType.Transfer)
            {
                throw new ArgumentException("Group rebalance planning expects Transfer moves only.", nameof(transferMoves));
            }

            // מקור מאבד משתתף; יעד מרוויח — סיכום נטו לכל קבוצה.
            deltaByGroup.TryGetValue(move.SourceGroupId, out var sourceDelta);
            deltaByGroup[move.SourceGroupId] = sourceDelta - 1;

            deltaByGroup.TryGetValue(move.TargetGroupId, out var targetDelta);
            deltaByGroup[move.TargetGroupId] = targetDelta + 1;
        }

        return deltaByGroup
            .Where(entry => entry.Value != 0)
            .OrderBy(entry => entry.Key.Value)
            .Select(entry => new GroupDelta(entry.Key, entry.Value))
            .ToList();
    }
}
