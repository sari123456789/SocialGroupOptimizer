using System;
using System.Collections.Generic;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Enums;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.Core.Domain.Constraints;

/// <summary>
/// אילוץ הפרדה: בכל קבוצה כל המשתתפים חייבים לשתף את אותה רמת מימד מתוך קבוצת הרמות.
/// </summary>
/// <remarks>
/// לכל משתתף חייבת להיות בדיוק רמה אחת מתוך <see cref="DimensionLevels"/> ברשימת הסיווגים שלו.
/// </remarks>
public sealed class ClassificationHomogeneousGroupConstraint : IConstraint
{
    private readonly HashSet<ClassificationType> _dimensionLevels;
    private readonly IReadOnlyDictionary<ParticipantId, IReadOnlyList<ClassificationType>> _participantClassifications;

    /// <summary>
    /// מאתחל מופע חדש של <see cref="ClassificationHomogeneousGroupConstraint"/>.
    /// </summary>
    public ClassificationHomogeneousGroupConstraint(
        IEnumerable<ClassificationType> dimensionLevels,
        IReadOnlyDictionary<ParticipantId, IReadOnlyList<ClassificationType>> participantClassifications)
    {
        _dimensionLevels = ClassificationDimensionConstraintSupport.ValidateAndCopyDimensionLevels(
            dimensionLevels,
            nameof(dimensionLevels));

        if (participantClassifications is null)
        {
            throw new ArgumentNullException(nameof(participantClassifications));
        }

        _participantClassifications = participantClassifications;
    }

    /// <summary>
    /// רמות המימד (מאפיינים בלעדיים) שעליהן חל האילוץ.
    /// </summary>
    public IReadOnlySet<ClassificationType> DimensionLevels => _dimensionLevels;

    /// <inheritdoc/>
    public ConstraintType Type => ConstraintType.ClassificationHomogeneousGroup;

    /// <inheritdoc/>
    public bool IsSatisfied(Assignment assignment)
    {
        foreach (var group in assignment.Groups)
        {
            ClassificationType? groupLevel = null;
            foreach (var id in group.ParticipantIds)
            {
                if (!_participantClassifications.TryGetValue(id, out var list))
                {
                    return false;
                }

                var level = ClassificationDimensionConstraintSupport.TryResolveSingleDimensionLevel(list, _dimensionLevels);
                if (!level.HasValue)
                {
                    return false;
                }

                if (!groupLevel.HasValue)
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
