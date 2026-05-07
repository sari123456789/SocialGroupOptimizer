using System;
using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Services;

namespace MyProject.BL.Logic.Constraints;

/// <summary>
/// מנוע האילוצים: מתאם את כל המאמתים ומחזיר תוצאת אימות מרוכזת.
/// מממש את <see cref="IAssignmentValidator"/>.
/// </summary>
public sealed class ConstraintEngine : IAssignmentValidator
{
    private readonly GroupSizeValidator _groupSizeValidator;
    private readonly GroupCountValidator _groupCountValidator;
    private readonly MandatoryPairValidator _mandatoryPairValidator;
    private readonly ForbiddenPairValidator _forbiddenPairValidator;
    private readonly ClassificationBalanceValidator _classificationBalanceValidator;

    /// <summary>
    /// מאתחל מופע חדש של <see cref="ConstraintEngine"/> עם המאמתים הסטנדרטיים.
    /// </summary>
    public ConstraintEngine()
    {
        _groupSizeValidator = new GroupSizeValidator();
        _groupCountValidator = new GroupCountValidator();
        _mandatoryPairValidator = new MandatoryPairValidator();
        _forbiddenPairValidator = new ForbiddenPairValidator();
        _classificationBalanceValidator = new ClassificationBalanceValidator();
    }

    /// <inheritdoc/>
    public bool IsValid(
        Assignment assignment,
        IReadOnlyList<IConstraint> constraints,
        out IReadOnlyList<string> errors)
    {
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        if (constraints is null)
        {
            throw new ArgumentNullException(nameof(constraints));
        }

        var allErrors = new List<string>();

        var groupSizeConstraints = constraints.OfType<GroupSizeConstraint>().ToList();
        var groupCountConstraints = constraints.OfType<GroupCountConstraint>().ToList();
        var mandatoryPairConstraints = constraints.OfType<MandatoryPairConstraint>().ToList();
        var forbiddenPairConstraints = constraints.OfType<ForbiddenPairConstraint>().ToList();
        var classificationBalanceConstraints = constraints.OfType<ClassificationBalanceConstraint>().ToList();

        allErrors.AddRange(_groupSizeValidator.Validate(assignment, groupSizeConstraints));
        allErrors.AddRange(_groupCountValidator.Validate(assignment, groupCountConstraints));
        allErrors.AddRange(_mandatoryPairValidator.Validate(assignment, mandatoryPairConstraints));
        allErrors.AddRange(_forbiddenPairValidator.Validate(assignment, forbiddenPairConstraints));
        allErrors.AddRange(_classificationBalanceValidator.Validate(assignment, classificationBalanceConstraints));

        errors = allErrors;
        return allErrors.Count == 0;
    }
}
