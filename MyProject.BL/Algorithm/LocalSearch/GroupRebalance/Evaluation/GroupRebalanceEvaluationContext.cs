using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using MyProject.BL.Logic.Configuration;

namespace MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Evaluation;

/// <summary>
/// קלט משותף להערכת הצעות איזון — משתתפים, אילוצים ומשקלות ניקוד.
/// </summary>
public sealed class GroupRebalanceEvaluationContext
{
    public GroupRebalanceEvaluationContext(
        IReadOnlyList<Participant> participants,
        IReadOnlyList<IConstraint> constraints,
        ScoringWeights scoringWeights)
    {
        Participants = participants ?? throw new ArgumentNullException(nameof(participants));
        Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
        ScoringWeights = scoringWeights ?? throw new ArgumentNullException(nameof(scoringWeights));
    }

    public IReadOnlyList<Participant> Participants { get; }

    public IReadOnlyList<IConstraint> Constraints { get; }

    public ScoringWeights ScoringWeights { get; }
}
