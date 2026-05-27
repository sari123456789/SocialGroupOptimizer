using System;
using System.Collections.Generic;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Enums;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.Core.Domain.Constraints;

/// <summary>
/// אילוץ איזון יחסי: בכל קבוצה, חלוקת הרמות במימד <see cref="TargetDimension"/> דומה לחלוקה הכללית.
/// </summary>
/// <remarks>
/// <para>מגיע מ־Data כש־<c>AssignmentClassificationConstraint.IsBalanceOrSeparation == true</c>.</para>
/// <para><see cref="DimensionLevels"/> — כל הרמות האפשריות במימד (מטבלת <c>ClassificationLevels</c>).</para>
/// <para>מפת הסיווגים נבנית ב־<c>ConstraintMapper.BuildParticipantClassificationsMap</c>.</para>
/// </remarks>
public sealed class ClassificationProportionalBalanceConstraint : IConstraint
{
    public const long DefaultMaxScaledDeviation = 10_000L;

    private readonly ClassificationDimensionCode _targetDimension;
    private readonly HashSet<ClassificationLevelCode> _dimensionLevels;
    private readonly IReadOnlyDictionary<ParticipantId, IReadOnlyDictionary<ClassificationDimensionCode, ClassificationLevelCode>> _participantClassifications;

    public ClassificationProportionalBalanceConstraint(
        ClassificationDimensionCode targetDimension,
        IEnumerable<ClassificationLevelCode> dimensionLevels,
        IReadOnlyDictionary<ParticipantId, IReadOnlyDictionary<ClassificationDimensionCode, ClassificationLevelCode>> participantClassifications)
        : this(targetDimension, dimensionLevels, participantClassifications, DefaultMaxScaledDeviation)
    {
    }

    public ClassificationProportionalBalanceConstraint(
        ClassificationDimensionCode targetDimension,
        IEnumerable<ClassificationLevelCode> dimensionLevels,
        IReadOnlyDictionary<ParticipantId, IReadOnlyDictionary<ClassificationDimensionCode, ClassificationLevelCode>> participantClassifications,
        long maxScaledDeviation)
    {
        _targetDimension = targetDimension;
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

    public ClassificationDimensionCode TargetDimension => _targetDimension;

    public IReadOnlySet<ClassificationLevelCode> DimensionLevels => _dimensionLevels;

    public long MaxScaledDeviation { get; }

    public ConstraintType Type => ConstraintType.ClassificationProportionalBalance;

    public bool IsSatisfied(Assignment assignment)
    {
        var assignedIds = assignment.GetAssignedParticipantIds();
        var nTotal = assignedIds.Count;
        if (nTotal == 0)
        {
            return true;
        }

        var globalCounts = new Dictionary<ClassificationLevelCode, int>();
        foreach (var level in _dimensionLevels)
        {
            globalCounts[level] = 0;
        }

        foreach (var id in assignedIds)
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

            globalCounts[level.Value]++;
        }

        foreach (var group in assignment.Groups)
        {
            var n = group.ParticipantIds.Count;
            var groupCounts = new Dictionary<ClassificationLevelCode, int>();
            foreach (var l in _dimensionLevels)
            {
                groupCounts[l] = 0;
            }

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
