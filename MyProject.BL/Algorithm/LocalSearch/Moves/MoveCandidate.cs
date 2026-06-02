using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.LocalSearch.Moves;

/// <summary>
/// הצעת שינוי (move) על מצב חלוקה — Swap או Transfer.
/// </summary>
/// <remarks>
/// נקרא מ-: MoveGenerator (עתידי), <see cref="State.AssignmentStateUpdater"/>.
/// </remarks>
public sealed class MoveCandidate
{
    // ctor פרטי — יצירה רק דרך Swap() / Transfer() כדי לשמור על עקביות שדות.
    private MoveCandidate(
        MoveType moveType,
        ParticipantId firstParticipantId,
        ParticipantId? secondParticipantId,
        GroupId sourceGroupId,
        GroupId targetGroupId)
    {
        MoveType = moveType;
        FirstParticipantId = firstParticipantId;
        SecondParticipantId = secondParticipantId;
        SourceGroupId = sourceGroupId;
        TargetGroupId = targetGroupId;
    }

    /// <summary>Swap = החלפה; Transfer = העברה חד-כיוונית.</summary>
    public MoveType MoveType { get; }

    /// <summary>ב-Swap: משתתף מקבוצת מקור. ב-Transfer: המשתתף המועבר.</summary>
    public ParticipantId FirstParticipantId { get; }

    /// <summary>רק ב-Swap — המשתתף השני. null ב-Transfer.</summary>
    public ParticipantId? SecondParticipantId { get; }

    /// <summary>קבוצה שממנה יוצא FirstParticipantId.</summary>
    public GroupId SourceGroupId { get; }

    /// <summary>קבוצה אליה נכנס FirstParticipantId (וב-Swap: ממנה יוצא SecondParticipantId).</summary>
    public GroupId TargetGroupId { get; }

    /// <summary>
    /// יוצר move מסוג Swap — A מקבוצה 1 ↔ B מקבוצה 2.
    /// </summary>
    /// <param name="firstParticipantId">משתתף A.</param>
    /// <param name="secondParticipantId">משתתף B.</param>
    /// <param name="sourceGroupId">קבוצה של A.</param>
    /// <param name="targetGroupId">קבוצה של B — חייבת להיות שונה מ-source.</param>
    /// <returns>MoveCandidate immutable.</returns>
    public static MoveCandidate Swap(
        ParticipantId firstParticipantId,
        ParticipantId secondParticipantId,
        GroupId sourceGroupId,
        GroupId targetGroupId)
    {
        // Swap בין אותה קבוצה = no-op — לא הגיוני כ-move.
        if (sourceGroupId.Equals(targetGroupId))
        {
            throw new ArgumentException("Swap requires different source and target groups.");
        }

        return new MoveCandidate(
            MoveType.Swap,
            firstParticipantId,
            secondParticipantId,
            sourceGroupId,
            targetGroupId);
    }

    /// <summary>
    /// יוצר move מסוג Transfer — משתתף בודד עובר קבוצה.
    /// </summary>
    /// <param name="participantId">המשתתף להעברה.</param>
    /// <param name="sourceGroupId">קבוצת מקור.</param>
    /// <param name="targetGroupId">קבוצת יעד.</param>
    /// <returns>MoveCandidate עם SecondParticipantId = null.</returns>
    public static MoveCandidate Transfer(
        ParticipantId participantId,
        GroupId sourceGroupId,
        GroupId targetGroupId)
    {
        if (sourceGroupId.Equals(targetGroupId))
        {
            throw new ArgumentException("Transfer requires different source and target groups.");
        }

        // secondParticipantId: null — מסמן שאין החלפה, רק העברה.
        return new MoveCandidate(
            MoveType.Transfer,
            participantId,
            secondParticipantId: null,
            sourceGroupId,
            targetGroupId);
    }
}
