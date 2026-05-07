using System;
using System.Linq;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Enums;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.Core.Domain.Constraints;

/// <summary>
/// אילוץ האוסר על שני משתתפים להיות מוקצים לאותה קבוצה.
/// </summary>
public sealed class ForbiddenPairConstraint : IConstraint
{
    /// <summary>
    /// מאתחל מופע חדש של <see cref="ForbiddenPairConstraint"/>.
    /// </summary>
    /// <param name="participantA">מזהה המשתתף הראשון.</param>
    /// <param name="participantB">מזהה המשתתף השני.</param>
    /// <exception cref="ArgumentException">נזרק כאשר שני המשתתפים זהים.</exception>
    public ForbiddenPairConstraint(ParticipantId participantA, ParticipantId participantB)
    {
        if (participantA == participantB)
        {
            throw new ArgumentException("ForbiddenPairConstraint requires two distinct participants.", nameof(participantB));
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
    public ConstraintType Type => ConstraintType.CannotLink;

    /// <inheritdoc/>
    public bool IsSatisfied(Assignment assignment)
    {
        var groupOfA = assignment.Groups.FirstOrDefault(g => g.ParticipantIds.Contains(ParticipantA));
        var groupOfB = assignment.Groups.FirstOrDefault(g => g.ParticipantIds.Contains(ParticipantB));

        if (groupOfA is null || groupOfB is null)
        {
            return true;
        }

        return groupOfA.Id != groupOfB.Id;
    }
}
