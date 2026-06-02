using System;
using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using InitialPlacementExecutionContext = MyProject.BL.Algorithm.InitialPlacement.Runtime.ExecutionContext;

namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// תפקיד: חוזה הקלט המרכזי לריצה אחת של תהליך ההצבה הראשונית — מחזיק הקשר ריצה, משתתפים ואילוצים.
/// </summary>
/// <remarks>
/// נבנה בשכבת BL ומועבר לכל הרכיבים (Feasibility, Greedy, Repair, Solver).
/// </remarks>
public sealed class InitialPlacementInput
{
    /// <summary>
    /// תפקיד: מאתחל קלט חדש עם ולידציה של רשימות.
    /// </summary>
    /// <param name="executionContext">נתוני הריצה (מזהה + seed).</param>
    /// <param name="participants">משתתפי הדומיין — לפחות אחד, ללא null.</param>
    /// <param name="constraints">אילוצי הדומיין — ללא null ברשימה.</param>
    /// <exception cref="ArgumentNullException">פרמטר null.</exception>
    /// <exception cref="ArgumentException">רשימת משתתפים ריקה/פסולה.</exception>
    /// <remarks>
    /// נקרא מ- <see cref="AssignmentPlacementLoader.LoadInputAsync"/>,
    /// <see cref="InitialPlacementDtoMapper.ToInput"/>,
    /// ParticipantsExcelToInitialPlacementMapper, _solver_validation.
    /// </remarks>
    public InitialPlacementInput(
        InitialPlacementExecutionContext executionContext,
        IReadOnlyList<Participant> participants,
        IReadOnlyList<IConstraint> constraints)
    {
        ExecutionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
        // ולידציה פרטית — מבטיחה רשימות לא ריקות/ללא null לפני שהקלט נכנס לאורקstrator.
        Participants = ValidateParticipants(participants);
        Constraints = ValidateConstraints(constraints);
    }

    /// <summary>
    /// תפקיד: מזהה ריצה ו-seed — לשחזור ולוגים.
    /// </summary>
    /// <remarks>נקרא מכל רכיבי InitialPlacement שצריכים הקשר ריצה.</remarks>
    public InitialPlacementExecutionContext ExecutionContext { get; }

    /// <summary>
    /// תפקיד: משתתפים לשיבוץ.
    /// </summary>
    /// <remarks>נקרא מ- FeasibilityPreChecker, GreedyPlacementBuilder, SolverInputBuilder, ProblemDifficultyAnalyzer.</remarks>
    public IReadOnlyList<Participant> Participants { get; }

    /// <summary>
    /// תפקיד: אילוצי הדומיין לריצה.
    /// </summary>
    /// <remarks>נקרא מ- FeasibilityPreChecker, InitialFeasibilityDecider, LocalRepairEngine, SolverInputBuilder.</remarks>
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

        // Any — בודק שאין פריט null ברשימה (לא רק Count).
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
