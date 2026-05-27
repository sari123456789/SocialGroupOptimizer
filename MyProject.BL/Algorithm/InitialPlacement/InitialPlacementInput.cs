using System;
using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using InitialPlacementExecutionContext = MyProject.BL.Algorithm.InitialPlacement.Runtime.ExecutionContext;

namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// קלט לריצה אחת של תהליך ההצבה הראשונית.
/// </summary>
public sealed class InitialPlacementInput
{
    /// <summary>
    /// מאתחל קלט חדש לתהליך ההצבה הראשונית.
    /// </summary>
    /// <param name="executionContext">נתוני הריצה הנוכחית.</param>
    /// <param name="participants">משתתפי הדומיין לשיבוץ.</param>
    /// <param name="constraints">אילוצי הדומיין הרלוונטיים לריצה.</param>
    /// <exception cref="ArgumentNullException">נזרק כאשר אחד מהפרמטרים הוא null.</exception>
    /// <exception cref="ArgumentException">נזרק כאשר רשימת המשתתפים ריקה או מכילה null, או כאשר רשימת האילוצים מכילה null.</exception>
    public InitialPlacementInput(
        InitialPlacementExecutionContext executionContext,
        IReadOnlyList<Participant> participants,
        IReadOnlyList<IConstraint> constraints)
    {
        ExecutionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
        Participants = ValidateParticipants(participants);
        Constraints = ValidateConstraints(constraints);
    }

    /// <summary>
    /// נתוני הריצה הנוכחית.
    /// </summary>
    public InitialPlacementExecutionContext ExecutionContext { get; }

    /// <summary>
    /// משתתפי הדומיין לשיבוץ.
    /// </summary>
    public IReadOnlyList<Participant> Participants { get; }

    /// <summary>
    /// אילוצי הדומיין הרלוונטיים לריצה.
    /// </summary>
    public IReadOnlyList<IConstraint> Constraints { get; }

    private static IReadOnlyList<Participant> ValidateParticipants(IReadOnlyList<Participant> participants)
    {
        if (participants is null)
        {
            throw new ArgumentNullException(nameof(participants));
        }

        if (participants.Count == 0)
        {
            throw new ArgumentException("Participants list must contain at least one participant.", nameof(participants));
        }

        if (participants.Any(participant => participant is null))
        {
            throw new ArgumentException("Participants list cannot contain null entries.", nameof(participants));
        }

        return participants;
    }

    private static IReadOnlyList<IConstraint> ValidateConstraints(IReadOnlyList<IConstraint> constraints)
    {
        if (constraints is null)
        {
            throw new ArgumentNullException(nameof(constraints));
        }

        if (constraints.Any(constraint => constraint is null))
        {
            throw new ArgumentException("Constraints list cannot contain null entries.", nameof(constraints));
        }

        return constraints;
    }
}
