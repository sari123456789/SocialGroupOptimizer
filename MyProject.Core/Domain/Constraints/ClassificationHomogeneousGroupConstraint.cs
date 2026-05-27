using System;
using System.Collections.Generic;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Enums;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.Core.Domain.Constraints;

/// <summary>
/// אילוץ הפרדה הומוגנית: בכל קבוצה כולם באותה רמת ערך במימד <see cref="TargetDimension"/>.
/// </summary>
/// <remarks>
/// <para>מגיע מ־Data כש־<c>AssignmentClassificationConstraint.IsBalanceOrSeparation == false</c>.</para>
/// <para>שונה מאיזון יחסי — כאן אסור לערבב רמות שונות באותה קבוצה (למימד זה).</para>
/// </remarks>
public sealed class ClassificationHomogeneousGroupConstraint : IConstraint
{
    private readonly ClassificationDimensionCode _targetDimension;
    private readonly HashSet<ClassificationLevelCode> _dimensionLevels;
    private readonly IReadOnlyDictionary<ParticipantId, IReadOnlyDictionary<ClassificationDimensionCode, ClassificationLevelCode>> _participantClassifications;

    public ClassificationHomogeneousGroupConstraint(
        ClassificationDimensionCode targetDimension,
        IEnumerable<ClassificationLevelCode> dimensionLevels,
        IReadOnlyDictionary<ParticipantId, IReadOnlyDictionary<ClassificationDimensionCode, ClassificationLevelCode>> participantClassifications)
    {
        _targetDimension = targetDimension;
        _dimensionLevels = ClassificationDimensionConstraintSupport.ValidateAndCopyDimensionLevels(
            dimensionLevels,
            nameof(dimensionLevels));

        if (participantClassifications is null)
        {
            throw new ArgumentNullException(nameof(participantClassifications));
        }

        _participantClassifications = participantClassifications;
    }

    public ClassificationDimensionCode TargetDimension => _targetDimension;

    public IReadOnlySet<ClassificationLevelCode> DimensionLevels => _dimensionLevels;

    public ConstraintType Type => ConstraintType.ClassificationHomogeneousGroup;

    public bool IsSatisfied(Assignment assignment)
    {
        foreach (var group in assignment.Groups)
        {
            ClassificationLevelCode? groupLevel = null;
            foreach (var id in group.ParticipantIds)
            {
                if (!_participantClassifications.TryGetValue(id, out var map))
                {
                    return false;
                }

                var level = ClassificationDimensionConstraintSupport.TryGetLevelInDimension(
                    map,
                    _targetDimension,
                    _dimensionLevels);
                if (level is null)
                {
                    return false;
                }

                if (groupLevel is null)
                {
                    groupLevel = level;
                }
                else if (groupLevel.Value != level.Value)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
