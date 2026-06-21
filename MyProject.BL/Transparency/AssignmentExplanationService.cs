using System;
using System.Collections.Generic;
using System.Linq;
using MyProject.BL.ManualMoves;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Enums;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Transparency;

/// <summary>
/// מימוש שכבת השקיפות — מסביר שיבוץ קיים ומעריך חלופות, ללא שינוי מצב.
/// </summary>
/// <remarks>
/// <para>גישה: Read Only מוחלט. השירות בונה אובייקטי דומיין זמניים בזיכרון בלבד
/// כדי להפעיל את לוגיקת האילוצים של Core (<see cref="IConstraint.IsSatisfied"/>),
/// אך לעולם אינו משנה את <paramref name="assignment"/> הנכנס ואינו שומר דבר.</para>
/// <para>חישוב הציון הוא הערכה מקומית מבוססת העדפות חברתיות (משקל 1/דירוג),
/// בעקבות אותה סמנטיקה של <c>SocialConnectionScorer</c>, אך ממוקדת במשתתף יחיד.</para>
/// <para>החלפות נבדקות דרך <see cref="IManualMoveEvaluator"/> — אותה לוגיקה כמו שינוי ידני.</para>
/// </remarks>
public sealed class AssignmentExplanationService : IAssignmentExplanationService
{
    private readonly IManualMoveEvaluator _manualMoveEvaluator;

    public AssignmentExplanationService(IManualMoveEvaluator manualMoveEvaluator)
    {
        _manualMoveEvaluator = manualMoveEvaluator
            ?? throw new ArgumentNullException(nameof(manualMoveEvaluator));
    }

    /// <summary>
    /// תפקיד הפונקציה: מסבירה למשתמש למה משתתף שובץ בקבוצה הנוכחית ומה החלופות.
    /// קלט עיקרי: חלוקה, מזהה משתתף, משתתפים ואילוצים.
    /// פלט עיקרי: ParticipantPlacementExplanation — Read Only, ללא שינוי מצב.
    /// </summary>
    /// <inheritdoc/>
    public ParticipantPlacementExplanation ExplainParticipantPlacement(
        Assignment assignment,
        ParticipantId participantId,
        IReadOnlyList<Participant> participants,
        IReadOnlyList<IConstraint> constraints)
    {
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        if (participants is null)
        {
            throw new ArgumentNullException(nameof(participants));
        }

        if (constraints is null)
        {
            throw new ArgumentNullException(nameof(constraints));
        }

        var participantById = BuildParticipantLookup(participants);

        // שלב 1: מציאת הקבוצה הנוכחית.
        var currentGroup = assignment.Groups
            .FirstOrDefault(group => group.ParticipantIds.Contains(participantId));

        if (currentGroup is null)
        {
            throw new ArgumentException(
                $"Participant {participantId.Value} is not part of the assignment.",
                nameof(participantId));
        }

        // מכנה קבוע לחישוב הציון — סך משקל ההעדפות שמערב את המשתתף (יוצאות + נכנסות).
        var scoreDenominator = ComputeScoreDenominator(participantId, participants, participantById);

        var currentMembers = currentGroup.ParticipantIds
            .Where(id => id != participantId)
            .ToList();

        var currentScore = ComputeParticipantScorePercent(
            participantId,
            currentMembers,
            participants,
            participantById,
            scoreDenominator);

        // שלב 2: סיבות לקבוצה הנוכחית.
        var reasons = BuildCurrentGroupReasons(
            participantId,
            currentMembers,
            participants,
            participantById);

        // שלב 3: הערכת כל קבוצה חלופית.
        var alternatives = new List<AlternativeGroupEvaluation>();

        foreach (var targetGroup in assignment.Groups)
        {
            if (targetGroup.Id == currentGroup.Id)
            {
                continue;
            }

            var alternative = EvaluateTransferAlternative(
                assignment,
                participantId,
                targetGroup,
                participants,
                participantById,
                constraints,
                scoreDenominator,
                currentScore);

            alternatives.Add(alternative);
        }

        // שלב 3ב: הערכת כל החלפה אפשרית עם משתתפים מקבוצות אחרות.
        var swapAlternatives = EvaluateSwapAlternatives(
            assignment,
            participantId,
            currentGroup,
            participants,
            participantById,
            constraints,
            scoreDenominator,
            currentScore);

        // שלב 4: בחירת חלופת העברה חוקית משפרת.
        var bestAlternative = alternatives
            .Where(alternative => alternative.IsLegal && alternative.EstimatedParticipantScoreDelta > 0)
            .OrderByDescending(alternative => alternative.EstimatedParticipantScoreDelta)
            .FirstOrDefault();

        bool canMove = bestAlternative is not null;
        GroupId? bestGroupId = bestAlternative?.GroupId;
        double? estimatedChange = bestAlternative?.EstimatedParticipantScoreDelta;

        var bestSwap = swapAlternatives
            .Where(swap => swap.IsLegal && swap.EstimatedParticipantScoreDelta > 0)
            .OrderByDescending(swap => swap.EstimatedParticipantScoreDelta)
            .FirstOrDefault();

        bool canSwap = bestSwap is not null;

        if (!canMove && !canSwap)
        {
            reasons.Add(new ExplanationReason(
                ExplanationReasonCode.BETTER_CURRENT_GROUP,
                "הקבוצה הנוכחית היא ההצבה הטובה ביותר עבור המשתתף — אין העברה או החלפה חוקית שמשפרת.",
                currentScore));
        }

        // שלב 5: החזרת הסבר מלא.
        return new ParticipantPlacementExplanation(
            participantId,
            currentGroup.Id,
            currentScore,
            canMove,
            bestGroupId,
            estimatedChange,
            canSwap,
            bestSwap?.SwapWithParticipantId,
            bestSwap?.TargetGroupId,
            bestSwap?.EstimatedParticipantScoreDelta,
            reasons,
            alternatives,
            swapAlternatives);
    }

    private List<AlternativeSwapEvaluation> EvaluateSwapAlternatives(
        Assignment assignment,
        ParticipantId participantId,
        Group currentGroup,
        IReadOnlyList<Participant> participants,
        IReadOnlyDictionary<ParticipantId, Participant> participantById,
        IReadOnlyList<IConstraint> constraints,
        double scoreDenominator,
        double currentScore)
    {
        var swapAlternatives = new List<AlternativeSwapEvaluation>();

        foreach (var targetGroup in assignment.Groups)
        {
            if (targetGroup.Id == currentGroup.Id)
            {
                continue;
            }

            foreach (var otherParticipantId in targetGroup.ParticipantIds)
            {
                var swap = EvaluateSwapAlternative(
                    assignment,
                    participantId,
                    targetGroup,
                    otherParticipantId,
                    participants,
                    participantById,
                    constraints,
                    scoreDenominator,
                    currentScore);

                swapAlternatives.Add(swap);
            }
        }

        return swapAlternatives;
    }

    private AlternativeSwapEvaluation EvaluateSwapAlternative(
        Assignment assignment,
        ParticipantId participantId,
        Group targetGroup,
        ParticipantId swapWithParticipantId,
        IReadOnlyList<Participant> participants,
        IReadOnlyDictionary<ParticipantId, Participant> participantById,
        IReadOnlyList<IConstraint> constraints,
        double scoreDenominator,
        double currentScore)
    {
        var reasons = new List<ExplanationReason>();
        var move = ManualMove.Swap(participantId, swapWithParticipantId);
        var evaluation = _manualMoveEvaluator.Evaluate(
            assignment,
            move,
            participants,
            constraints);

        foreach (var error in evaluation.BlockingErrors)
        {
            reasons.Add(new ExplanationReason(
                ExplanationReasonCode.CAPACITY_LIMIT,
                error,
                0));
        }

        foreach (var broken in evaluation.BrokenConstraints)
        {
            reasons.Add(MapBrokenConstraintToReason(broken, targetGroup.Id));
        }

        double targetScore = currentScore;
        if (evaluation.ResultingAssignment is not null)
        {
            var newGroup = evaluation.ResultingAssignment.Groups
                .FirstOrDefault(group => group.ParticipantIds.Contains(participantId));

            if (newGroup is not null)
            {
                var newMembers = newGroup.ParticipantIds
                    .Where(id => id != participantId)
                    .ToList();

                targetScore = ComputeParticipantScorePercent(
                    participantId,
                    newMembers,
                    participants,
                    participantById,
                    scoreDenominator);
            }
        }

        var participantDelta = targetScore - currentScore;
        var assignmentDelta = evaluation.ResultingAssignment is not null
            ? evaluation.ScoreDelta
            : 0;

        if (evaluation.IsLegal)
        {
            if (participantDelta > 0)
            {
                reasons.Add(new ExplanationReason(
                    ExplanationReasonCode.SOCIAL_MATCH,
                    $"החלפה חוקית עם {swapWithParticipantId.Value} משפרת את ציון המשתתף בכ-{participantDelta:0.0} ואת ציון החלוקה בכ-{assignmentDelta:0.0}.",
                    participantDelta));
            }
            else if (participantDelta < 0)
            {
                reasons.Add(new ExplanationReason(
                    ExplanationReasonCode.SCORE_DECREASE,
                    $"החלפה חוקית, אך ציון המשתתף יורד בכ-{Math.Abs(participantDelta):0.0} (ציון החלוקה: {assignmentDelta:+#0.0;-#0.0;0}).",
                    participantDelta));
            }
            else if (Math.Abs(assignmentDelta) >= 0.05)
            {
                reasons.Add(new ExplanationReason(
                    ExplanationReasonCode.SOCIAL_MATCH,
                    $"החלפה חוקית — ציון החלוקה משתנה בכ-{assignmentDelta:+#0.0;-#0.0;0}.",
                    assignmentDelta));
            }
        }
        else if (evaluation.RequiresOverride)
        {
            reasons.Add(new ExplanationReason(
                ExplanationReasonCode.MANDATORY_PAIR,
                "החלפה אפשרית רק עם אישור חריגה על אילוצים עסקיים.",
                0));
        }

        return new AlternativeSwapEvaluation(
            targetGroup.Id,
            swapWithParticipantId,
            evaluation.IsLegal,
            evaluation.RequiresOverride,
            participantDelta,
            assignmentDelta,
            reasons);
    }

    private static ExplanationReason MapBrokenConstraintToReason(
        BrokenConstraint broken,
        GroupId targetGroupId)
    {
        return broken.ConstraintType switch
        {
            ConstraintType.CannotLink => new ExplanationReason(
                ExplanationReasonCode.FORBIDDEN_CONSTRAINT,
                $"החלפה לקבוצה {targetGroupId.Value} מפרה זוג אסור: {broken.Message}",
                0),
            ConstraintType.MustLink => new ExplanationReason(
                ExplanationReasonCode.MANDATORY_PAIR,
                $"החלפה לקבוצה {targetGroupId.Value} מפרידה זוג חובה: {broken.Message}",
                0),
            ConstraintType.GroupSize => new ExplanationReason(
                ExplanationReasonCode.CAPACITY_LIMIT,
                broken.Message,
                0),
            ConstraintType.GroupCount => new ExplanationReason(
                ExplanationReasonCode.CAPACITY_LIMIT,
                broken.Message,
                0),
            ConstraintType.ClassificationProportionalBalance
                or ConstraintType.ClassificationHomogeneousGroup => new ExplanationReason(
                ExplanationReasonCode.CLASSIFICATION_BALANCE,
                broken.Message,
                0),
            _ => new ExplanationReason(
                ExplanationReasonCode.CAPACITY_LIMIT,
                broken.Message,
                0),
        };
    }

    /// <summary>
    /// תפקיד הפונקציה: מעריכה העברה לקבוצה חלופית — חוקיות, ציון וסיבות.
    /// קלט עיקרי: חלוקה, משתתף, קבוצת יעד, אילוצים וציון נוכחי.
    /// פלט עיקרי: AlternativeGroupEvaluation.
    /// </summary>
    private AlternativeGroupEvaluation EvaluateTransferAlternative(
        Assignment assignment,
        ParticipantId participantId,
        Group targetGroup,
        IReadOnlyList<Participant> participants,
        IReadOnlyDictionary<ParticipantId, Participant> participantById,
        IReadOnlyList<IConstraint> constraints,
        double scoreDenominator,
        double currentScore)
    {
        var reasons = new List<ExplanationReason>();
        var move = ManualMove.Transfer(participantId, targetGroup.Id);
        var evaluation = _manualMoveEvaluator.Evaluate(
            assignment,
            move,
            participants,
            constraints);

        foreach (var error in evaluation.BlockingErrors)
        {
            reasons.Add(new ExplanationReason(
                ExplanationReasonCode.CAPACITY_LIMIT,
                error,
                0));
        }

        foreach (var broken in evaluation.BrokenConstraints)
        {
            reasons.Add(MapBrokenConstraintToReason(broken, targetGroup.Id));
        }

        double targetScore = currentScore;
        if (evaluation.ResultingAssignment is not null)
        {
            var newGroup = evaluation.ResultingAssignment.Groups
                .FirstOrDefault(group => group.ParticipantIds.Contains(participantId));

            if (newGroup is not null)
            {
                var newMembers = newGroup.ParticipantIds
                    .Where(id => id != participantId)
                    .ToList();

                targetScore = ComputeParticipantScorePercent(
                    participantId,
                    newMembers,
                    participants,
                    participantById,
                    scoreDenominator);
            }
        }

        var participantDelta = targetScore - currentScore;
        var assignmentDelta = evaluation.ResultingAssignment is not null
            ? evaluation.ScoreDelta
            : 0;

        if (evaluation.IsLegal)
        {
            if (participantDelta > 0)
            {
                reasons.Add(new ExplanationReason(
                    ExplanationReasonCode.SOCIAL_MATCH,
                    $"מעבר חוקי שמשפר את ציון המשתתף בכ-{participantDelta:0.0} ואת ציון החלוקה בכ-{assignmentDelta:0.0}.",
                    participantDelta));
            }
            else if (participantDelta < 0)
            {
                reasons.Add(new ExplanationReason(
                    ExplanationReasonCode.SCORE_DECREASE,
                    $"מעבר חוקי, אך ציון המשתתף יורד בכ-{Math.Abs(participantDelta):0.0} (ציון החלוקה: {assignmentDelta:+#0.0;-#0.0;0}).",
                    participantDelta));
            }
            else if (Math.Abs(assignmentDelta) >= 0.05)
            {
                reasons.Add(new ExplanationReason(
                    ExplanationReasonCode.SOCIAL_MATCH,
                    $"מעבר חוקי — ציון החלוקה משתנה בכ-{assignmentDelta:+#0.0;-#0.0;0}.",
                    assignmentDelta));
            }
        }
        else if (evaluation.RequiresOverride)
        {
            reasons.Add(new ExplanationReason(
                ExplanationReasonCode.MANDATORY_PAIR,
                "מעבר אפשרי רק עם אישור חריגה על אילוצים עסקיים.",
                0));
        }

        return new AlternativeGroupEvaluation(
            targetGroup.Id,
            evaluation.IsLegal,
            evaluation.RequiresOverride,
            participantDelta,
            assignmentDelta,
            reasons);
    }

    /// <summary>
    /// תפקיד הפונקציה: בונה רשימת סיבות למה המשתתף בקבוצה הנוכחית (העדפות ממומשות).
    /// קלט עיקרי: משתתף, חברי קבוצה, רשימת משתתפים.
    /// פלט עיקרי: רשימת ExplanationReason.
    /// </summary>
    private static List<ExplanationReason> BuildCurrentGroupReasons(
        ParticipantId participantId,
        IReadOnlyList<ParticipantId> currentMembers,
        IReadOnlyList<Participant> participants,
        IReadOnlyDictionary<ParticipantId, Participant> participantById)
    {
        var reasons = new List<ExplanationReason>();

        var memberSet = currentMembers.ToHashSet();

        // כמה מההעדפות של המשתתף מומשו בקבוצה.
        int fulfilledOutgoing = 0;
        if (participantById.TryGetValue(participantId, out var participant))
        {
            fulfilledOutgoing = participant.Preferences
                .Count(preference => memberSet.Contains(preference.PreferredParticipantId));
        }

        if (fulfilledOutgoing > 0)
        {
            reasons.Add(new ExplanationReason(
                ExplanationReasonCode.SOCIAL_MATCH,
                $"{fulfilledOutgoing} מהעדפותיו החברתיות מומשו בקבוצה.",
                fulfilledOutgoing));
        }

        // כמה משתתפים בקבוצה ביקשו דווקא אותו.
        int incomingRequesters = currentMembers.Count(memberId =>
            participantById.TryGetValue(memberId, out var member)
            && member.Preferences.Any(preference => preference.PreferredParticipantId == participantId));

        if (incomingRequesters > 0)
        {
            reasons.Add(new ExplanationReason(
                ExplanationReasonCode.SOCIAL_REQUESTED,
                $"{incomingRequesters} משתתפים בקבוצה ביקשו אותו.",
                incomingRequesters));
        }

        // מניעת בידוד — קיים לפחות קשר חברתי אחד (יוצא או נכנס) בקבוצה.
        int totalLinks = fulfilledOutgoing + incomingRequesters;
        if (totalLinks > 0 && currentMembers.Count > 0)
        {
            reasons.Add(new ExplanationReason(
                ExplanationReasonCode.ISOLATION_PREVENTED,
                "נמנע בידוד חברתי — קיים לפחות קשר חברתי אחד בקבוצה.",
                totalLinks));
        }

        return reasons;
    }

    /// <summary>
    /// סך משקל ההעדפות שמערב את המשתתף — מכנה קבוע (אינו תלוי בקבוצה).
    /// </summary>
    private static double ComputeScoreDenominator(
        ParticipantId participantId,
        IReadOnlyList<Participant> participants,
        IReadOnlyDictionary<ParticipantId, Participant> participantById)
    {
        double total = 0;

        if (participantById.TryGetValue(participantId, out var participant))
        {
            total += participant.Preferences.Sum(preference => RankToWeight(preference.Rank));
        }

        foreach (var other in participants)
        {
            if (other.Id == participantId)
            {
                continue;
            }

            total += other.Preferences
                .Where(preference => preference.PreferredParticipantId == participantId)
                .Sum(preference => RankToWeight(preference.Rank));
        }

        return total;
    }

    /// <summary>
    /// תפקיד הפונקציה: מחשבת אחוז תרומת משתתף לציון החברתי בקבוצה נתונה.
    /// קלט עיקרי: משתתף, חברי קבוצה, מכנה קבוע.
    /// פלט עיקרי: אחוז 0–100.
    /// </summary>
    private static double ComputeParticipantScorePercent(
        ParticipantId participantId,
        IReadOnlyList<ParticipantId> groupMembers,
        IReadOnlyList<Participant> participants,
        IReadOnlyDictionary<ParticipantId, Participant> participantById,
        double denominator)
    {
        if (denominator <= 0)
        {
            return 0;
        }

        var memberSet = groupMembers.Where(id => id != participantId).ToHashSet();
        double numerator = 0;

        // העדפות יוצאות שמומשו.
        if (participantById.TryGetValue(participantId, out var participant))
        {
            numerator += participant.Preferences
                .Where(preference => memberSet.Contains(preference.PreferredParticipantId))
                .Sum(preference => RankToWeight(preference.Rank));
        }

        // העדפות נכנסות שמומשו (חברים שביקשו אותו).
        foreach (var memberId in memberSet)
        {
            if (!participantById.TryGetValue(memberId, out var member))
            {
                continue;
            }

            numerator += member.Preferences
                .Where(preference => preference.PreferredParticipantId == participantId)
                .Sum(preference => RankToWeight(preference.Rank));
        }

        var percent = numerator / denominator * 100.0;
        return Math.Clamp(percent, 0.0, 100.0);
    }

    private static double RankToWeight(int rank) => rank <= 0 ? 0 : 1.0 / rank;

    private static Dictionary<ParticipantId, Participant> BuildParticipantLookup(
        IReadOnlyList<Participant> participants)
    {
        var lookup = new Dictionary<ParticipantId, Participant>();

        foreach (var participant in participants)
        {
            if (participant is null)
            {
                continue;
            }

            lookup[participant.Id] = participant;
        }

        return lookup;
    }
}
