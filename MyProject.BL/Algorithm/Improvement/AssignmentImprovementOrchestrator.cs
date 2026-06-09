using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Services;
using MyProject.BL.Algorithm.Initialization;
using MyProject.BL.Algorithm.LocalSearch.Engine;
using MyProject.BL.Algorithm.LocalSearch.State;
using MyProject.BL.Logic.Configuration;
using MyProject.BL.Logic.Scoring;

namespace MyProject.BL.Algorithm.Improvement;

/// <summary>
/// מתזמר שיפור חלוקה — גשר בין Initial Placement ל-LocalSearchEngine.
/// </summary>
public sealed class AssignmentImprovementOrchestrator : IAssignmentImprovementOrchestrator
{
    private readonly ILocalSearchEngine _localSearchEngine;
    private readonly IAssignmentScorer _scorer;
    private readonly IAssignmentValidator _validator;
    private readonly AlgorithmSettings _settings;

    /// <summary>
    /// יוצר מתזמר עם תלויות מוזרקות.
    /// </summary>
    public AssignmentImprovementOrchestrator(
        ILocalSearchEngine localSearchEngine,
        IAssignmentScorer scorer,
        IAssignmentValidator validator,
        AlgorithmSettings settings)
    {
        _localSearchEngine = localSearchEngine ?? throw new ArgumentNullException(nameof(localSearchEngine));
        _scorer = scorer ?? throw new ArgumentNullException(nameof(scorer));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    public AssignmentImprovementResult Improve(AssignmentImprovementInput input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var assignment = input.InitialAssignment;
        var initialScore = _scorer.CalculateScore(
            assignment,
            input.Participants,
            input.ScoringWeights);

        if (!_settings.EnableLocalSearch)
        {
            return AssignmentImprovementResult.Skipped(assignment, initialScore);
        }

        var state = AssignmentStateFactory.CreateFromAssignment(assignment);
        state.CurrentScore = initialScore;

        var localSearchInput = new LocalSearchInput(
            state,
            input.Participants,
            input.Constraints,
            input.ScoringWeights,
            initialScore);

        var searchResult = _localSearchEngine.Improve(localSearchInput);
        var improvedAssignment = AssignmentStateConverter.ToAssignment(searchResult.FinalState);

        if (!_validator.IsValid(improvedAssignment, input.Constraints, out var errors))
        {
            var message = errors.Count > 0
                ? string.Join("; ", errors)
                : "Improved assignment failed final constraint validation.";
            throw new InvalidOperationException(message);
        }

        return AssignmentImprovementResult.Improved(improvedAssignment, searchResult);
    }
}
