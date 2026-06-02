using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.LocalSearch.RuntimeData;

/// <summary>
/// פרופיל מצב זמני של משתתף לפי החלוקה הנוכחית.
/// </summary>
/// <remarks>
/// <para>תפקיד: סיכום מצב העדפות משתתף ביחס לקבוצה הנוכחית — לזיהוי מועמדים ל-swap ול-scoring.</para>
/// <para>נקרא מ-: <see cref="RuntimeDataBuilder"/>, MoveGenerator ו-IAssignmentScorer עתידיים.</para>
/// </remarks>
public sealed class ParticipantRuntimeProfile
{
    /// <summary>
    /// יוצר פרופיל runtime למשתתף בודד.
    /// </summary>
    /// <param name="participantId">מזהה המשתתף.</param>
    /// <param name="currentGroupId">קבוצת השיבוץ הנוכחית.</param>
    /// <param name="preferredInsideCount">מספר מועדפים שנמצאים באותה קבוצה.</param>
    /// <param name="preferredOutsideCount">מספר מועדפים שנמצאים בקבוצות אחרות.</param>
    /// <param name="isIsolated">true אם יש העדפות אך אף מועדף לא בקבוצה.</param>
    /// <param name="isNearMiss">true אם יש מועדפים גם בפנים וגם בחוץ.</param>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="RuntimeDataBuilder"/> (BuildParticipantProfiles).</para>
    /// </remarks>
    public ParticipantRuntimeProfile(
        ParticipantId participantId,
        GroupId currentGroupId,
        int preferredInsideCount,
        int preferredOutsideCount,
        bool isIsolated,
        bool isNearMiss)
    {
        ParticipantId = participantId;
        CurrentGroupId = currentGroupId;
        PreferredInsideCount = preferredInsideCount;
        PreferredOutsideCount = preferredOutsideCount;
        // IsIsolated / IsNearMiss — דגלים לזיהוי מועמדים ל-moves בחיפוש המקומי.
        IsIsolated = isIsolated;
        IsNearMiss = isNearMiss;
    }

    /// <summary>
    /// מזהה המשתתף.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לבחירת משתתפים למועמדי move.</para>
    /// </remarks>
    public ParticipantId ParticipantId { get; }

    /// <summary>
    /// קבוצת השיבוץ הנוכחית של המשתתף.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לסינון moves לפי קבוצה.</para>
    /// </remarks>
    public GroupId CurrentGroupId { get; }

    /// <summary>
    /// מספר המועדפים שנמצאים באותה קבוצה עם המשתתף.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: IAssignmentScorer עתידי — לחישוב ציון העדפות.</para>
    /// </remarks>
    public int PreferredInsideCount { get; }

    /// <summary>
    /// מספר המועדפים שנמצאים בקבוצות אחרות.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לזיהוי פוטנציאל שיפור.</para>
    /// </remarks>
    public int PreferredOutsideCount { get; }

    /// <summary>
    /// למשתתף יש העדפות, אבל אף מועדף שלו לא נמצא איתו בקבוצה.
    /// </summary>
    /// <remarks>
    /// <para>תפקיד: דגל לזיהוי משתתפים מבודדים — מועמדים עדיפים ל-transfer/swap.</para>
    /// <para>נקרא מ-: MoveGenerator עתידי — אין שימוש חיצוני כרגע.</para>
    /// </remarks>
    public bool IsIsolated { get; }

    /// <summary>
    /// יש לפחות מועדף אחד בתוך הקבוצה ולפחות מועדף אחד מחוץ לקבוצה.
    /// </summary>
    /// <remarks>
    /// <para>תפקיד: דגל לזיהוי near-miss — פוטנציאל לשיפור ב-swap קטן.</para>
    /// <para>נקרא מ-: MoveGenerator עתידי — אין שימוש חיצוני כרגע.</para>
    /// </remarks>
    public bool IsNearMiss { get; }
}
