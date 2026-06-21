using MyProject.BL.Algorithm.InitialPlacement.Difficulty;
using MyProject.BL.Algorithm.InitialPlacement.Placement;
using MyProject.BL.Algorithm.InitialPlacement.Results;
using MyProject.BL.Algorithm.InitialPlacement.Solver;
using MyProject.BL.Algorithm.InitialPlacement.Strategy;
using MyProject.BL.Logic.Configuration;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Services;

namespace MyProject.BL.Algorithm.InitialPlacement.Orchestration;

/// <summary>
/// תפקיד המחלקה: נקודת הכניסה המרכזית לבניית חלוקה ראשונית.
/// המחלקה משתתפת בשלב השיבוץ הראשוני — מתזמרת את כל תתי-השלבים.
/// </summary>
/// <remarks>
/// <para>זרימת הביצוע:</para>
/// <list type="number">
///   <item><description>בדיקת היתכנות מוקדמת (<see cref="FeasibilityPreChecker"/>)</description></item>
///   <item><description>בניית יחידות חובה וגרף קונפליקטים</description></item>
///   <item><description>ניתוח קושי הבעיה (<see cref="ProblemDifficultyAnalyzer"/>)</description></item>
///   <item><description>בנייה חמדנית (<see cref="GreedyPlacementBuilder"/>)</description></item>
///   <item><description>בדיקת חוקיות (<see cref="InitialFeasibilityDecider"/>)</description></item>
///   <item><description>תיקון מקומי אם נדרש (<see cref="LocalRepairEngine"/>)</description></item>
/// </list>
/// </remarks>
public sealed class InitialPlacementOrchestrator
{
    /// המחלקה הזו היא "נקודת כניסה" לתהליך חלוקה ראשונית.
    /// היא לא מבצעת את כל הלוגיקה בעצמה,
   /// אלא מתזמרת רכיבים ייעודיים לפי סדר זרימה קבוע.

    private readonly IAssignmentValidator _validator;
    private readonly AlgorithmSettings _settings;
    /// <summary>
    /// // רכיבי המשנה הם מחלקות ייעודיות שמטפלות בכל אחד מהשלבים בתהליך.
    /// </summary>
    private readonly FeasibilityPreChecker _preChecker;//בדיקת היתכנות מוקדמת
    private readonly GreedyPlacementBuilder _greedyBuilder;//בניית חלוקה חמדנית
    private readonly InitialFeasibilityDecider _decider;//בדיקת חוקיות ראשונית
    private readonly LocalRepairEngine _repairEngine;//תיקון מקומי
    private readonly InitialPlacementStrategyDecider _strategyDecider;//קבלת החלטה על אסטרטגיית החלוקה הראשונית
    private readonly ISolverFallback _solverFallback;//מסלול גיבוי עם פותר אילוצים חיצוני

    /// <summary>
    /// מאתחל מופע חדש של <see cref="InitialPlacementOrchestrator"/>.
    /// </summary>
    /// <param name="validator">מאמת האילוצים לשימוש לאורך כל התהליך.</param>
    /// <param name="settings">הגדרות האלגוריתם. אם null, ישמשו ערכי ברירת מחדל.</param>
    public InitialPlacementOrchestrator(IAssignmentValidator validator, AlgorithmSettings? settings = null)
    {
        // זה שומר על כלל ארכיטקטורה: אימות סופי רק דרך IAssignmentValidator.
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _settings = settings ?? new AlgorithmSettings();

        // בניית רכיבי המשנה:
        // כל אחד ממומש בקובץ ייעודי ומטפל בחלק אחר של הזרימה.
        _preChecker = new FeasibilityPreChecker();
        _greedyBuilder = new GreedyPlacementBuilder();
        _decider = new InitialFeasibilityDecider(_validator);
        _repairEngine = new LocalRepairEngine(_validator);
        _strategyDecider = new InitialPlacementStrategyDecider();
        _solverFallback = new ExternalSolverFallback(
            new SolverInputBuilder(),
            new SolverResultMapper(),
            new SolverTranslationValidator(_validator),
            new ExternalSolverClient());
    }

    /// <summary>
    /// מריץ את תהליך בניית החלוקה הראשונית.
    /// </summary>
    /// <param name="input">קלט ההצבה הראשונית.</param>
    /// <returns>תוצאת התהליך עם <see cref="Assignment"/> אם הצליח.</returns>
    public InitialPlacementResult Run(InitialPlacementInput input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        // שלב 1: בדיקת היתכנות מוקדמת.
        // התפקיד שלה: לעצור מוקדם קלטים שסותרים אילוצים בסיסיים.
        var preCheckResult = _preChecker.Check(input);
        if (preCheckResult.Status != FeasibilityPreCheckStatus.Feasible)
        {
            var preCheckMessages = preCheckResult.Errors
                .Select(InfeasibilityExplanationBuilder.TranslatePublic)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            return InitialPlacementResult.InfeasiblePreCheck(
                InitialPlacementUserMessages.InfeasibleWithDetails(preCheckMessages));
        }

        // שלב 2: בניית מבני עזר.
        var mandatoryPairs = input.Constraints
            .OfType<MyProject.Core.Domain.Constraints.MandatoryPairConstraint>()
            .ToList();
        var forbiddenPairs = input.Constraints
            .OfType<MyProject.Core.Domain.Constraints.ForbiddenPairConstraint>()
            .ToList();

        // מאגד משתתפים ליחידות חובה שלמות (רכיבי MustLink).
        var mandatoryUnits = MandatoryGroupBuilder.Build(input, mandatoryPairs);
        
        // בונה גרף סתירות בין יחידות חובה לפי זוגות אסורים.
        var conflictGraph = ConflictGraphBuilder.Build(mandatoryUnits, forbiddenPairs);

        // שלב 3: ניתוח קושי הבעיה .
        var difficultyProfile = ProblemDifficultyAnalyzer.Analyze(input, mandatoryUnits, conflictGraph);

        var strategy = _strategyDecider.Decide(difficultyProfile, _settings);
        if (strategy == InitialPlacementStrategy.SolverFirst)
        {
            var solverFirstResult = _solverFallback.TrySolve(input, mandatoryUnits, conflictGraph, _settings);
            if (solverFirstResult.Status is InitialPlacementStatus.SuccessViaSolver)
            {
                return solverFirstResult;
            }

            if (!InitialPlacementUserMessages.IsSolverInfrastructureFailure(solverFirstResult.Errors))
            {
                return ReportSolverInfeasible(
                    input,
                    mandatoryUnits,
                    conflictGraph,
                    solverFirstResult.Errors);
            }
        }

        // שלב 4: בנייה חמדנית.
        // הוא מנסה לשבץ יחידות חובה שלמות, ואז משתתפים יחידניים שנשארו.
        var buildResult = _greedyBuilder.TryBuild(input, mandatoryUnits, out var buildErrors);
        var builtGroups = buildResult.Groups;
        if (builtGroups is null || builtGroups.Count == 0)
        {
            return TrySolverOrReportInfeasible(
                input,
                mandatoryUnits,
                conflictGraph,
                buildErrors,
                InitialPlacementStatus.BuildFailed,
                null);
        }

        // שלב 5: בדיקת חוקיות ראשונית.
        // תחביר "out var" מחזיר מידע נוסף מהמתודה דרך פרמטר פלט.
        // IsValid במחלקה InitialFeasibilityDecider (קובץ Placement/InitialFeasibilityDecider.cs)
        // עוטף קריאה ל-IAssignmentValidator כדי שכל אימות סופי יעבור דרך המנוע המרכזי.
        if (_decider.IsValid(builtGroups, input.Constraints, out _))
        {
            // Assignment מוגדר בשכבת Core (Entities/Assignment.cs)
            // ומייצג פתרון חלוקה מלא וחוקי.
            return InitialPlacementResult.Success(new Assignment(builtGroups));
        }

        // שלב 6: תיקון מקומי.
        var mutableGroups = builtGroups.ToList();
        // TryRepair נמצא במחלקה LocalRepairEngine (קובץ Placement/LocalRepairEngine.cs).
        // הוא מבצע תיקון ברמת יחידות שיבוץ שלמות, בלי לפצל יחידת חובה.
        if (_repairEngine.TryRepair(mutableGroups, input, mandatoryUnits, _settings.RepairAttempts, out var repairErrors))
        {
            return InitialPlacementResult.Success(new Assignment(mutableGroups));
        }

        return TrySolverOrReportInfeasible(
            input,
            mandatoryUnits,
            conflictGraph,
            repairErrors,
            InitialPlacementStatus.RepairFailed,
            mutableGroups);
    }

    /// <summary>
    /// תפקיד הפונקציה: מנסה פותר אילוצים — Solver — כגיבוי, או מדווח על חוסר היתכנות.
    /// קלט עיקרי: קלט, יחידות חובה, גרף קונפליקטים, שגיאות מקומיות.
    /// פלט עיקרי: תוצאת שיבוץ או דיווח כשלון מפורט.
    /// </summary>
    private InitialPlacementResult TrySolverOrReportInfeasible(
        InitialPlacementInput input, // קלט ההצבה הראשונית.
        MandatoryUnitMap mandatoryUnits, // מיפוי יחידות חובה שלמות (MustLink) לפי מזהי משתתפים.
        ConflictGraph conflictGraph, // גרף סתירות בין יחידות חובה לפי זוגות אסורים.
        IReadOnlyList<string> localErrors, // רשימת שגיאות מקומיות שנוצרו בשלב החמדני או התיקון.
        InitialPlacementStatus localFailureStatus, // סטטוס כשל מקומי (BuildFailed או RepairFailed).
        IReadOnlyList<Group>? partialGroups) // רשימת קבוצות חלקיות שנוצרו בשלב החמדני או התיקון, אם קיימת.
    {
        // שלב 1: העשרת שגיאות מקומיות — הסברים בעברית ורמזים לפי מצב החלקי.
        var enrichedErrors = InfeasibilityExplanationBuilder.Enrich(
            localFailureStatus,
            input,
            mandatoryUnits,
            localErrors,
            partialGroups,
            _validator);

        // שלב 2: ניסיון פותר אילוצים — Solver — כמסלול גיבוי אחרי כשל Greedy או תיקון.
        var solverResult = _solverFallback.TrySolve(input, mandatoryUnits, conflictGraph, _settings);
        if (solverResult.Status is InitialPlacementStatus.SuccessViaSolver)
        {
            return solverResult;
        }

        // שלב 3: כשל תשתיתי (timeout, שירות לא זמין) — מחזירים את השגיאות המקומיות המועשרות.
        if (InitialPlacementUserMessages.IsSolverInfrastructureFailure(solverResult.Errors))
        {
            return localFailureStatus switch
            {
                InitialPlacementStatus.BuildFailed => InitialPlacementResult.BuildFailed(
                    InitialPlacementUserMessages.InfeasibleWithDetails(enrichedErrors)),
                _ => InitialPlacementResult.RepairFailed(
                    InitialPlacementUserMessages.InfeasibleWithDetails(enrichedErrors)),
            };
        }

        // שלב 4: הפותר רץ אך לא מצא פתרון — מאחדים שגיאות מקומיות, מהפותר ואבחון.
        return ReportSolverInfeasible(
            input,
            mandatoryUnits,
            conflictGraph,
            enrichedErrors,
            solverResult.Errors);
    }

    /// <summary>
    /// תפקיד הפונקציה: מרכזת דיווח כשלון כשהפותר לא מצא פתרון חוקי.
    /// קלט עיקרי: קלט, יחידות חובה, גרף קונפליקטים, מקורות הודעות שגיאה.
    /// פלט עיקרי: InitialPlacementResult.SolverFailed עם הודעות ממוזגות בעברית.
    /// </summary>
    private static InitialPlacementResult ReportSolverInfeasible(
        InitialPlacementInput input,
        MandatoryUnitMap mandatoryUnits,
        ConflictGraph conflictGraph,
        params IReadOnlyList<string>[] detailSources)
    {
        // שלב 1: אבחון מבני — רמזים על סיבות אפשריות לחוסר היתכנות.
        var diagnostics = InfeasibilityExplanationBuilder.BuildDiagnostics(
            input,
            mandatoryUnits,
            conflictGraph);

        // שלב 2: מיזוג הודעות מכל המקורות — ללא כפילויות.
        var merged = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var source in detailSources)
        {
            foreach (var message in source)
            {
                var translated = InfeasibilityExplanationBuilder.TranslatePublic(message);
                if (!string.IsNullOrWhiteSpace(translated) && seen.Add(translated))
                {
                    merged.Add(translated);
                }
            }
        }

        foreach (var diagnostic in diagnostics)
        {
            if (seen.Add(diagnostic))
            {
                merged.Add(diagnostic);
            }
        }

        return InitialPlacementResult.SolverFailed(
            InitialPlacementUserMessages.InfeasibleWithDetails(merged));
    }
}
