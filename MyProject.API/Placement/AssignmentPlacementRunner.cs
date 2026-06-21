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
// שירות שמריץ חלוקה ראשונית ואז שיפור
public sealed class AssignmentPlacementRunner
{
    // מתזמר החלוקה הראשונית
    private readonly InitialPlacementOrchestrator _placementOrchestrator;
    // מתזמר השיפור (חיפוש מקומי ואיזון קבוצות)
    private readonly IAssignmentImprovementOrchestrator _improvementOrchestrator;
    // משקלות לניקוד בשלב השיפור
    private readonly ScoringWeights _scoringWeights;
    // לוגים לרישום אזהרות
    private readonly ILogger<AssignmentPlacementRunner> _logger;

    /// <summary>
    /// יוצר runner עם תלויות מוזרקות.
    /// </summary>
    // בונה את השירות עם כל התלויות
    public AssignmentPlacementRunner(
        InitialPlacementOrchestrator placementOrchestrator,// מתזמר החלוקה הראשונית
        IAssignmentImprovementOrchestrator improvementOrchestrator,//מתזמר שיפור
        ScoringWeights scoringWeights,// משקלות לניקוד
        ILogger<AssignmentPlacementRunner> logger)// לוגים
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
        // בודקים שהקלט לא ריק
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        // שלב 1 — מריצים חלוקה ראשונית
        var placementResult = _placementOrchestrator.Run(input);

        // אם החלוקה לא הצליחה — מחזירים בלי שיפור
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

        // הצלחה אבל בלי חלוקה — מצב חריג
        if (placementResult.Assignment is null)
        {
            // הודעת אזהרה קבועה
            const string missingAssignmentWarning = "Initial placement succeeded but no assignment was returned.";
            // רושמים אזהרה בלוג
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

        // מכינים קלט לשלב השיפור
        var improvementInput = new AssignmentImprovementInput(
            placementResult.Assignment, // החלוקה הראשונית שהתקבלה
            input.Participants, // רשימת המשתתפים מהקלט
            input.Constraints, // רשימת המגבלות מהקלט
            _scoringWeights); // משקלות לניקוד מהתלויות

        // מנסים לשפר את החלוקה
        try
        {
            // שלב 2 — חיפוש מקומי ואיזון קבוצות
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
        // תופסים כשלון באימות אחרי השיפור
        catch (InvalidOperationException ex)
        {
            // חוזרים לחלוקה הראשונית אם השיפור נכשל
            const string fallbackWarning =
                "Local search improvement failed final validation; using initial placement assignment.";

            // רושמים אזהרה עם פרטי השגיאה
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