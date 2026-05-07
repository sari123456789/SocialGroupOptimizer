using System;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Enums;

namespace MyProject.Core.Domain.Constraints;

/// <summary>
/// אילוץ על מספר הקבוצות בהקצאה: מינימום ומקסימום.
/// </summary>
public sealed class GroupCountConstraint : IConstraint
{
    /// <summary>
    /// מאתחל מופע חדש של <see cref="GroupCountConstraint"/>.
    /// </summary>
    /// <param name="minGroups">מספר קבוצות מינימלי נדרש.</param>
    /// <param name="maxGroups">מספר קבוצות מקסימלי מותר.</param>
    /// <exception cref="ArgumentOutOfRangeException">נזרק כאשר <paramref name="minGroups"/> קטן מאחד.</exception>
    /// <exception cref="ArgumentException">נזרק כאשר <paramref name="maxGroups"/> קטן מ-<paramref name="minGroups"/>.</exception>
    public GroupCountConstraint(int minGroups, int maxGroups)
    {
        if (minGroups <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minGroups), "Minimum group count must be greater than zero.");
        }

        if (maxGroups < minGroups)
        {
            throw new ArgumentException("Maximum group count cannot be less than minimum group count.", nameof(maxGroups));
        }

        MinGroups = minGroups;
        MaxGroups = maxGroups;
    }

    /// <summary>
    /// מספר קבוצות מינימלי נדרש.
    /// </summary>
    public int MinGroups { get; }

    /// <summary>
    /// מספר קבוצות מקסימלי מותר.
    /// </summary>
    public int MaxGroups { get; }

    /// <inheritdoc/>
    public ConstraintType Type => ConstraintType.GroupCount;

    /// <inheritdoc/>
    public bool IsSatisfied(Assignment assignment)
    {
        var count = assignment.Groups.Count;
        return count >= MinGroups && count <= MaxGroups;
    }
}
