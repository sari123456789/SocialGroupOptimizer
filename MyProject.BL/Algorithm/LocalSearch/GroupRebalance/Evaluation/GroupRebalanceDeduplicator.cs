using System.Text;
using MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Models;
using MyProject.BL.Algorithm.LocalSearch.Moves;

namespace MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Evaluation;

/// <summary>
/// מונע הערכה כפולה של אותה רצף העברות — חתימה יציבה לפי משתתף ומקור/יעד.
/// </summary>
public sealed class GroupRebalanceDeduplicator
{
    private readonly HashSet<string> _seenSignatures = new(StringComparer.Ordinal);

    public bool TryRegister(GroupRebalanceCandidate candidate)
    {
        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }

        var signature = BuildSignature(candidate.TransferMoves);
        return _seenSignatures.Add(signature);
    }

    public static string BuildSignature(IReadOnlyList<MoveCandidate> transferMoves)
    {
        if (transferMoves is null)
        {
            throw new ArgumentNullException(nameof(transferMoves));
        }

        var builder = new StringBuilder();

        foreach (var move in transferMoves
                     .OrderBy(m => m.FirstParticipantId.Value, StringComparer.Ordinal))
        {
            if (builder.Length > 0)
            {
                builder.Append(';');
            }

            builder.Append(move.FirstParticipantId.Value)
                .Append(':')
                .Append(move.SourceGroupId.Value)
                .Append("->")
                .Append(move.TargetGroupId.Value);
        }

        return builder.ToString();
    }
}
