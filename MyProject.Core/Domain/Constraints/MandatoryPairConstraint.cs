using System;
using System.Linq;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Enums;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.Core.Domain.Constraints;

/// <summary>
/// אילוץ הדורש ששני משתתפים יוקצו לאותה קבוצה.
/// </summary>
public sealed class MandatoryPairConstraint : IConstraint
{
    /// <summary>
    /// מאתחל מופע חדש של <see cref="MandatoryPairConstraint"/>.
    /// </summary>
    /// <param name="participantA">מזהה המשתתף הראשון.</param>
    /// <param name="participantB">מזהה המשתתף השני.</param>
    /// <exception cref="ArgumentException">נזרק כאשר שני המשתתפים זהים.</exception>
    public MandatoryPairConstraint(ParticipantId participantA, ParticipantId participantB)
    {
        if (participantA == participantB)
        {
            throw new ArgumentException("MandatoryPairConstraint requires two distinct participants.", nameof(participantB));
        }

        ParticipantA = participantA;
        ParticipantB = participantB;
    }

    /// <summary>
    /// מזהה המשתתף הראשון.
    /// </summary>
    public ParticipantId ParticipantA { get; }

    /// <summary>
    /// מזהה המשתתף השני.
    /// </summary>
    public ParticipantId ParticipantB { get; }

    /// <inheritdoc/>
    public ConstraintType Type => ConstraintType.MustLink;

    /// <inheritdoc/>
    public bool IsSatisfied(Assignment assignment)
    {
        var groupOfA = assignment.Groups.FirstOrDefault(g => g.ParticipantIds.Contains(ParticipantA));
        var groupOfB = assignment.Groups.FirstOrDefault(g => g.ParticipantIds.Contains(ParticipantB));

        return groupOfA is not null
            && groupOfB is not null
            && groupOfA.Id == groupOfB.Id;
    }
}
