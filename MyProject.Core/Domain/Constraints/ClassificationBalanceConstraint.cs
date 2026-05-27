using System;
using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Enums;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.Core.Domain.Constraints;

/// <summary>
/// אילוץ הדורש שמספר משתתפים ברמת ערך מסוימת במימד יופיע בכל קבוצה בטווח מינימום-מקסימום.
/// </summary>
public sealed class ClassificationBalanceConstraint : IConstraint
{
    private readonly IReadOnlyDictionary<ParticipantId, IReadOnlyDictionary<ClassificationDimensionCode, ClassificationLevelCode>> _participantClassifications;

    public ClassificationBalanceConstraint(
        ClassificationDimensionCode targetDimension,
        ClassificationLevelCode targetLevel,
        int minCountPerGroup,
        int maxCountPerGroup,
        IReadOnlyDictionary<ParticipantId, IReadOnlyDictionary<ClassificationDimensionCode, ClassificationLevelCode>> participantClassifications)
    {
        if (minCountPerGroup < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minCountPerGroup), "Minimum count per group cannot be negative.");
        }

        if (maxCountPerGroup < minCountPerGroup)
        {
            throw new ArgumentException("Maximum count per group cannot be less than minimum count per group.", nameof(maxCountPerGroup));
        }

        if (participantClassifications is null)
        {
            throw new ArgumentNullException(nameof(participantClassifications));
        }

        TargetDimension = targetDimension;
        TargetLevel = targetLevel;
        MinCountPerGroup = minCountPerGroup;
        MaxCountPerGroup = maxCountPerGroup;
        _participantClassifications = participantClassifications;
    }

    public ClassificationDimensionCode TargetDimension { get; }

    public ClassificationLevelCode TargetLevel { get; }

    public int MinCountPerGroup { get; }

    public int MaxCountPerGroup { get; }

    public ConstraintType Type => ConstraintType.ClassificationBalance;

    public bool IsSatisfied(Assignment assignment)
    {
        foreach (var group in assignment.Groups)
        {
            var count = group.ParticipantIds.Count(id =>
                _participantClassifications.TryGetValue(id, out var map)
                && map.TryGetValue(TargetDimension, out var level)
                && level == TargetLevel);
            if (count < MinCountPerGroup || count > MaxCountPerGroup)
            {
                return false;
            }
        }

        return true;
    }
}
