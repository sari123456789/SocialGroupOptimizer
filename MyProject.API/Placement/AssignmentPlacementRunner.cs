using MyProject.BL.Algorithm.Improvement;
using MyProject.BL.Algorithm.InitialPlacement;
using MyProject.BL.Algorithm.InitialPlacement.Orchestration;
using MyProject.BL.Algorithm.InitialPlacement.Results;
using MyProject.BL.Logic.Configuration;
using MyProject.Core.Domain.Entities;

namespace MyProject.API.Placement;

/// <summary>
/// מריץ Initial Placement ואחריו שיפור Local Search על חלוקה מוצלחת.
/// </summary>
public sealed class AssignmentPlacementRunner
{
    private readonly InitialPlacementOrchestrator _placementOrchestrator;
    private readonly IAssignmentImprovementOrchestrator _improvementOrchestrator;
    private readonly ScoringWeights _scoringWeights;
    private readonly ILogger<AssignmentPlacementRunner> _logger;

    /// <summary>
    /// יוצר runner עם תלויות מוזרקות.
    /// </summary>
    public AssignmentPlacementRunner(
        InitialPlacementOrchestrator placementOrchestrator,
        IAssignmentImprovementOrchestrator improvementOrchestrator,
        ScoringWeights scoringWeights,
        ILogger<AssignmentPlacementRunner> logger)
    {
        _placementOrchestrator = placementOrchestrator ?? throw new ArgumentNullException(nameof(placementOrchestrator));
        _improvementOrchestrator = improvementOrchestrator ?? throw new ArgumentNullException(nameof(improvementOrchestrator));
        _scoringWeights = scoringWeights ?? throw new ArgumentNullException(nameof(scoringWeights));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// מריץ חלוקה ראשונית ואם הצליח — שיפור מקומי.
    /// </summary>
    public AssignmentPlacementRunResult RunAndImprove(InitialPlacementInput input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var placementResult = _placementOrchestrator.Run(input);

        if (placementResult.Status is not (InitialPlacementStatus.Success or InitialPlacementStatus.SuccessViaSolver))
        {
            return new AssignmentPlacementRunResult
            {
                PlacementResult = placementResult,
                FinalAssignment = null,
                ImprovementResult = null,
                WasImprovementAttempted = false,
                UsedFallback = false,
            };
        }

        if (placementResult.Assignment is null)
        {
            const string missingAssignmentWarning = "Initial placement succeeded but no assignment was returned.";
            _logger.LogWarning(missingAssignmentWarning);

            return new AssignmentPlacementRunResult
            {
                PlacementResult = placementResult,
                FinalAssignment = null,
                ImprovementResult = null,
                WasImprovementAttempted = false,
                UsedFallback = false,
                Warning = missingAssignmentWarning,
            };
        }

        var improvementInput = new AssignmentImprovementInput(
            placementResult.Assignment,
            input.Participants,
            input.Constraints,
            _scoringWeights);

        try
        {
            var improvement = _improvementOrchestrator.Improve(improvementInput);

            return new AssignmentPlacementRunResult
            {
                PlacementResult = placementResult,
                FinalAssignment = improvement.Assignment,
                ImprovementResult = improvement,
                WasImprovementAttempted = true,
                UsedFallback = false,
            };
        }
        catch (InvalidOperationException ex)
        {
            const string fallbackWarning =
                "Local search improvement failed final validation; using initial placement assignment.";

            _logger.LogWarning(
                ex,
                "{FallbackWarning} Details: {Details}",
                fallbackWarning,
                ex.Message);

            return new AssignmentPlacementRunResult
            {
                PlacementResult = placementResult,
                FinalAssignment = placementResult.Assignment,
                ImprovementResult = null,
                WasImprovementAttempted = true,
                UsedFallback = true,
                Warning = fallbackWarning,
            };
        }
    }
}
