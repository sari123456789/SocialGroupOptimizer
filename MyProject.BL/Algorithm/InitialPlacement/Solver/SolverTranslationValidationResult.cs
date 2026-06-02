namespace MyProject.BL.Algorithm.InitialPlacement.Solver;

/// <summary>
/// תפקיד: מעטפת תוצאה לאימות סופי של חלוקה שהוחזרה מהפותר — לפני החזרת SuccessViaSolver.
/// </summary>
/// <remarks>
/// נוצר ע"י <see cref="SolverTranslationValidator.Validate"/>; נצרך ע"י <see cref="ExternalSolverFallback"/>.
/// </remarks>
public sealed class SolverTranslationValidationResult
{
    private SolverTranslationValidationResult(bool isValid, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    /// <summary>
    /// תפקיד: מציין האם החלוקה עברה אימות אילוצים.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="SolverTranslationValidator.TryValidate"/>, <see cref="ExternalSolverFallback.TrySolve"/>.</remarks>
    public bool IsValid { get; }

    /// <summary>
    /// תפקיד: שגיאות אימות; ריקה כש-<see cref="IsValid"/> הוא true.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="SolverTranslationValidator.TryValidate"/> — מועבר ל-<see cref="InitialPlacementResult.SolverFailed"/>.</remarks>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// תפקיד: יוצר תוצאת אימות תקינה.
    /// </summary>
    /// <returns>מעטפת הצלחה ללא שגיאות.</returns>
    /// <remarks>נקרא מ- <see cref="SolverTranslationValidator.Validate"/>.</remarks>
    public static SolverTranslationValidationResult Valid() =>
        new(true, Array.Empty<string>());

    /// <summary>
    /// תפקיד: יוצר תוצאת אימות שנכשלה.
    /// </summary>
    /// <param name="errors">שגיאות האימות; null מומר לרשימה ריקה.</param>
    /// <returns>מעטפת כישלון.</returns>
    /// <remarks>נקרא מ- <see cref="SolverTranslationValidator.Validate"/>.</remarks>
    public static SolverTranslationValidationResult Invalid(IReadOnlyList<string> errors) =>
        new(false, errors ?? Array.Empty<string>());
}
