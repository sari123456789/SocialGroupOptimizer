using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.LocalSearch.RuntimeData;

/// <summary>
/// snapshot read-only של נתוני ריצה — לא משנה את <see cref="SolutionState.AssignmentState"/>.
/// </summary>
/// <remarks>
/// <para>תפקיד: אגרגציה של כל האינדקסים והפרופילים לשימוש ב-MoveGenerator, MoveEvaluator ו-IAssignmentScorer.</para>
/// <para>נקרא מ-: <see cref="RuntimeDataBuilder.Build"/> (יצירה), מתזמר Local Search עתידי (צריכה).</para>
/// </remarks>
public sealed class RuntimeDataSnapshot
{
    /// <summary>
    /// יוצר snapshot read-only של כל נתוני הריצה.
    /// </summary>
    /// <param name="preferenceIndex">אינדקס העדפות סטטי.</param>
    /// <param name="mutualPreferenceIndex">אינדקס קשרים הדדיים.</param>
    /// <param name="friendClusterIndex">אינדקס קלאסטרי חברים.</param>
    /// <param name="dominantParticipantIndex">אינדקס משתתפים דומיננטיים.</param>
    /// <param name="unrequestedParticipantIndex">אינדקס משתתפים לא מבוקשים.</param>
    /// <param name="participantProfiles">פרופיל runtime לכל משתתף לפי החלוקה הנוכחית.</param>
    /// <param name="groupProfiles">פרופיל runtime לכל קבוצה.</param>
    /// <param name="closedFriendGroupProfiles">פרופילי קלאסטרי חברים סגורים.</param>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="RuntimeDataBuilder.Build"/> בלבד.</para>
    /// </remarks>
    public RuntimeDataSnapshot(
        ParticipantPreferenceIndex preferenceIndex,
        MutualPreferenceIndex mutualPreferenceIndex,
        FriendClusterIndex friendClusterIndex,
        DominantParticipantIndex dominantParticipantIndex,
        UnrequestedParticipantIndex unrequestedParticipantIndex,
        IReadOnlyDictionary<ParticipantId, ParticipantRuntimeProfile> participantProfiles,
        IReadOnlyDictionary<GroupId, GroupRuntimeProfile> groupProfiles,
        IReadOnlyList<ClosedFriendGroupProfile> closedFriendGroupProfiles)
    {
        // כל שדה חובה — snapshot הוא immutable; null כאן = באג ב-RuntimeDataBuilder.
        PreferenceIndex = preferenceIndex ?? throw new ArgumentNullException(nameof(preferenceIndex));
        MutualPreferenceIndex = mutualPreferenceIndex ?? throw new ArgumentNullException(nameof(mutualPreferenceIndex));
        FriendClusterIndex = friendClusterIndex ?? throw new ArgumentNullException(nameof(friendClusterIndex));
        DominantParticipantIndex = dominantParticipantIndex ?? throw new ArgumentNullException(nameof(dominantParticipantIndex));
        UnrequestedParticipantIndex = unrequestedParticipantIndex ?? throw new ArgumentNullException(nameof(unrequestedParticipantIndex));
        // פרופילים תלויי-חלוקה — נבנים מחדש אחרי כל move (בניגוד לאינדקסים הסטטיים).
        ParticipantProfiles = participantProfiles ?? throw new ArgumentNullException(nameof(participantProfiles));
        GroupProfiles = groupProfiles ?? throw new ArgumentNullException(nameof(groupProfiles));
        ClosedFriendGroupProfiles = closedFriendGroupProfiles ?? throw new ArgumentNullException(nameof(closedFriendGroupProfiles));
    }

    /// <summary>
    /// אינדקס העדפות סטטי — לא תלוי בחלוקה נוכחית.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator ו-IAssignmentScorer עתידיים — אין שימוש חיצוני כרגע.</para>
    /// </remarks>
    public ParticipantPreferenceIndex PreferenceIndex { get; }

    /// <summary>
    /// אינדקס קשרים הדדיים — A→B ו-B→A.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לזיהוי זוגות הדדיים — אין שימוש חיצוני כרגע.</para>
    /// </remarks>
    public MutualPreferenceIndex MutualPreferenceIndex { get; }

    /// <summary>
    /// אינדקס קלאסטרי חברים — רכיבים קשירים בגרף הדדי.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לזיהוי קלאסטרים מפוצלים — אין שימוש חיצוני כרגע.</para>
    /// </remarks>
    public FriendClusterIndex FriendClusterIndex { get; }

    /// <summary>
    /// אינדקס משתתפים דומיננטיים — הרבה העדפות נכנסות.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — אין שימוש חיצוני כרגע.</para>
    /// </remarks>
    public DominantParticipantIndex DominantParticipantIndex { get; }

    /// <summary>
    /// אינדקס משתתפים שלא קיבלו אף העדפה.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — אין שימוש חיצוני כרגע.</para>
    /// </remarks>
    public UnrequestedParticipantIndex UnrequestedParticipantIndex { get; }

    /// <summary>
    /// פרופיל runtime לכל משתתף — לפי החלוקה הנוכחית.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לבחירת משתתפים למועמדי move.</para>
    /// </remarks>
    public IReadOnlyDictionary<ParticipantId, ParticipantRuntimeProfile> ParticipantProfiles { get; }

    /// <summary>
    /// פרופיל runtime לכל קבוצה — גודל וחברים.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לבדיקת גודל קבוצה.</para>
    /// </remarks>
    public IReadOnlyDictionary<GroupId, GroupRuntimeProfile> GroupProfiles { get; }

    /// <summary>
    /// פרופילי קלאסטרי חברים סגורים — צפיפות ופיצול בין קבוצות.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: MoveGenerator עתידי — לזיהוי קלאסטרים לא מאוחדים.</para>
    /// </remarks>
    public IReadOnlyList<ClosedFriendGroupProfile> ClosedFriendGroupProfiles { get; }
}
