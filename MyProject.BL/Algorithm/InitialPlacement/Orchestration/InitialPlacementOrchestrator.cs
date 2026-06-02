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
/// מתזמר את כל שלבי בניית החלוקה הראשונית.
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
    // המחלקה הזו היא "נקודת כניסה" לתהליך חלוקה ראשונית.
    // היא לא מבצעת את כל הלוגיקה בעצמה,
    // אלא מתזמרת רכיבים ייעודיים לפי סדר זרימה קבוע.

    private readonly IAssignmentValidator _validator;
    private readonly AlgorithmSettings _settings;

    private readonly FeasibilityPreChecker _preChecker;
    private readonly GreedyPlacementBuilder _greedyBuilder;
    private readonly InitialFeasibilityDecider _decider;
    private readonly LocalRepairEngine _repairEngine;
    private readonly InitialPlacementStrategyDecider _strategyDecider;
    private readonly ISolverFallback _solverFallback;

    /// <summary>
    /// מאתחל מופע חדש של <see cref="InitialPlacementOrchestrator"/>.
    /// </summary>
    /// <param name="validator">מאמת האילוצים לשימוש לאורך כל התהליך.</param>
    /// <param name="settings">הגדרות האלגוריתם. אם null, ישמשו ערכי ברירת מחדל.</param>
    public InitialPlacementOrchestrator(IAssignmentValidator validator, AlgorithmSettings? settings = null)
    {
        // תחביר "??" אומר: אם הערך שמשמאל ריק, קח את הערך שמימין.
        // validator מגיע מבחוץ (הזרקה), בדרך כלל ConstraintEngine.
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

        // ===== שלב 1: בדיקת היתכנות מוקדמת =====
        // שלב 1: בדיקת היתכנות מוקדמת.
        // המתודה Check נמצאת במחלקה FeasibilityPreChecker
        // בקובץ Algorithm/InitialPlacement/Feasibility/FeasibilityPreChecker.cs.
        // התפקיד שלה: לעצור מוקדם קלטים שסותרים אילוצים בסיסיים.
        var preCheckResult = _preChecker.Check(input);
        if (preCheckResult.Status != FeasibilityPreCheckStatus.Feasible)
        {
            // כאן משתמשים במחלקת InitialPlacementResult (קובץ Results/InitialPlacementResult.cs)
            // כדי להחזיר תוצאה אחידה לכל תרחיש כישלון/הצלחה.
            return InitialPlacementResult.InfeasiblePreCheck(preCheckResult.Errors);
        }

        // ===== שלב 2: הכנת מבני עזר =====
        // שלב 2: בניית מבני עזר.
        // OfType<T> מסנן מתוך input.Constraints רק אילוצים מסוג מסוים.
        var mandatoryPairs = input.Constraints
            .OfType<MyProject.Core.Domain.Constraints.MandatoryPairConstraint>()
            .ToList();
        var forbiddenPairs = input.Constraints
            .OfType<MyProject.Core.Domain.Constraints.ForbiddenPairConstraint>()
            .ToList();

        // Build במחלקה MandatoryGroupBuilder (קובץ MandatoryGroups/MandatoryGroupBuilder.cs):
        // מאגד משתתפים ליחידות חובה שלמות (רכיבי MustLink).
        var mandatoryUnits = MandatoryGroupBuilder.Build(input, mandatoryPairs);
        
        // Build במחלקה ConflictGraphBuilder (קובץ ConflictGraph/ConflictGraphBuilder.cs):
        // בונה גרף סתירות בין יחידות חובה לפי זוגות אסורים.
        var conflictGraph = ConflictGraphBuilder.Build(mandatoryUnits, forbiddenPairs);

        // ===== שלב 3: פרופיל קושי =====
        // שלב 3: ניתוח קושי הבעיה (מידע לדיבוג ולוגים עתידיים).
        // תחביר "_ =" אומר במפורש: מפעילים את החישוב בשביל תופעות לוואי/מידע, ולא משתמשים בתוצאה כרגע.
        // המתודה Analyze נמצאת בקובץ Difficulty/ProblemDifficultyAnalyzer.cs.
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
                return solverFirstResult;
            }
        }

        // ===== שלב 4: בנייה חמדנית =====
        // שלב 4: בנייה חמדנית.
        // TryBuild נמצא במחלקה GreedyPlacementBuilder (קובץ Placement/GreedyPlacementBuilder.cs).
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

        // ===== שלב 5: אימות חלוקה =====
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

        // ===== שלב 6: תיקון מקומי =====
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

    private InitialPlacementResult TrySolverOrReportInfeasible(
        InitialPlacementInput input,
        MandatoryUnitMap mandatoryUnits,
        ConflictGraph conflictGraph,
        IReadOnlyList<string> localErrors,
        InitialPlacementStatus localFailureStatus,
        IReadOnlyList<Group>? partialGroups)
    {
        var enrichedErrors = InfeasibilityExplanationBuilder.Enrich(
            localFailureStatus,
            input,
            mandatoryUnits,
            localErrors,
            partialGroups,
            _validator);

        var solverResult = _solverFallback.TrySolve(input, mandatoryUnits, conflictGraph, _settings);
        if (solverResult.Status is InitialPlacementStatus.SuccessViaSolver)
        {
            return solverResult;
        }

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

        return solverResult;
    }
}
