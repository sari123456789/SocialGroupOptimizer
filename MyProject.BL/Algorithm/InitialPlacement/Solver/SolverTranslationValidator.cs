using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Services;

namespace MyProject.BL.Algorithm.InitialPlacement.Solver;

/// <summary>
/// תפקיד: מבצע אימות סופי לחלוקה שהוחזרה מהפותר — מוודא שהיא עומדת בכל אילוצי הדומיין לפני SuccessViaSolver.
/// </summary>
/// <remarks>
/// אין להחזיר SuccessViaSolver בלי מעבר דרך רכיב זה.
/// נוצר ע"י <see cref="InitialPlacementOrchestrator"/> ו-<see cref="ExternalSolverFallback"/>.
/// </remarks>
public sealed class SolverTranslationValidator
{
    private readonly IAssignmentValidator _validator;

    /// <summary>
    /// תפקיד: מאתחל מאמת עם מנוע האילוצים.
    /// </summary>
    /// <param name="validator">מאמת חלוקות; לא יכול להיות null.</param>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator"/>, <see cref="ExternalSolverFallback"/>, בדיקות _solver_validation.</remarks>
    public SolverTranslationValidator(IAssignmentValidator validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <summary>
    /// תפקיד: מאמת חלוקה מול אילוצים ומחזיר מעטפת תוצאה מפורטת.
    /// </summary>
    /// <param name="assignment">החלוקה שהוחזרה מהפותר.</param>
    /// <param name="constraints">אילוצי הדומיין לבדיקה.</param>
    /// <returns>תוצאת אימות — Valid או Invalid עם שגיאות מעוצבות.</returns>
    /// <remarks>נקרא מ- <see cref="TryValidate"/>.</remarks>
    public SolverTranslationValidationResult Validate(
        Assignment assignment,
        IReadOnlyList<IConstraint> constraints)
    {
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        if (constraints is null)
        {
            throw new ArgumentNullException(nameof(constraints));
        }

        if (_validator.IsValid(assignment, constraints, out var errors) && errors.Count == 0)
        {
            return SolverTranslationValidationResult.Valid();
        }

        // אם IsValid=false אך errors ריק — מוסיפים הודעת ברירת מחדל.
        IReadOnlyList<string> formattedErrors = errors.Count == 0
            ? new[] { "Solver assignment failed constraint validation." }
            : errors
                .Select(error => $"Solver assignment failed constraint validation: {error}")
                .ToList();

        return SolverTranslationValidationResult.Invalid(formattedErrors);
    }

    /// <summary>
    /// תפקיד: מאמת חלוקה ומחזיר תוצאה בוליאנית עם שגיאות ב-out.
    /// </summary>
    /// <param name="assignment">החלוקה לבדיקה.</param>
    /// <param name="constraints">אילוצי הדומיין.</param>
    /// <param name="errors">שגיאות אימות; ריקה בהצלחה.</param>
    /// <returns>true אם החלוקה חוקית.</returns>
    /// <remarks>נקרא מ- <see cref="ExternalSolverFallback.TrySolve"/>.</remarks>
    public bool TryValidate(
        Assignment assignment,
        IReadOnlyList<IConstraint> constraints,
        out IReadOnlyList<string> errors)
    {
        // עטיפה דקה — מעבירה ל-Validate ומחזירה bool + errors ב-out.
        var result = Validate(assignment, constraints);
        errors = result.Errors;
        return result.IsValid;
    }
}
