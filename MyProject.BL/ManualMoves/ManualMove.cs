using System;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.ManualMoves;

/// <summary>
/// תפקיד המחלקה: מבנה שמייצג שינוי ידני — העברה או החלפה.
/// משמש את ManualMoveEvaluator להערכת השפעה על חוקיות וציון.
/// </summary>
public sealed class ManualMove
{
    private ManualMove(
        ManualMoveType type,
        ParticipantId participant,
        GroupId? targetGroupId,
        ParticipantId? secondParticipant)
    {
        Type = type;
        Participant = participant;
        TargetGroupId = targetGroupId;
        SecondParticipant = secondParticipant;
    }

    /// <summary>
    /// סוג המהלך.
    /// </summary>
    public ManualMoveType Type { get; }

    /// <summary>
    /// המשתתף הראשי (המועבר / הראשון בהחלפה).
    /// </summary>
    public ParticipantId Participant { get; }

    /// <summary>
    /// קבוצת היעד — רלוונטי ל-<see cref="ManualMoveType.Transfer"/>.
    /// </summary>
    public GroupId? TargetGroupId { get; }

    /// <summary>
    /// המשתתף השני — רלוונטי ל-<see cref="ManualMoveType.Swap"/>.
    /// </summary>
    public ParticipantId? SecondParticipant { get; }

    /// <summary>
    /// תפקיד הפונקציה: יוצרת מהלך העברה — Transfer — של משתתף לקבוצת יעד.
    /// קלט עיקרי: משתתף וקבוצת יעד.
    /// פלט עיקרי: ManualMove.
    /// </summary>
    public static ManualMove Transfer(ParticipantId participant, GroupId targetGroupId) =>
        new(ManualMoveType.Transfer, participant, targetGroupId, null);

    /// <summary>
    /// תפקיד הפונקציה: יוצרת מהלך החלפה — Swap — בין שני משתתפים.
    /// קלט עיקרי: שני מזהי משתתפים.
    /// פלט עיקרי: ManualMove.
    /// </summary>
    public static ManualMove Swap(ParticipantId first, ParticipantId second) =>
        new(ManualMoveType.Swap, first, null, second);
}
