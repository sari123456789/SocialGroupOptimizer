using MyProject.BL.Algorithm.InitialPlacement.Results;
using MyProject.BL.Algorithm.InitialPlacement.Solver.Models;
using MyProject.BL.Logic.Configuration;

namespace MyProject.BL.Algorithm.InitialPlacement.Solver;

/// <summary>
/// תפקיד: מתאם מלא למסלול פותר חיצוני — בונה בקשה, קורא לשירות, ממפה תשובה, ומאמת לפני SuccessViaSolver.
/// </summary>
/// <remarks>
/// מימוש ברירת מחדל של <see cref="ISolverFallback"/>; נוצר ע"י <see cref="InitialPlacementOrchestrator"/>.
/// </remarks>
public sealed class ExternalSolverFallback : ISolverFallback
{
    private readonly SolverInputBuilder _inputBuilder;
    private readonly SolverResultMapper _resultMapper;
    private readonly SolverTranslationValidator _translationValidator;
    private readonly IExternalSolverClient _solverClient;

    /// <summary>
    /// תפקיד: מאתחל את כל רכיבי מסלול הפותר.
    /// </summary>
    /// <param name="inputBuilder">בונה בקשת פותר מקלט דומיין.</param>
    /// <param name="resultMapper">ממפה תשובת פותר לחלוקה.</param>
    /// <param name="translationValidator">מאמת חלוקה מול אילוצים.</param>
    /// <param name="solverClient">לקוח HTTP; null יוצר <see cref="ExternalSolverClient"/> ברירת מחדל.</param>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator"/>, בדיקות _solver_validation.</remarks>
    public ExternalSolverFallback(
        SolverInputBuilder inputBuilder,
        SolverResultMapper resultMapper,
        SolverTranslationValidator translationValidator,
        IExternalSolverClient? solverClient = null)
    {
        _inputBuilder = inputBuilder ?? throw new ArgumentNullException(nameof(inputBuilder));
        _resultMapper = resultMapper ?? throw new ArgumentNullException(nameof(resultMapper));
        _translationValidator = translationValidator ?? throw new ArgumentNullException(nameof(translationValidator));
        _solverClient = solverClient ?? new ExternalSolverClient();
    }

    /// <summary>
    /// תפקיד: מריץ את מסלול הפותר המלא — build → solve → map → validate.
    /// </summary>
    /// <param name="input">קלט ההצבה הראשונית.</param>
    /// <param name="mandatoryUnits">יחידות חובה.</param>
    /// <param name="conflictGraph">גרף קונפליקטים.</param>
    /// <param name="settings">הגדרות אלגוריתם.</param>
    /// <returns>SuccessViaSolver, SolverFailed או UnknownTimeout.</returns>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator"/> — SolverFirst ו-fallback.</remarks>
    public InitialPlacementResult TrySolve(
        InitialPlacementInput input,
        MandatoryUnitMap mandatoryUnits,
        ConflictGraph conflictGraph,
        AlgorithmSettings settings)
    {
        _ = input ?? throw new ArgumentNullException(nameof(input));
        _ = mandatoryUnits ?? throw new ArgumentNullException(nameof(mandatoryUnits));
        _ = conflictGraph ?? throw new ArgumentNullException(nameof(conflictGraph));
        _ = settings ?? throw new ArgumentNullException(nameof(settings));

        // שלב 1: תרגום קלט דומיין לבקשת פותר.
        var buildResult = _inputBuilder.TryBuild(input, mandatoryUnits, conflictGraph, settings);
        if (!buildResult.IsSuccess)
        {
            return InitialPlacementResult.SolverFailed(buildResult.Errors);
        }

        var request = buildResult.Request!;
        // שלב 2: קריאה סינכרונית לשירות החיצוני (GetAwaiter().GetResult()).
        var response = _solverClient.SolveAsync(request, settings).GetAwaiter().GetResult();

        if (response.IsTimeout)
        {
            return InitialPlacementResult.UnknownTimeout(
                response.Errors.Count > 0
                    ? response.Errors
                    : new[] { "External solver timed out." });
        }

        if (response.Status != SolverResponseStatus.Success)
        {
            return InitialPlacementResult.SolverFailed(
                response.Errors.Count > 0
                    ? response.Errors
                    : new[] { "External solver failed." });
        }

        // שלב 3: מיפוי תשובת הפותר לישות Assignment.
        if (!_resultMapper.TryMapAssignment(request, response, out var assignment, out var mappingErrors))
        {
            return InitialPlacementResult.SolverFailed(mappingErrors);
        }

        if (assignment is null)
        {
            return InitialPlacementResult.SolverFailed(new[] { "Mapped assignment is null." });
        }

        // שלב 4: אימות סופי מול אילוצי הדומיין לפני SuccessViaSolver.
        if (!_translationValidator.TryValidate(assignment, input.Constraints, out var validationErrors))
        {
            return InitialPlacementResult.SolverFailed(validationErrors);
        }

        return InitialPlacementResult.SuccessViaSolver(assignment);
    }
}
