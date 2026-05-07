using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.Core.Domain.Entities;

/// <summary>
/// פתרון הקצאה מלא לכל הקבוצות.
/// </summary>
public sealed class Assignment
{
    /// <summary>
    /// מאתחל מופע חדש של המחלקה <see cref="Assignment"/>.
    /// </summary>
    /// <param name="groups">הקבוצות בפתרון ההקצאה.</param>
    /// <exception cref="ArgumentNullException">נזרק כאשר <paramref name="groups"/> הוא null.</exception>
    /// <exception cref="ArgumentException">נזרק כאשר מצב ההקצאה אינו חוקי.</exception>
    public Assignment(IEnumerable<Group> groups)
    {
        if (groups is null)
        {
            throw new ArgumentNullException(nameof(groups));
        }

        var groupList = groups.ToList();
        if (groupList.Count == 0)
        {
            throw new ArgumentException("Assignment must contain at least one group.", nameof(groups));
        }

        if (groupList.Any(g => g is null))
        {
            throw new ArgumentException("Assignment groups cannot contain null entries.", nameof(groups));
        }

        if (groupList.Select(g => g.Id).Distinct().Count() != groupList.Count)
        {
            throw new ArgumentException("Assignment cannot contain duplicate group ids.", nameof(groups));
        }

        var allParticipants = groupList.SelectMany(g => g.ParticipantIds).ToList();
        if (allParticipants.Distinct().Count() != allParticipants.Count)
        {
            throw new ArgumentException("A participant cannot appear in more than one group.", nameof(groups));
        }

        Groups = new ReadOnlyCollection<Group>(groupList);
    }

    /// <summary>
    /// מחזיר את כל הקבוצות בהקצאה.
    /// </summary>
    public IReadOnlyList<Group> Groups { get; }

    /// <summary>
    /// מחזיר את כל מזהי המשתתפים שהוקצו.
    /// </summary>
    /// <returns>רשימה לקריאה בלבד של מזהי משתתפים.</returns>
    public IReadOnlyList<ParticipantId> GetAssignedParticipantIds()
    {
        var ids = Groups.SelectMany(g => g.ParticipantIds).ToList();
        return new ReadOnlyCollection<ParticipantId>(ids);
    }
}
