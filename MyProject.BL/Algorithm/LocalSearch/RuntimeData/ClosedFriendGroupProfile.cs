using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.LocalSearch.RuntimeData;

/// <summary>
/// פרופיל של קבוצת חברים סגורה — רכיב קשיר בגרף קשרים הדדיים.
/// </summary>
/// <remarks>
/// <para>תפקיד: תיאור קלאסטר חברים הדדיים — האם מפוצל בין קבוצות, צפיפות קשרים, ומיקום נוכחי.</para>
/// <para>נקרא מ-: <see cref="RuntimeDataBuilder"/>, MoveGenerator עתידי — לזיהוי קלאסטרים מפוצלים.</para>
/// </remarks>
public sealed class ClosedFriendGroupProfile
{
    /// <summary>
    /// יוצר פרופיל לקלאסטר חברים סגור.
    /// </summary>
    /// <param name="clusterId">מזהה הקלאסטר (מ-<see cref="FriendClusterIndex"/>).</param>
    /// <param name="members">רשימת חברי הקלאסטר.</param>
    /// <param name="mutualConnectionCount">מספר קשרים הדדיים בתוך הקלאסטר.</param>
    /// <param name="densityScore">צפיפות קשרים — 0..1 (קשרים קיימים / קשרים אפשריים).</param>
    /// <param name="currentGroupIds">קבוצות השיבוץ הנוכחיות של חברי הקלאסטר.</param>
    /// <param name="isSplitAcrossGroups">true אם חברי הקלאסטר מפוזרים ביותר מקבוצה אחת.</param>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="RuntimeDataBuilder"/> (BuildClosedFriendGroupProfiles).</para>
    /// </remarks>
    public ClosedFriendGroupProfile(
        int clusterId,
        IReadOnlyList<ParticipantId> members,
        int mutualConnectionCount,
        double densityScore,
        IReadOnlyList<GroupId> currentGroupIds,
        bool isSplitAcrossGroups)
    {
        if (members is null)
        {
            throw new ArgumentNullException(nameof(members));
        }

        if (currentGroupIds is null)
        {
            throw new ArgumentNullException(nameof(currentGroupIds));
        }

        // שדות read-only — snapshot של מצב הקלאסטר בזמן הבנייה.
        ClusterId = clusterId;
        Members = members;
        MutualConnectionCount = mutualConnectionCount;
        DensityScore = densityScore;
        CurrentGroupIds = currentGroupIds;
        IsSplitAcrossGroups = isSplitAcrossGroups;
    }

    /// <summary>
    /// מזהה הקלאסטר — ייחודי בתוך FriendClusterIndex.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לסינון קלאסטרים לפי מזהה.</para>
    /// </remarks>
    public int ClusterId { get; }

    /// <summary>
    /// רשימת חברי הקלאסטר (רכיב קשיר בגרף הדדי).
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לבחירת moves שמאחדים קלאסטר.</para>
    /// </remarks>
    public IReadOnlyList<ParticipantId> Members { get; }

    /// <summary>
    /// מספר קשרים הדדיים בתוך הקלאסטר.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: IAssignmentScorer עתידי — לחישוב ציון לכידות קלאסטר.</para>
    /// </remarks>
    public int MutualConnectionCount { get; }

    /// <summary>
    /// צפיפות קשרים — יחס קשרים קיימים לקשרים אפשריים (0..1).
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לעדיפות קלאסטרים צפופים.</para>
    /// </remarks>
    public double DensityScore { get; }

    /// <summary>
    /// קבוצות השיבוץ הנוכחיות של חברי הקלאסטר (ייחודיות, ממוינות).
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לזיהוי פיצול בין קבוצות.</para>
    /// </remarks>
    public IReadOnlyList<GroupId> CurrentGroupIds { get; }

    /// <summary>
    /// האם חברי הקלאסטר מפוזרים ביותר מקבוצה אחת.
    /// </summary>
    /// <remarks>
    /// <para>תפקיד: דגל לזיהוי קלאסטרים שיש לנסות לאחד.</para>
    /// <para>נקרא מ-: MoveGenerator עתידי — אין שימוש חיצוני כרגע.</para>
    /// </remarks>
    public bool IsSplitAcrossGroups { get; }
}
