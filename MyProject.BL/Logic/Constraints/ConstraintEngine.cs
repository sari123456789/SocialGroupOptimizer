using System;
using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Services;

namespace MyProject.BL.Logic.Constraints;

/// <summary>
/// תפקיד המחלקה: מנוע אימות האילוצים המרכזי — IAssignmentValidator.
/// המחלקה משתתפת בכל שלב שדורש בדיקת חוקיות חלוקה.
/// כל סוג אילוץ נבדק בפונקציה ייעודית בתוך מחלקה זו.
/// </summary>
public sealed class ConstraintEngine : IAssignmentValidator
{
    /// <summary>
    /// בודק האם ההקצאה עומדת בכל האילוצים שסופקו.
    /// </summary>
    /// <param name="assignment">החלוקה לבדיקה — קבוצות ומשתתפים מוקצים.</param>
    /// <param name="constraints">רשימת אילוצים מעורבת; המנוע מפצל לפי סוג לפני האימות.</param>
    /// <param name="errors">בפלט: כל הודעות השגיאה מכל המאמתים; ריקה אם ההקצאה חוקית.</param>
    /// <returns>true אם אין הפרות; אחרת false.</returns>
    public bool IsValid(
        Assignment assignment,
        IReadOnlyList<IConstraint> constraints,
        out IReadOnlyList<string> errors)
    {
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        if (constraints is null)
        {
            throw new ArgumentNullException(nameof(constraints));
        }

        var allErrors = new List<string>();

        // שלב 1: פיצול אילוצים לפי סוג — כל פונקציה בודקת סוג אחד בלבד.
        var groupSizeConstraints = constraints.OfType<GroupSizeConstraint>().ToList();
        var groupCountConstraints = constraints.OfType<GroupCountConstraint>().ToList();
        var mandatoryPairConstraints = constraints.OfType<MandatoryPairConstraint>().ToList();
        var forbiddenPairConstraints = constraints.OfType<ForbiddenPairConstraint>().ToList();
        var classificationProportionalBalanceConstraints = constraints.OfType<ClassificationProportionalBalanceConstraint>().ToList();
        var classificationHomogeneousGroupConstraints = constraints.OfType<ClassificationHomogeneousGroupConstraint>().ToList();

        allErrors.AddRange(ValidateGroupSize(assignment, groupSizeConstraints));
        allErrors.AddRange(ValidateGroupCount(assignment, groupCountConstraints));
        allErrors.AddRange(ValidateMandatoryPairs(assignment, mandatoryPairConstraints));
        allErrors.AddRange(ValidateForbiddenPairs(assignment, forbiddenPairConstraints));
        allErrors.AddRange(ValidateClassificationProportionalBalance(assignment, classificationProportionalBalanceConstraints));
        allErrors.AddRange(ValidateClassificationHomogeneousGroup(assignment, classificationHomogeneousGroupConstraints));

        errors = allErrors;
        return allErrors.Count == 0;
    }

    /// <summary>
    /// מאמת שכל קבוצה מכבדת מגבלות גודל (מינימום / מקסימום משתתפים).
    /// </summary>
    private static IReadOnlyList<string> ValidateGroupSize(
        Assignment assignment,
        IReadOnlyList<GroupSizeConstraint> constraints)
    {
        var errors = new List<string>();

        foreach (var constraint in constraints)
        {
            if (constraint.IsSatisfied(assignment))
            {
                continue;
            }

            var group = null as Group;
            foreach (var g in assignment.Groups)
            {
                if (g.Id == constraint.GroupId)
                {
                    group = g;
                    break;
                }
            }

            if (group is null)
            {
                errors.Add($"Group {constraint.GroupId} required by GroupSizeConstraint was not found in the assignment.");
            }
            else
            {
                errors.Add(
                    $"Group {constraint.GroupId} has {group.ParticipantIds.Count} participant(s) " +
                    $"but requires between {constraint.MinSize} and {constraint.MaxCapacity.Value}.");
            }
        }

        return errors;
    }

    /// <summary>
    /// מאמת שמספר הקבוצות בהקצאה נמצא בטווח המותר.
    /// </summary>
    private static IReadOnlyList<string> ValidateGroupCount(
        Assignment assignment,
        IReadOnlyList<GroupCountConstraint> constraints)
    {
        var errors = new List<string>();
        var actualCount = assignment.Groups.Count;

        foreach (var constraint in constraints)
        {
            if (constraint.IsSatisfied(assignment))
            {
                continue;
            }

            errors.Add(
                $"Assignment has {actualCount} group(s) " +
                $"but requires between {constraint.MinGroups} and {constraint.MaxGroups}.");
        }

        return errors;
    }

    /// <summary>
    /// מאמת שזוגות חובה נמצאים באותה קבוצה.
    /// </summary>
    private static IReadOnlyList<string> ValidateMandatoryPairs(
        Assignment assignment,
        IReadOnlyList<MandatoryPairConstraint> constraints)
    {
        var errors = new List<string>();

        foreach (var constraint in constraints)
        {
            if (constraint.IsSatisfied(assignment))
            {
                continue;
            }

            var groupOfA = assignment.Groups.FirstOrDefault(g => g.ParticipantIds.Contains(constraint.ParticipantA));
            var groupOfB = assignment.Groups.FirstOrDefault(g => g.ParticipantIds.Contains(constraint.ParticipantB));

            if (groupOfA is null && groupOfB is null)
            {
                errors.Add($"MandatoryPair violated: both {constraint.ParticipantA} and {constraint.ParticipantB} are missing from the assignment.");
            }
            else if (groupOfA is null)
            {
                errors.Add($"MandatoryPair violated: participant {constraint.ParticipantA} is missing from the assignment.");
            }
            else if (groupOfB is null)
            {
                errors.Add($"MandatoryPair violated: participant {constraint.ParticipantB} is missing from the assignment.");
            }
            else
            {
                errors.Add($"MandatoryPair violated: {constraint.ParticipantA} is in group {groupOfA.Id} but {constraint.ParticipantB} is in group {groupOfB.Id}.");
            }
        }

        return errors;
    }

    /// <summary>
    /// מאמת שזוגות משתתפים אסורים אינם מוקצים לאותה קבוצה.
    /// </summary>
    private static IReadOnlyList<string> ValidateForbiddenPairs(
        Assignment assignment,
        IReadOnlyList<ForbiddenPairConstraint> constraints)
    {
        var errors = new List<string>();

        foreach (var constraint in constraints)
        {
            if (constraint.IsSatisfied(assignment))
            {
                continue;
            }

            var group = assignment.Groups.FirstOrDefault(g =>
                g.ParticipantIds.Contains(constraint.ParticipantA)
                && g.ParticipantIds.Contains(constraint.ParticipantB));

            if (group is not null)
            {
                errors.Add($"ForbiddenPair violated: {constraint.ParticipantA} and {constraint.ParticipantB} are both in group {group.Id}.");
            }
        }

        return errors;
    }

    /// <summary>
    /// מאמת שיחסי רמות מימד בכל קבוצה תואמים לפרופורציות הגלובליות (עם סטייה מותרת).
    /// </summary>
    private static IReadOnlyList<string> ValidateClassificationProportionalBalance(
        Assignment assignment,
        IReadOnlyList<ClassificationProportionalBalanceConstraint> constraints)
    {
        var errors = new List<string>();

        foreach (var constraint in constraints)
        {
            if (constraint.IsSatisfied(assignment))
            {
                continue;
            }

            var levels = string.Join(", ", constraint.DimensionLevels.OrderBy(x => x.Value));

            errors.Add(
                $"ClassificationProportionalBalance violated for dimension '{constraint.TargetDimension}' levels [{levels}]: " +
                $"each group's level counts must match global proportions within scaled deviation {constraint.MaxScaledDeviation} " +
                $"(|groupCount*N - globalCount*groupSize|). Participants must have exactly one allowed level in this dimension.");
        }

        return errors;
    }

    /// <summary>
    /// מאמת שבכל קבוצה כל המשתתפים שייכים לרמה אחת בלבד במימד הסיווג (הפרדה הומוגנית).
    /// </summary>
    private static IReadOnlyList<string> ValidateClassificationHomogeneousGroup(
        Assignment assignment,
        IReadOnlyList<ClassificationHomogeneousGroupConstraint> constraints)
    {
        var errors = new List<string>();

        foreach (var constraint in constraints)
        {
            if (constraint.IsSatisfied(assignment))
            {
                continue;
            }

            var levels = string.Join(", ", constraint.DimensionLevels.OrderBy(x => x.Value));

            errors.Add(
                $"ClassificationHomogeneousGroup violated for dimension '{constraint.TargetDimension}' levels [{levels}]: " +
                $"each group must contain participants of at most one level, and each participant must have exactly one allowed level in this dimension.");
        }

        return errors;
    }
}
