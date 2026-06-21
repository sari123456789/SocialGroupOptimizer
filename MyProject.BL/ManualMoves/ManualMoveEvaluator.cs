using System;
using System.Collections.Generic;
using System.Linq;
using MyProject.BL.Logic.Configuration;
using MyProject.BL.Logic.Scoring;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Enums;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.ManualMoves;

/// <summary>
/// מימוש הערכת מהלך ידני — בונה חלוקה היפותטית בזיכרון ובודק אילוצים, ללא שינוי מצב.
/// </summary>
/// <remarks>
/// <para>תקינות מערכת נבדקת תחילה (קיום משתתף/קבוצה, חברות יחידה, מבנה תקין).
/// אם נמצאה שגיאת תקינות — היא חוסמת ואינה ניתנת לחריגה.</para>
/// <para>אם המבנה תקין, נבדקים אילוצי הדומיין (<see cref="IConstraint.IsSatisfied"/>) על
/// החלוקה המתקבלת. כל אילוץ שאינו מתקיים נחשב הפרה עסקית הניתנת לאישור חריגה.</para>
/// <para>הניקוד מחושב דרך <see cref="IAssignmentScorer"/> הקיים — ללא שינוי מנוע הניקוד.</para>
/// </remarks>
public sealed class ManualMoveEvaluator : IManualMoveEvaluator
{
    private readonly IAssignmentScorer _scorer;
    private readonly ScoringWeights _weights;

    public ManualMoveEvaluator(IAssignmentScorer scorer, ScoringWeights weights)
    {
        _scorer = scorer ?? throw new ArgumentNullException(nameof(scorer));
        _weights = weights ?? throw new ArgumentNullException(nameof(weights));
    }

    /// <summary>
    /// תפקיד הפונקציה: מעריכה שינוי ידני — חוקיות, ציון לפני/אחרי והאם נדרש override.
    /// קלט עיקרי: חלוקה נוכחית, ManualMove, משתתפים ואילוצים.
    /// פלט עיקרי: ManualMoveEvaluation — ללא שינוי מצב אמיתי.
    /// </summary>
    /// <inheritdoc/>
    public ManualMoveEvaluation Evaluate(
        Assignment current,
        ManualMove move,
        IReadOnlyList<Participant> participants,
        IReadOnlyList<IConstraint> constraints)
    {
        if (current is null)
        {
            throw new ArgumentNullException(nameof(current));
        }

        if (move is null)
        {
            throw new ArgumentNullException(nameof(move));
        }

        if (participants is null)
        {
            throw new ArgumentNullException(nameof(participants));
        }

        if (constraints is null)
        {
            throw new ArgumentNullException(nameof(constraints));
        }

        var scoreBefore = ComputeScore(current, participants);

        // שלב 1: תקינות מערכת (חוסם, לא ניתן לחריגה).
        var blocking = new List<string>();
        Assignment? resulting = null;

        try
        {
            resulting = BuildResultingAssignment(current, move, blocking);
        }
        catch (ArgumentException ex)
        {
            blocking.Add($"מבנה החלוקה אינו תקין: {ex.Message}");
            resulting = null;
        }

        if (blocking.Count > 0 || resulting is null)
        {
            return new ManualMoveEvaluation(
                isLegal: false,
                requiresOverride: false,
                canOverride: false,
                scoreBefore: scoreBefore,
                scoreAfter: scoreBefore,
                brokenConstraints: new List<BrokenConstraint>(),
                blockingErrors: blocking,
                message: "לא ניתן לבצע שינוי זה.",
                resultingAssignment: null);
        }

        // שלב 2: אילוצים עסקיים (ניתנים לחריגה).
        var scoreAfter = ComputeScore(resulting, participants);

        var broken = constraints
            .Where(constraint => !constraint.IsSatisfied(resulting))
            .Select(MapBrokenConstraint)
            .ToList();

        if (broken.Count == 0)
        {
            return new ManualMoveEvaluation(
                isLegal: true,
                requiresOverride: false,
                canOverride: false,
                scoreBefore: scoreBefore,
                scoreAfter: scoreAfter,
                brokenConstraints: broken,
                blockingErrors: new List<string>(),
                message: "השינוי תקין.",
                resultingAssignment: resulting);
        }

        return new ManualMoveEvaluation(
            isLegal: false,
            requiresOverride: true,
            canOverride: true,
            scoreBefore: scoreBefore,
            scoreAfter: scoreAfter,
            brokenConstraints: broken,
            blockingErrors: new List<string>(),
            message: "השינוי מפר אילוצים עסקיים.",
            resultingAssignment: resulting);
    }

    /// <summary>
    /// בונה את החלוקה המתקבלת בזיכרון בלבד. מוסיף שגיאות תקינות ל-<paramref name="blocking"/>
    /// ומחזיר null כאשר המהלך אינו תקין מבנית.
    /// </summary>
    private static Assignment? BuildResultingAssignment(
        Assignment current,
        ManualMove move,
        List<string> blocking)
    {
        var sourceGroupsOfFirst = current.Groups
            .Where(group => group.ParticipantIds.Contains(move.Participant))
            .ToList();

        if (sourceGroupsOfFirst.Count == 0)
        {
            blocking.Add("המשתתף אינו קיים בחלוקה.");
        }
        else if (sourceGroupsOfFirst.Count > 1)
        {
            blocking.Add("המשתתף מופיע ביותר מקבוצה אחת.");
        }

        if (move.Type == ManualMoveType.Transfer)
        {
            return BuildTransfer(current, move, sourceGroupsOfFirst, blocking);
        }

        return BuildSwap(current, move, sourceGroupsOfFirst, blocking);
    }

    /// <summary>
    /// תפקיד הפונקציה: בונה חלוקה היפותטית אחרי העברה — Transfer.
    /// קלט עיקרי: חלוקה נוכחית, מהלך ידני, קבוצת מקור.
    /// פלט עיקרי: Assignment חדש או null עם שגיאות חוסמות.
    /// </summary>
    private static Assignment? BuildTransfer(
        Assignment current,
        ManualMove move,
        IReadOnlyList<Group> sourceGroupsOfFirst,
        List<string> blocking)
    {
        if (move.TargetGroupId is null)
        {
            blocking.Add("לא צוינה קבוצת יעד.");
        }

        var target = move.TargetGroupId is { } targetId
            ? current.Groups.FirstOrDefault(group => group.Id == targetId)
            : null;

        if (move.TargetGroupId is not null && target is null)
        {
            blocking.Add("קבוצת היעד אינה קיימת.");
        }

        if (blocking.Count > 0 || target is null)
        {
            return null;
        }

        var source = sourceGroupsOfFirst[0];
        if (source.Id == target.Id)
        {
            blocking.Add("המשתתף כבר נמצא בקבוצת היעד.");
            return null;
        }

        var newGroups = new List<Group>();
        foreach (var group in current.Groups)
        {
            if (group.Id == source.Id)
            {
                var remaining = group.ParticipantIds
                    .Where(id => id != move.Participant)
                    .ToList();

                if (remaining.Count > 0)
                {
                    newGroups.Add(new Group(group.Id, remaining));
                }

                continue;
            }

            if (group.Id == target.Id)
            {
                var expanded = group.ParticipantIds.ToList();
                expanded.Add(move.Participant);
                newGroups.Add(new Group(group.Id, expanded));
                continue;
            }

            newGroups.Add(new Group(group.Id, group.ParticipantIds.ToList()));
        }

        return new Assignment(newGroups);
    }

    /// <summary>
    /// תפקיד הפונקציה: בונה חלוקה היפותטית אחרי החלפה — Swap.
    /// קלט עיקרי: חלוקה נוכחית, מהלך, קבוצת מקור של המשתתף הראשון.
    /// פלט עיקרי: Assignment חדש או null עם שגיאות.
    /// </summary>
    private static Assignment? BuildSwap(
        Assignment current,
        ManualMove move,
        IReadOnlyList<Group> sourceGroupsOfFirst,
        List<string> blocking)
    {
        if (move.SecondParticipant is null)
        {
            blocking.Add("לא צוין משתתף שני להחלפה.");
        }

        var sourceGroupsOfSecond = move.SecondParticipant is { } second
            ? current.Groups.Where(group => group.ParticipantIds.Contains(second)).ToList()
            : new List<Group>();

        if (move.SecondParticipant is not null)
        {
            if (sourceGroupsOfSecond.Count == 0)
            {
                blocking.Add("המשתתף השני אינו קיים בחלוקה.");
            }
            else if (sourceGroupsOfSecond.Count > 1)
            {
                blocking.Add("המשתתף השני מופיע ביותר מקבוצה אחת.");
            }
        }

        if (blocking.Count > 0)
        {
            return null;
        }

        var first = move.Participant;
        var secondParticipant = move.SecondParticipant!.Value;
        var sourceA = sourceGroupsOfFirst[0];
        var sourceB = sourceGroupsOfSecond[0];

        if (sourceA.Id == sourceB.Id)
        {
            blocking.Add("שני המשתתפים נמצאים באותה קבוצה.");
            return null;
        }

        var newGroups = new List<Group>();
        foreach (var group in current.Groups)
        {
            if (group.Id == sourceA.Id)
            {
                var ids = group.ParticipantIds
                    .Select(id => id == first ? secondParticipant : id)
                    .ToList();
                newGroups.Add(new Group(group.Id, ids));
                continue;
            }

            if (group.Id == sourceB.Id)
            {
                var ids = group.ParticipantIds
                    .Select(id => id == secondParticipant ? first : id)
                    .ToList();
                newGroups.Add(new Group(group.Id, ids));
                continue;
            }

            newGroups.Add(new Group(group.Id, group.ParticipantIds.ToList()));
        }

        return new Assignment(newGroups);
    }

    private static BrokenConstraint MapBrokenConstraint(IConstraint constraint)
    {
        var message = constraint.Type switch
        {
            ConstraintType.GroupSize => "חריגה ממגבלת גודל קבוצה.",
            ConstraintType.MustLink => "זוג חובה הופרד בין קבוצות.",
            ConstraintType.CannotLink => "זוג אסור הוקצה לאותה קבוצה.",
            ConstraintType.GroupCount => "מספר הקבוצות אינו עומד בדרישה.",
            ConstraintType.ClassificationProportionalBalance
                or ConstraintType.ClassificationHomogeneousGroup => "פגיעה באיזון/הפרדת הסיווגים.",
            _ => "הפרת אילוץ עסקי.",
        };

        return new BrokenConstraint(constraint.Type, message);
    }

    /// <summary>
    /// תפקיד הפונקציה: מחשבת ציון חלוקה דרך IAssignmentScorer.
    /// קלט עיקרי: Assignment ומשתתפים.
    /// פלט עיקרי: Score.
    /// </summary>
    private double ComputeScore(Assignment assignment, IReadOnlyList<Participant> participants)
    {
        if (participants.Count == 0)
        {
            return 0;
        }

        return _scorer.CalculateScore(assignment, participants, _weights).Value;
    }
}
