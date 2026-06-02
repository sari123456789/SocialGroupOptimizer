using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.LocalSearch.RuntimeData;

/// <summary>
/// פרופיל בסיסי של קבוצה לפי החלוקה הנוכחית.
/// </summary>
/// <remarks>
/// <para>תפקיד: snapshot של חברי קבוצה וגודלה — לבדיקות גודל קבוצה וליצירת moves.</para>
/// <para>נקרא מ-: <see cref="RuntimeDataBuilder"/>, MoveGenerator ו-IAssignmentScorer עתידיים.</para>
/// </remarks>
public sealed class GroupRuntimeProfile
{
    /// <summary>
    /// יוצר פרופיל runtime לקבוצה בודדת.
    /// </summary>
    /// <param name="groupId">מזהה הקבוצה.</param>
    /// <param name="participants">רשימת משתתפי הקבוצה (עותק).</param>
    /// <param name="size">גודל הקבוצה — מספר המשתתפים.</param>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="RuntimeDataBuilder"/> (BuildGroupProfiles).</para>
    /// </remarks>
    public GroupRuntimeProfile(
        GroupId groupId,
        IReadOnlyList<ParticipantId> participants,
        int size)
    {
        if (participants is null)
        {
            throw new ArgumentNullException(nameof(participants));
        }

        GroupId = groupId;
        Participants = participants;
        // Size — עותק מ-explicit count; לא תלוי ב-Participants.Count אם יועברו שדות נפרדים בעתיד.
        Size = size;
    }

    /// <summary>
    /// מזהה הקבוצה.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לזיהוי קבוצות מקור/יעד.</para>
    /// </remarks>
    public GroupId GroupId { get; }

    /// <summary>
    /// רשימת משתתפי הקבוצה (read-only).
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לבחירת זוגות swap.</para>
    /// </remarks>
    public IReadOnlyList<ParticipantId> Participants { get; }

    /// <summary>
    /// גודל הקבוצה — מספר המשתתפים.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: IAssignmentScorer ו-ConstraintEngine עתידיים — לבדיקת אילוצי גודל.</para>
    /// </remarks>
    public int Size { get; }
}
