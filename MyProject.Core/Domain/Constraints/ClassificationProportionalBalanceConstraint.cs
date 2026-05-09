using System;
using System.Collections.Generic;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Enums;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.Core.Domain.Constraints;

/// <summary>
/// אילוץ הדורש שיחסי רמות מימד (מאפיינים בלעדיים) בכל קבוצה יהיו קרובים ליחס הגלובלי בהקצאה.
/// </summary>
/// <remarks>
/// לכל משתתף חייבת להיות בדיוק רמה אחת מתוך <see cref="DimensionLevels"/> ברשימת הסיווגים שלו.
/// לכל קבוצה בגודל <c>n</c> ולכל רמה <c>L</c>, מספר המשתתפים ברמה <c>L</c> בקבוצה הוא <c>g</c>,
/// ומספרם הגלובלי <c>G</c> מתוך <c>N</c> משתתפים מוקצים — נדרש
/// <c>|g·N − G·n| ≤ MaxScaledDeviation</c>.
/// </remarks>
public sealed class ClassificationProportionalBalanceConstraint : IConstraint
{
    /// <summary>
    /// סטייה מקסימלית מוגדרת היטב כאשר <see cref="MaxScaledDeviation"/> שווה ל-<c>N−1</c> (עיגול שלם).
    /// </summary>
    public const long DefaultMaxScaledDeviation = 10_000L;

    private readonly HashSet<ClassificationType> _dimensionLevels;
    private readonly IReadOnlyDictionary<ParticipantId, IReadOnlyList<ClassificationType>> _participantClassifications;

    /// <summary>
    /// מאתחל מופע עם סטייה מוגדרת מראש (ברירת מחדל: <see cref="DefaultMaxScaledDeviation"/>).
    /// </summary>
    public ClassificationProportionalBalanceConstraint(
        IEnumerable<ClassificationType> dimensionLevels,
        IReadOnlyDictionary<ParticipantId, IReadOnlyList<ClassificationType>> participantClassifications)
        : this(dimensionLevels, participantClassifications, DefaultMaxScaledDeviation)
    {
    }

    /// <summary>
    /// מאתחל מופע עם סטייה מקסימלית מותאמת לביטוי המוקנה <c>|g·N − G·n|</c>.
    /// </summary>
    /// <param name="maxScaledDeviation">חייב להיות אי-שלילי.</param>
    public ClassificationProportionalBalanceConstraint(
        IEnumerable<ClassificationType> dimensionLevels,
        IReadOnlyDictionary<ParticipantId, IReadOnlyList<ClassificationType>> participantClassifications,
        long maxScaledDeviation)
    {
        _dimensionLevels = ClassificationDimensionConstraintSupport.ValidateAndCopyDimensionLevels(
            dimensionLevels,
            nameof(dimensionLevels));

        if (participantClassifications is null)
        {
            throw new ArgumentNullException(nameof(participantClassifications));
        }

        if (maxScaledDeviation < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxScaledDeviation), "Max scaled deviation cannot be negative.");
        }

        _participantClassifications = participantClassifications;
        MaxScaledDeviation = maxScaledDeviation;
    }

    /// <summary>
    /// רמות המימד (מאפיינים בלעדיים) שעליהן חל האילוץ.
    /// </summary>
    public IReadOnlySet<ClassificationType> DimensionLevels => _dimensionLevels;

    /// <summary>
    /// סטייה מקסימלית מותרת בביטוי המוקנה לכל רמה ולכל קבוצה.
    /// </summary>
    public long MaxScaledDeviation { get; }

    /// <inheritdoc/>
    public ConstraintType Type => ConstraintType.ClassificationProportionalBalance;

    /// <inheritdoc/>
    public bool IsSatisfied(Assignment assignment)
    {
        var assignedIds = assignment.GetAssignedParticipantIds();
        var nTotal = assignedIds.Count;
        if (nTotal == 0)
        {
            return true;
        }

        var globalCounts = new Dictionary<ClassificationType, int>();
        foreach (var level in _dimensionLevels)
        {
            globalCounts[level] = 0;
        }

        foreach (var id in assignedIds)
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

            globalCounts[level.Value]++;
        }

        foreach (var group in assignment.Groups)
        {
            var n = group.ParticipantIds.Count;
            var groupCounts = new Dictionary<ClassificationType, int>();
            foreach (var l in _dimensionLevels)
            {
                groupCounts[l] = 0;
            }

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

                groupCounts[level.Value]++;
            }

            foreach (var level in _dimensionLevels)
            {
                var g = groupCounts[level];
                var gGlobal = globalCounts[level];
                var deviation = Math.Abs((long)g * nTotal - (long)gGlobal * n);
                if (deviation > MaxScaledDeviation)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
