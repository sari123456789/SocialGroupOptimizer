using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.Core.Domain.Entities;

/// <summary>
/// קבוצת הקצאה המכילה מזהי משתתפים.
/// </summary>
public sealed class Group
{
    /// <summary>
    /// מאתחל מופע חדש של המחלקה <see cref="Group"/>.
    /// </summary>
    /// <param name="id">מזהה הקבוצה.</param>
    /// <param name="participantIds">מזהי המשתתפים השייכים לקבוצה.</param>
    /// <exception cref="ArgumentNullException">נזרק כאשר <paramref name="participantIds"/> הוא null.</exception>
    /// <exception cref="ArgumentException">נזרק כאשר מצב הקבוצה אינו חוקי.</exception>
    public Group(GroupId id, IEnumerable<ParticipantId> participantIds)
    {
        if (participantIds is null)
        {
            throw new ArgumentNullException(nameof(participantIds));
        }

        var ids = participantIds.ToList();
        if (ids.Count == 0)
        {
            throw new ArgumentException("Group must contain at least one participant.", nameof(participantIds));
        }

        if (ids.Distinct().Count() != ids.Count)
        {
            throw new ArgumentException("Group cannot contain duplicate participants.", nameof(participantIds));
        }

        Id = id;
        ParticipantIds = new ReadOnlyCollection<ParticipantId>(ids);
    }

    /// <summary>
    /// מחזיר את מזהה הקבוצה.
    /// </summary>
    public GroupId Id { get; }

    /// <summary>
    /// מחזיר את מזהי המשתתפים שהוקצו לקבוצה זו.
    /// </summary>
    public IReadOnlyList<ParticipantId> ParticipantIds { get; }
}
