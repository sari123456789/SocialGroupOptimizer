using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Services;
using MyProject.Core.Domain.ValueObjects;
using MyProject.BL.Algorithm.Initialization;
using MyProject.BL.Algorithm.LocalSearch.Engine;
using MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Evaluation;
using MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Models;
using MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Strategies;
using MyProject.BL.Algorithm.LocalSearch.RuntimeData;
using MyProject.BL.Algorithm.LocalSearch.State;
using MyProject.BL.Algorithm.SolutionState;
using MyProject.BL.Logic.Configuration;
using MyProject.BL.Logic.Scoring;


namespace MyProject.BL.Algorithm.Improvement;

/// <summary>

/// תפקיד המחלקה: תזמור שלב שיפור החלוקה אחרי השיבוץ הראשוני.
/// המחלקה משתתפת בשלב השיפור — מפעילה Local Search ואז GroupRebalance כ-fallback.
/// </summary>

public sealed class AssignmentImprovementOrchestrator : IAssignmentImprovementOrchestrator

{
    // מנוע חיפוש מקומי שמבצע שיפורים דרך Swap/Transfer
    private readonly ILocalSearchEngine _localSearchEngine;

    // אסטרטגיית איזון קבוצות שמחפשת שיפורים על ידי העברת משתתפים בין קבוצות
    private readonly SplitClusterRebalanceStrategy _groupRebalanceStrategy;

    // שירות ניקוד שמחשב את הניקוד של חלוקה נתונה
    private readonly IAssignmentScorer _scorer;

    // שירות אימות שמוודא שהחלוקה עומדת בכל האילוצים
    private readonly IAssignmentValidator _validator;

    // הגדרות אלגוריתם שמכילות פרמטרים כמו מספר מקסימום מעברים, הפעלת Local Search וכו'
    private readonly AlgorithmSettings _settings;



    /// <summary>
    /// בנאי
    /// </summary>
    public AssignmentImprovementOrchestrator(
        ILocalSearchEngine localSearchEngine,
        SplitClusterRebalanceStrategy groupRebalanceStrategy,
        IAssignmentScorer scorer,
        IAssignmentValidator validator,
        AlgorithmSettings settings)
    {
       _localSearchEngine = localSearchEngine ?? throw new ArgumentNullException(nameof(localSearchEngine));
        _groupRebalanceStrategy = groupRebalanceStrategy
            ?? throw new ArgumentNullException(nameof(groupRebalanceStrategy));
        _scorer = scorer ?? throw new ArgumentNullException(nameof(scorer));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// תפקיד הפונקציה: משפר חלוקה חוקית 
    /// </summary>
    public AssignmentImprovementResult Improve(AssignmentImprovementInput input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var assignment = input.InitialAssignment;// חלוקה חוקית ראשונית מ-Initial Placement
        var initialScore = _scorer.CalculateScore( // חישוב ניקוד ראשוני
            assignment,
            input.Participants,
            input.ScoringWeights);

        if (!_settings.EnableLocalSearch)
        {
            return AssignmentImprovementResult.Skipped(assignment, initialScore); // אם חיפוש מקומי לא מופעל בהגדרות, מחזירים תוצאה שמציינת שהשיפור דולג עליו
        }

        // בנית מצב חלוקה התחלתי לחיפוש מקומי
        var state = AssignmentStateFactory.CreateFromAssignment(assignment);

        state.CurrentScore = initialScore;



        var searchResult = RunLocalSearch(state, input, initialScore); // חיפוש מקומי ראשון

        var currentState = searchResult.FinalState;

        ValidateAssignment(currentState, input); // אימות שהחלוקה אחרי החיפוש המקומי עדיין חוקית

        var firstLocalSearchStopReason = searchResult.StopReason; // סיבת עצירה של החיפוש המקומי הראשון
        var passDiagnostics = new List<GroupRebalancePassDiagnostics>(); // איסוף נתונים דיאגנוסטיים על כל מעבר איזון קבוצות
        var groupRebalancePassesApplied = 0; // ספירת מספר המעברים שהוחלו בפועל
        var localSearchRunsAfterFirst = 0; // ספירת מספר הריצות של חיפוש מקומי אחרי המעברים
        var totalCandidatesEvaluated = 0; // ספירת מספר המועמדים שהוערכו במהלך כל המעברים
        var groupRebalanceAttempted = false; // דגל שמציין אם נעשה ניסיון לאיזון קבוצות
        var groupRebalanceWasApplied = false; // דגל שמציין אם לפחות מעבר איזון קבוצות אחד הוחל בפועל
        GroupRebalanceSearchOutcome? lastOutcome = null; // שמירת התוצאה של המעבר האחרון לאיזון קבוצות

        // שלב 2: לולאת איזון קבוצות — ניסיון למצוא שיפורים על ידי העברת משתתפים בין קבוצות.
        for (var passIndex = 0; passIndex < _settings.MaxGroupRebalancePasses; passIndex++)
        {
            groupRebalanceAttempted = true;
            // snapshot מחדש — פרופילי אשכולות ותרומה חברתית נגזרים מהמצב הנוכחי.
            var snapshot = RuntimeDataBuilder.Build(currentState, input.Participants);
            var rebalanceContext = new GroupRebalanceEvaluationContext(
                input.Participants,
                input.Constraints,
                input.ScoringWeights);

            var outcome = _groupRebalanceStrategy.SearchForBestImprovement(
                currentState,
                snapshot,
                rebalanceContext);

            lastOutcome = outcome;
            totalCandidatesEvaluated += outcome.CandidatesEvaluated;

            var bestResult = outcome.BestResult;
            var applied = bestResult is not null && bestResult.IsValid && bestResult.IsImproving;

            passDiagnostics.Add(new GroupRebalancePassDiagnostics
            {
                PassIndex = passIndex + 1,
                LocalSearchStopReasonBeforePass = searchResult.StopReason,
                SplitClusterProfilesFound = outcome.SplitClusterCount,
                CandidatesEvaluated = outcome.CandidatesEvaluated,
                BestResultWasNull = bestResult is null,
                BestScoreDelta = bestResult?.ScoreDelta,
                Applied = applied,
            });

            LogGroupRebalancePass(passIndex + 1, searchResult.StopReason, outcome, applied);// דיאגנוסטיקה של מעבר איזון קבוצות

            if (!applied) 
            {
                // אין הצעת איזון משפרת — מפסיקים את הלולאה.
                break;
            }

            ApplyGroupRebalanceMoves(currentState, bestResult!, input); // החלת המעברים שהוצעו על ידי GroupRebalance
            groupRebalancePassesApplied++;
            groupRebalanceWasApplied = true;

            // אחרי איזון מוצלח — חיפוש מקומי נוסף לניצול השיפור.
            searchResult = RunLocalSearch(currentState, input, currentState.CurrentScore);
            currentState = searchResult.FinalState;
            localSearchRunsAfterFirst++;
            ValidateAssignment(currentState, input);
        }

        var diagnostics = new GroupRebalanceDiagnostics
        {
            EnableLocalSearch = _settings.EnableLocalSearch, // האם חיפוש מקומי מופעל בכלל
            FirstLocalSearchStopReason = firstLocalSearchStopReason, // סיבת עצירה של החיפוש המקומי הראשון
            FinalLocalSearchStopReason = searchResult.StopReason, // סיבת עצירה של החיפוש המקומי האחרון (אחרי המעברים)
            GroupRebalanceAttempted = groupRebalanceAttempted, // האם נעשה ניסיון לאיזון קבוצות
            SplitClusterProfilesFoundOnLastPass = lastOutcome?.SplitClusterCount ?? 0, // כמה פרופילי אשכולות מפוצלים נמצאו במעבר האחרון
            TotalCandidatesEvaluated = totalCandidatesEvaluated, // כמה מועמדים הוערכו בסך הכל במהלך המעברים
            BestResultWasNullOnLastPass = lastOutcome?.BestResult is null, // האם התוצאה הטובה ביותר במעבר האחרון הייתה null
            BestScoreDeltaOnLastPass = lastOutcome?.BestResult?.ScoreDelta, // מה היה שינוי הניקוד של התוצאה הטובה ביותר במעבר האחרון
            GroupRebalanceWasApplied = groupRebalanceWasApplied, // האם לפחות מעבר איזון קבוצות אחד הוחל בפועל
            GroupRebalancePassesApplied = groupRebalancePassesApplied, // כמה מעברי איזון קבוצות הוחלו בפועל
            LocalSearchRunsAfterFirst = localSearchRunsAfterFirst, // כמה ריצות של חיפוש מקומי התרחשו אחרי המעברים
            Passes = passDiagnostics,
        };

        LogGroupRebalanceSummary(diagnostics);
        var improvedAssignment = AssignmentStateConverter.ToAssignment(currentState);
        return AssignmentImprovementResult.Improved(
            improvedAssignment,
            searchResult,
            initialScore,
            diagnostics);
    }

    private LocalSearchResult RunLocalSearch( // הפעלת חיפוש מקומי על מצב נתון
        AssignmentState state, // מצב החלוקה הנוכחי
        AssignmentImprovementInput input, // קלט שמכיל משתתפים, אילוצים, משקלי ניקוד וכו'
        Score scoreBeforeRun) // ניקוד המצב לפני הריצה של החיפוש המקומי
    {
        var localSearchInput = new LocalSearchInput( // יצירת קלט עבור מנוע החיפוש המקומי
            state, // המצב ההתחלתי לחיפוש המקומי
            input.Participants, // רשימת המשתתפים
            input.Constraints, // רשימת האילוצים
            input.ScoringWeights, // משקלי הניקוד
            scoreBeforeRun); // ניקוד התחלתי שמועבר למנוע החיפוש המקומי כדי שיוכל לעקוב אחרי שיפורים
        return _localSearchEngine.Improve(localSearchInput); // הפעלת החיפוש המקומי והחזרת התוצאה שלו
    }
    private void ApplyGroupRebalanceMoves( // החלת המעברים שהוצעו על ידי GroupRebalance על מצב החלוקה
        AssignmentState state, // מצב החלוקה הנוכחי שישודרג עם המעברים
        GroupRebalanceResult rebalanceResult, // תוצאת החיפוש של GroupRebalance שמכילה את המעברים המוצעים
        AssignmentImprovementInput input) // קלט שמכיל משתתפים, אילוצים, משקלי ניקוד וכו
    {
        var candidate = rebalanceResult.Candidate // קבלת המועמד הטוב ביותר מהתוצאה של GroupRebalance
            ?? throw new InvalidOperationException("GroupRebalance result is missing its candidate.");

        foreach (var move in candidate.TransferMoves)
        {
            AssignmentStateUpdater.ApplyMoveInPlace(state, move); // החלת כל מעבר העברה שהוצע על ידי GroupRebalance על מצב החלוקה הנוכחי
        }

        ValidateAssignment(state, input); // אימות שהחלוקה אחרי החלת המעברים עדיין חוקית

        if (rebalanceResult.ScoreAfter is not null)
        {
            state.CurrentScore = rebalanceResult.ScoreAfter.Value; // אם תוצאת GroupRebalance כוללת ניקוד אחרי המעברים, משתמשים בו ישירות כדי לעדכן את הניקוד של המצב
        }
        else
        {
            var assignment = AssignmentStateConverter.ToAssignment(state); // המרה של מצב החלוקה הנוכחי לאובייקט Assignment כדי לחשב את הניקוד מחדש
            state.CurrentScore = _scorer.CalculateScore( // חישוב ניקוד מחדש אחרי החלת המעברים
                assignment,
                input.Participants,
                input.ScoringWeights);
        }
    }

    private void ValidateAssignment(AssignmentState state, AssignmentImprovementInput input)
    {
        var assignment = AssignmentStateConverter.ToAssignment(state);
        if (!_validator.IsValid(assignment, input.Constraints, out var errors))
        {
            var message = errors.Count > 0
                ? string.Join("; ", errors)
                : "Assignment failed final constraint validation.";
            throw new InvalidOperationException(message);
        }
    }

    private static void LogGroupRebalancePass(

        int passIndex,

        LocalSearchStopReason stopReasonBeforePass,

        GroupRebalanceSearchOutcome outcome,

        bool applied)

    {

        System.Diagnostics.Debug.WriteLine(

            $"[GroupRebalance] pass={passIndex} lsStopBefore={stopReasonBeforePass} " +

            $"splitClusters={outcome.SplitClusterCount} evaluated={outcome.CandidatesEvaluated} " +

            $"bestDelta={outcome.BestResult?.ScoreDelta.ToString() ?? "null"} applied={applied}");

    }



    private static void LogGroupRebalanceSummary(GroupRebalanceDiagnostics diagnostics)

    {

        System.Diagnostics.Debug.WriteLine(

            $"[GroupRebalance] summary attempted={diagnostics.GroupRebalanceAttempted} " +

            $"passesApplied={diagnostics.GroupRebalancePassesApplied} " +

            $"firstLsStop={diagnostics.FirstLocalSearchStopReason} " +

            $"finalLsStop={diagnostics.FinalLocalSearchStopReason} " +

            $"totalEvaluated={diagnostics.TotalCandidatesEvaluated} " +

            $"applied={diagnostics.GroupRebalanceWasApplied} " +

            $"lsRunsAfterFirst={diagnostics.LocalSearchRunsAfterFirst}");

    }

}


