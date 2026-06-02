using System;
using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Services;

namespace MyProject.BL.Logic.Constraints;

/// <summary>
/// מנוע האילוצים: מתאם את כל המאמתים ומחזיר תוצאת אימות מרוכזת.
/// </summary>
/// <remarks>
/// <para>תפקיד: מימוש יחיד ב-BL של <see cref="IAssignmentValidator"/> — פיצול אילוצים לפי סוג, הרצת מאמתים ייעודיים, ואיסוף כל הפרות.</para>
/// <para>נקרא מ-: <c>MyProject.API/Program.cs</c> (רישום DI כ-<see cref="IAssignmentValidator"/>),
/// <see cref="Algorithm.InitialPlacement.Orchestration.InitialPlacementOrchestrator"/>,
/// <see cref="Algorithm.InitialPlacement.Placement.LocalRepairEngine"/>,
/// <see cref="Algorithm.InitialPlacement.Placement.InitialFeasibilityDecider"/>,
/// <see cref="Algorithm.InitialPlacement.Solver.SolverTranslationValidator"/>,
/// <see cref="Algorithm.InitialPlacement.Results.InfeasibilityExplanationBuilder"/>,
/// <c>_solver_validation/Program.cs</c>.</para>
/// <para>ארכיטקטורה:</para>
/// <list type="bullet">
///   <item><description>Core מגדיר את <see cref="IAssignmentValidator"/> — חוזה בלבד.</description></item>
///   <item><description>BL מממש את החוזה דרך מחלקה זו.</description></item>
///   <item><description>Algorithm קורא ל-<see cref="IsValid"/> ולא יודע אילו מאמתים פנימיים קיימים.</description></item>
/// </list>
/// </remarks>
public sealed class ConstraintEngine : IAssignmentValidator
{

    // כל מאמת אחראי לסוג אילוץ אחד (Single Responsibility).
    // הם stateless — נוצרים פעם אחת ב-constructor.
    private readonly GroupSizeValidator _groupSizeValidator;
    private readonly GroupCountValidator _groupCountValidator;
    private readonly MandatoryPairValidator _mandatoryPairValidator;
    private readonly ForbiddenPairValidator _forbiddenPairValidator;
    private readonly ClassificationBalanceValidator _classificationBalanceValidator;
    private readonly ClassificationProportionalBalanceValidator _classificationProportionalBalanceValidator;
    private readonly ClassificationHomogeneousGroupValidator _classificationHomogeneousGroupValidator;
    /// <summary>
    /// מאתחל מופע חדש עם כל המאמתים הסטנדרטיים.
    /// </summary>
    /// <remarks>
    /// <para>תפקיד: בניית רשימת המאמתים הפנימיים לשימוש חוזר בכל קריאת <see cref="IsValid"/>.</para>
    /// <para>נקרא מ-: רישום DI ב-<c>Program.cs</c>, יצירה ישירה ב-<c>_solver_validation/Program.cs</c>.</para>
    /// </remarks>
    public ConstraintEngine()
    {
        // כל מאמת אחראי לסוג אילוץ אחד — נוצר פעם אחת ונשמר לשימוש חוזר.
        _groupSizeValidator = new GroupSizeValidator();
        _groupCountValidator = new GroupCountValidator();
        _mandatoryPairValidator = new MandatoryPairValidator();
        _forbiddenPairValidator = new ForbiddenPairValidator();
        _classificationBalanceValidator = new ClassificationBalanceValidator();
        _classificationProportionalBalanceValidator = new ClassificationProportionalBalanceValidator();
        _classificationHomogeneousGroupValidator = new ClassificationHomogeneousGroupValidator();
    }
    /// <summary>
    /// בודק האם ההקצאה עומדת בכל האילוצים שסופקו.
    /// </summary>
    /// <param name="assignment">החלוקה לבדיקה — קבוצות ומשתתפים מוקצים.</param>
    /// <param name="constraints">רשימת אילוצים מעורבת; המנוע מפצל לפי סוג לפני האימות.</param>
    /// <param name="errors">בפלט: כל הודעות השגיאה מכל המאמתים; ריקה אם ההקצאה חוקית.</param>
    /// <returns><c>true</c> אם אין הפרות; אחרת <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">כאשר <paramref name="assignment"/> או <paramref name="constraints"/> הוא null.</exception>
    /// <remarks>
    /// <para>נקרא מ-: כל רכיב Algorithm שמקבל <see cref="IAssignmentValidator"/> (תיקון מקומי, החלטת היתכנות, אימות תרגום פותר, בניית הסבר כשל).</para>
    /// </remarks>
    public bool IsValid(
        Assignment assignment,
        IReadOnlyList<IConstraint> constraints,
        out IReadOnlyList<string> errors)
    {

        // nameof — מחזיר את שם הפרמטר כמחרוזת (בטוח ל-refactoring).
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        if (constraints is null)
        {
            throw new ArgumentNullException(nameof(constraints));
        }
        // allErrors אוסף הודעות מכל המאמתים — מוחזר דרך out errors בסוף.
        var allErrors = new List<string>();
        // OfType<T>() — LINQ: מסנן רק איברים מטיפוס T; ToList() מממש את השאילתה.
        var groupSizeConstraints = constraints.OfType<GroupSizeConstraint>().ToList();
        var groupCountConstraints = constraints.OfType<GroupCountConstraint>().ToList();
        var mandatoryPairConstraints = constraints.OfType<MandatoryPairConstraint>().ToList();
        var forbiddenPairConstraints = constraints.OfType<ForbiddenPairConstraint>().ToList();
        var classificationBalanceConstraints = constraints.OfType<ClassificationBalanceConstraint>().ToList();
        var classificationProportionalBalanceConstraints = constraints.OfType<ClassificationProportionalBalanceConstraint>().ToList();
        var classificationHomogeneousGroupConstraints = constraints.OfType<ClassificationHomogeneousGroupConstraint>().ToList();
        // AddRange — אוסף את כל ההפרות בבת אחת, לא עוצר בראשונה.
        allErrors.AddRange(_groupSizeValidator.Validate(assignment, groupSizeConstraints));
        allErrors.AddRange(_groupCountValidator.Validate(assignment, groupCountConstraints));
        allErrors.AddRange(_mandatoryPairValidator.Validate(assignment, mandatoryPairConstraints));
        allErrors.AddRange(_forbiddenPairValidator.Validate(assignment, forbiddenPairConstraints));
        allErrors.AddRange(_classificationBalanceValidator.Validate(assignment, classificationBalanceConstraints));
        allErrors.AddRange(_classificationProportionalBalanceValidator.Validate(assignment, classificationProportionalBalanceConstraints));
        allErrors.AddRange(_classificationHomogeneousGroupValidator.Validate(assignment, classificationHomogeneousGroupConstraints));
        // out errors — מחזירים את כל ההפרות גם כשהתוצאה false (לא עוצרים בראשונה).
        errors = allErrors;
        return allErrors.Count == 0;
    }

}


