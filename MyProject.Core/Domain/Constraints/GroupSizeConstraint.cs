using System;
using System.Linq;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Enums;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.Core.Domain.Constraints;

/// <summary>
/// אילוץ על גודל קבוצה ספציפית: מינימום ומקסימום מספר משתתפים.
/// </summary>
public sealed class GroupSizeConstraint : IConstraint
{
    /// <summary>
    /// מאתחל מופע חדש של <see cref="GroupSizeConstraint"/>.
    /// </summary>
    /// <param name="groupId">מזהה הקבוצה עליה חל האילוץ.</param>
    /// <param name="minSize">מספר המינימום של משתתפים בקבוצה.</param>
    /// <param name="maxCapacity">קיבולת המקסימום של הקבוצה.</param>
    /// <exception cref="ArgumentOutOfRangeException">נזרק כאשר <paramref name="minSize"/> קטן מאחד.</exception>
    /// <exception cref="ArgumentException">נזרק כאשר המינימום גדול מהמקסימום.</exception>
    public GroupSizeConstraint(GroupId groupId, int minSize, GroupCapacity maxCapacity)
    {
        if (minSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minSize), "Minimum group size must be greater than zero.");
        }

        if (minSize > maxCapacity.Value)
        {
            throw new ArgumentException("Minimum size cannot be greater than maximum capacity.", nameof(minSize));
        }

        GroupId = groupId;
        MinSize = minSize;
        MaxCapacity = maxCapacity;
    }

    /// <summary>
    /// מזהה הקבוצה עליה חל האילוץ.
    /// </summary>
    public GroupId GroupId { get; }

    /// <summary>
    /// מספר המינימום של משתתפים בקבוצה.
    /// </summary>
    public int MinSize { get; }

    /// <summary>
    /// קיבולת המקסימום של הקבוצה.
    /// </summary>
    public GroupCapacity MaxCapacity { get; }

    /// <inheritdoc/>
    public ConstraintType Type => ConstraintType.GroupSize;

    /// <inheritdoc/>
    public bool IsSatisfied(Assignment assignment)
    {
        var group = assignment.Groups.FirstOrDefault(g => g.Id == GroupId);
        if (group is null)
        {
            return false;
        }

        var count = group.ParticipantIds.Count;
        return count >= MinSize && count <= MaxCapacity.Value;
    }
}
