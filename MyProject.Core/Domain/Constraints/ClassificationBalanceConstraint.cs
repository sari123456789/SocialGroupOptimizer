using System;
using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Enums;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.Core.Domain.Constraints;

/// <summary>
/// אילוץ הדורש שסיווג מסוים יופיע בכל קבוצה בטווח מינימום-מקסימום.
/// </summary>
/// <remarks>
/// האילוץ מקבל בנייתו מפה של סיווגי משתתפים כיוון שלישות <see cref="Assignment"/>
/// מכילה רק מזהי משתתפים ולא את הישויות עצמן.
/// </remarks>
public sealed class ClassificationBalanceConstraint : IConstraint
{
    private readonly IReadOnlyDictionary<ParticipantId, IReadOnlyList<ClassificationType>> _participantClassifications;

    /// <summary>
    /// מאתחל מופע חדש של <see cref="ClassificationBalanceConstraint"/>.
    /// </summary>
    /// <param name="targetClassification">סוג הסיווג שיש לבדוק.</param>
    /// <param name="minCountPerGroup">מינימום משתתפים בעלי הסיווג בכל קבוצה.</param>
    /// <param name="maxCountPerGroup">מקסימום משתתפים בעלי הסיווג בכל קבוצה.</param>
    /// <param name="participantClassifications">מפה ממזהה משתתף לרשימת הסיווגים שלו.</param>
    /// <exception cref="ArgumentException">נזרק כאשר הסיווג הוא <see cref="ClassificationType.Unspecified"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">נזרק כאשר <paramref name="minCountPerGroup"/> שלילי.</exception>
    /// <exception cref="ArgumentException">נזרק כאשר <paramref name="maxCountPerGroup"/> קטן מ-<paramref name="minCountPerGroup"/>.</exception>
    /// <exception cref="ArgumentNullException">נזרק כאשר <paramref name="participantClassifications"/> הוא null.</exception>
    public ClassificationBalanceConstraint(
        ClassificationType targetClassification,
        int minCountPerGroup,
        int maxCountPerGroup,
        IReadOnlyDictionary<ParticipantId, IReadOnlyList<ClassificationType>> participantClassifications)
    {
        if (targetClassification == ClassificationType.Unspecified)
        {
            throw new ArgumentException("Target classification cannot be Unspecified.", nameof(targetClassification));
        }

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

        TargetClassification = targetClassification;
        MinCountPerGroup = minCountPerGroup;
        MaxCountPerGroup = maxCountPerGroup;
        _participantClassifications = participantClassifications;
    }

    /// <summary>
    /// סוג הסיווג שיש לבדוק.
    /// </summary>
    public ClassificationType TargetClassification { get; }

    /// <summary>
    /// מינימום משתתפים בעלי הסיווג בכל קבוצה.
    /// </summary>
    public int MinCountPerGroup { get; }

    /// <summary>
    /// מקסימום משתתפים בעלי הסיווג בכל קבוצה.
    /// </summary>
    public int MaxCountPerGroup { get; }

    /// <inheritdoc/>
    public ConstraintType Type => ConstraintType.ClassificationBalance;

    /// <inheritdoc/>
    public bool IsSatisfied(Assignment assignment)
    {
        foreach (var group in assignment.Groups)
        {
            var count = group.ParticipantIds.Count(id =>
                _participantClassifications.TryGetValue(id, out var classifications)
                && classifications.Contains(TargetClassification));
            if (count < MinCountPerGroup || count > MaxCountPerGroup)
            {
                return false;
            }
        }

        return true;
    }
}
