using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Models;

/// <summary>
/// הצעת איזון מלאה: העברות איסוף של אשכול מפוצל + העברות פיצוי לשמירה על גודל קבוצות.
/// </summary>
public sealed class GroupRebalanceCandidate
{
    public GroupRebalanceCandidate(
        int clusterId,
        IReadOnlyList<ParticipantId> anchorClusterMembers,
        GroupId targetGroupId,
        IReadOnlyList<MoveCandidate> transferMoves,
        CompensationType compensationType,
        string reason,
        double priority)
    {
        if (anchorClusterMembers is null)
        {
            throw new ArgumentNullException(nameof(anchorClusterMembers));
        }

        if (transferMoves is null)
        {
            throw new ArgumentNullException(nameof(transferMoves));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required.", nameof(reason));
        }

        ClusterId = clusterId;
        AnchorClusterMembers = anchorClusterMembers;
        TargetGroupId = targetGroupId;
        TransferMoves = transferMoves;
        CompensationType = compensationType;
        Reason = reason;
        Priority = priority;
    }

    public int ClusterId { get; }

    public IReadOnlyList<ParticipantId> AnchorClusterMembers { get; }

    public GroupId TargetGroupId { get; }

    public IReadOnlyList<MoveCandidate> TransferMoves { get; }

    public CompensationType CompensationType { get; }

    public string Reason { get; }

    public double Priority { get; }
}
