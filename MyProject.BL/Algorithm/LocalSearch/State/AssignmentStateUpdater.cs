using MyProject.Core.Domain.ValueObjects;
using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.LocalSearch.State;

/// <summary>
/// נקודת הכניסה היחידה לשינוי מיפויי <see cref="AssignmentState"/>.
/// </summary>
/// <remarks>
/// נקרא מ-: MoveEvaluator (ApplyToClone), MoveExecutor (ApplyMoveInPlace) — עתידי.
/// </remarks>
public static class AssignmentStateUpdater
{
    /// <summary>
    /// מיישם move על עותק — המקור נשאר ללא שינוי.
    /// </summary>
    /// <param name="state">מצב נוכחי.</param>
    /// <param name="move">Swap או Transfer.</param>
    /// <returns>מצב חדש אחרי ה-move.</returns>
    public static AssignmentState ApplyToClone(AssignmentState state, MoveCandidate move)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (move is null)
        {
            throw new ArgumentNullException(nameof(move));
        }

        // שלב 1: שכפול עמוק — MoveEvaluator בודק הרבה moves בלי לדרוס את הפתרון הנוכחי.
        var clone = AssignmentStateCloner.Clone(state);

        // שלב 2: יישום in-place על העותק.
        ApplyMoveInPlace(clone, move);
        return clone;
    }

    /// <summary>
    /// מיישם move in-place — משנה את state ישירות.
    /// </summary>
    /// <param name="state">מצב לעדכון.</param>
    /// <param name="move">הצעת שינוי.</param>
    public static void ApplyMoveInPlace(AssignmentState state, MoveCandidate move)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (move is null)
        {
            throw new ArgumentNullException(nameof(move));
        }

        // switch על enum — dispatch ל-handler מתאים.
        switch (move.MoveType)
        {
            case MoveType.Swap:
                ApplySwapInPlace(state, move);
                break;
            case MoveType.Transfer:
                ApplyTransferInPlace(state, move);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(move), move.MoveType, "Unsupported move type.");
        }
    }

    /// <summary>
    /// Transfer in-place + עדכון hash incrementally.
    /// </summary>
    public static void ApplyTransferInPlace(AssignmentState state, MoveCandidate move)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (move is null)
        {
            throw new ArgumentNullException(nameof(move));
        }

        // מעדכן את שני המילונים (ParticipantToGroup + GroupToParticipants) בסנכרון.
        ApplyTransfer(
            state.ParticipantToGroup,
            state.GroupToParticipants,
            move);

        // hash: XOR מחיקת תרומה ישנה + הוספת חדשה — O(1) בלי לחשב מחדש את כל החלוקה.
        // ראה AssignmentHash.ApplyTransfer.
        state.CurrentHash = AssignmentHash.ApplyTransfer(
            state.CurrentHash,
            move.FirstParticipantId,
            move.SourceGroupId,
            move.TargetGroupId);
    }

    /// <summary>
    /// Swap in-place + עדכון hash incrementally.
    /// </summary>
    public static void ApplySwapInPlace(AssignmentState state, MoveCandidate move)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (move is null)
        {
            throw new ArgumentNullException(nameof(move));
        }

        if (move.SecondParticipantId is null)
        {
            throw new ArgumentException("Swap move requires a second participant.", nameof(move));
        }

        ApplySwap(
            state.ParticipantToGroup,
            state.GroupToParticipants,
            move);

        // ארבע פעולות XOR — שני משתתפים מחליפים קבוצות.
        state.CurrentHash = AssignmentHash.ApplySwap(
            state.CurrentHash,
            move.FirstParticipantId,
            move.SourceGroupId,
            move.SecondParticipantId.Value,
            move.TargetGroupId);
    }

    /// <summary>
    /// לוגיקת swap על המילונים — ללא hash (hash מתעדכן ב-ApplySwapInPlace).
    /// </summary>
    private static void ApplySwap(
        Dictionary<ParticipantId, GroupId> participantToGroup,
        Dictionary<GroupId, List<ParticipantId>> groupToParticipants,
        MoveCandidate move)
    {
        if (move.SecondParticipantId is null)
        {
            throw new ArgumentException("Swap move requires a second participant.", nameof(move));
        }

        var secondParticipantId = move.SecondParticipantId.Value;

        // שליפת רשימות המשתתפים בשתי הקבוצות — references לאותן List ב-state.
        var sourceParticipants = GetGroupParticipants(groupToParticipants, move.SourceGroupId);
        var targetParticipants = GetGroupParticipants(groupToParticipants, move.TargetGroupId);

        // וידוא שהמשתתפים באמת שייכים לקבוצות שצוינו — מונע מצב לא עקבי.
        EnsureParticipantInGroup(sourceParticipants, move.FirstParticipantId, move.SourceGroupId);
        EnsureParticipantInGroup(targetParticipants, secondParticipantId, move.TargetGroupId);

        // הסרה מהקבוצות הישנות.
        sourceParticipants.Remove(move.FirstParticipantId);
        targetParticipants.Remove(secondParticipantId);

        // הוספה לקבוצות החדשות (החלפה).
        sourceParticipants.Add(secondParticipantId);
        targetParticipants.Add(move.FirstParticipantId);

        // עדכון המילון ההפוך: כל משתתף → GroupId חדש.
        participantToGroup[move.FirstParticipantId] = move.TargetGroupId;
        participantToGroup[secondParticipantId] = move.SourceGroupId;
    }

    /// <summary>
    /// לוגיקת transfer על המילונים — משתתף אחד עובר קבוצה.
    /// </summary>
    private static void ApplyTransfer(
        Dictionary<ParticipantId, GroupId> participantToGroup,
        Dictionary<GroupId, List<ParticipantId>> groupToParticipants,
        MoveCandidate move)
    {
        var sourceParticipants = GetGroupParticipants(groupToParticipants, move.SourceGroupId);
        var targetParticipants = GetGroupParticipants(groupToParticipants, move.TargetGroupId);

        EnsureParticipantInGroup(sourceParticipants, move.FirstParticipantId, move.SourceGroupId);

        sourceParticipants.Remove(move.FirstParticipantId);
        targetParticipants.Add(move.FirstParticipantId);
        participantToGroup[move.FirstParticipantId] = move.TargetGroupId;
    }

    /// <summary>
    /// מחזיר רשימת משתתפים לקבוצה — זורק אם הקבוצה לא קיימת במצב.
    /// </summary>
    private static List<ParticipantId> GetGroupParticipants(
        Dictionary<GroupId, List<ParticipantId>> groupToParticipants,
        GroupId groupId)
    {
        // TryGetValue — בטוח יותר מ-groupToParticipants[groupId] שלא זורק KeyNotFoundException לא ברור.
        if (!groupToParticipants.TryGetValue(groupId, out var participants))
        {
            throw new ArgumentException($"Group '{groupId}' does not exist in the current state.");
        }

        return participants;
    }

    /// <summary>
    /// בודק שהמשתתף נמצא ברשימת הקבוצה לפני remove/swap.
    /// </summary>
    private static void EnsureParticipantInGroup(
        List<ParticipantId> participants,
        ParticipantId participantId,
        GroupId groupId)
    {
        if (!participants.Contains(participantId))
        {
            throw new ArgumentException(
                $"Participant '{participantId.Value}' is not assigned to group '{groupId}'.");
        }
    }
}
