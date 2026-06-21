using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using MyProject.BL.Logic.Configuration;

namespace MyProject.BL.Algorithm.Improvement;

/// <summary>
/// קלט לשיפור חלוקה — חלוקה ראשונית חוקית, משתתפים, אילוצים ומשקלות.
/// </summary>
public sealed class AssignmentImprovementInput
{
    /// <summary>
    /// יוצר קלט שיפור.
    /// </summary>
    public AssignmentImprovementInput(
        Assignment initialAssignment, // חלוקה ראשונית חוקית מ-Initial Placement
        IReadOnlyList<Participant> participants, // כל המשתתפים — לניקוד ול-Local Search
        IReadOnlyList<IConstraint> constraints, // אילוצים — ל-Local Search ולאימות סופי
        ScoringWeights scoringWeights) // משקלות ניקוד
    {
        InitialAssignment = initialAssignment ?? throw new ArgumentNullException(nameof(initialAssignment));
        Participants = participants ?? throw new ArgumentNullException(nameof(participants));

        if (participants.Count == 0)
        {
            throw new ArgumentException("Participants must not be empty.", nameof(participants));
        }

        Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
        ScoringWeights = scoringWeights ?? throw new ArgumentNullException(nameof(scoringWeights));
    }

    /// <summary>חלוקה ראשונית חוקית מ-Initial Placement.</summary>
    public Assignment InitialAssignment { get; }

    /// <summary>כל המשתתפים — לניקוד ול-Local Search.</summary>
    public IReadOnlyList<Participant> Participants { get; }

    /// <summary>אילוצים — ל-Local Search ולאימות סופי.</summary>
    public IReadOnlyList<IConstraint> Constraints { get; }

    /// <summary>משקלות ניקוד.</summary>
    public ScoringWeights ScoringWeights { get; }
}
