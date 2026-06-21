using System.Collections.Generic;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Transparency;

/// <summary>
/// הסבר מלא לשיבוץ של משתתף יחיד בחלוקה קיימת.
/// </summary>
/// <remarks>
/// נבנה על ידי <see cref="AssignmentExplanationService"/> מבלי לשנות את החלוקה
/// ומבלי לשמור דבר במסד. כל החישובים הם הערכה תיאורטית בלבד.
/// </remarks>
public sealed class ParticipantPlacementExplanation
{
    /// <summary>
    /// מאתחל הסבר שיבוץ חדש.
    /// </summary>
    public ParticipantPlacementExplanation(
        ParticipantId participantId,
        GroupId currentGroupId,
        double currentGroupScore,
        bool canMove,
        GroupId? bestAlternativeGroupId,
        double? estimatedScoreChange,
        bool canSwap,
        ParticipantId? bestSwapWithParticipantId,
        GroupId? bestSwapTargetGroupId,
        double? bestSwapScoreChange,
        IReadOnlyList<ExplanationReason> reasons,
        IReadOnlyList<AlternativeGroupEvaluation> alternatives,
        IReadOnlyList<AlternativeSwapEvaluation> swapAlternatives)
    {
        ParticipantId = participantId;
        CurrentGroupId = currentGroupId;
        CurrentGroupScore = currentGroupScore;
        CanMove = canMove;
        BestAlternativeGroupId = bestAlternativeGroupId;
        EstimatedScoreChange = estimatedScoreChange;
        CanSwap = canSwap;
        BestSwapWithParticipantId = bestSwapWithParticipantId;
        BestSwapTargetGroupId = bestSwapTargetGroupId;
        BestSwapScoreChange = bestSwapScoreChange;
        Reasons = reasons ?? new List<ExplanationReason>();
        Alternatives = alternatives ?? new List<AlternativeGroupEvaluation>();
        SwapAlternatives = swapAlternatives ?? new List<AlternativeSwapEvaluation>();
    }

    /// <summary>
    /// מזהה המשתתף (תעודת זהות).
    /// </summary>
    public ParticipantId ParticipantId { get; }

    /// <summary>
    /// מזהה הקבוצה הנוכחית של המשתתף.
    /// </summary>
    public GroupId CurrentGroupId { get; }

    /// <summary>
    /// ציון השיבוץ הנוכחי של המשתתף בקבוצתו (0-100).
    /// </summary>
    public double CurrentGroupScore { get; }

    /// <summary>
    /// האם קיים מעבר חוקי שמשפר את ציון המשתתף.
    /// </summary>
    public bool CanMove { get; }

    /// <summary>
    /// הקבוצה החלופית החוקית הטובה ביותר (אם קיימת כזו שמשפרת).
    /// </summary>
    public GroupId? BestAlternativeGroupId { get; }

    /// <summary>
    /// שינוי הציון המשוער עבור החלופה הטובה ביותר.
    /// </summary>
    public double? EstimatedScoreChange { get; }

    /// <summary>
    /// סיבות מדוע המשתתף נמצא בקבוצתו הנוכחית.
    /// </summary>
    public IReadOnlyList<ExplanationReason> Reasons { get; }

    /// <summary>
    /// הערכות לכל הקבוצות החלופיות (העברה).
    /// </summary>
    public IReadOnlyList<AlternativeGroupEvaluation> Alternatives { get; }

    /// <summary>
    /// האם קיימת החלפה חוקית שמשפרת את ציון המשתתף.
    /// </summary>
    public bool CanSwap { get; }

    /// <summary>
    /// המשתתף הטוב ביותר להחלפה (אם קיימת החלפה חוקית משפרת).
    /// </summary>
    public ParticipantId? BestSwapWithParticipantId { get; }

    /// <summary>
    /// קבוצת היעד של ההחלפה הטובה ביותר.
    /// </summary>
    public GroupId? BestSwapTargetGroupId { get; }

    /// <summary>
    /// שינוי הציון המשוער עבור ההחלפה הטובה ביותר.
    /// </summary>
    public double? BestSwapScoreChange { get; }

    /// <summary>
    /// הערכות לכל החלפות אפשריות עם משתתפים מקבוצות אחרות.
    /// </summary>
    public IReadOnlyList<AlternativeSwapEvaluation> SwapAlternatives { get; }
}
