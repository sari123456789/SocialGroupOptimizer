using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.SolutionState;

/// <summary>
/// מזהה hash יציב לחלוקה — XOR של תרומות (ParticipantId, GroupId).
/// </summary>
/// <remarks>
/// <para>תפקיד: זיהוי מהיר ועקבי של מצב שיבוץ — מאפשר עדכון incrementally אחרי swap/transfer ומניעת חזרה על מצבים.</para>
/// <para>נקרא מ-: <see cref="Initialization.AssignmentStateFactory"/>,
/// <see cref="LocalSearch.State.AssignmentStateUpdater"/>,
/// <see cref="RuntimeState.VisitedStateTracker"/>,
/// <see cref="RuntimeState.RuntimeStateManager"/>.</para>
/// </remarks>
public readonly record struct AssignmentHash
{
    private const ulong FnvOffsetBasis = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;

    /// <summary>
    /// יוצר hash מערך ulong גולמי.
    /// </summary>
    /// <param name="value">ערך ה-hash המשולב (XOR של כל התרומות).</param>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="ComputeFromState"/>, <see cref="ApplyTransfer"/>, <see cref="ApplySwap"/>.</para>
    /// </remarks>
    public AssignmentHash(ulong value)
    {
        Value = value;
    }

    /// <summary>
    /// ערך ה-hash הגולמי.
    /// </summary>
    /// <remarks>
    /// <para>תפקיד: אחסון ערך XOR לשימוש ב-HashSet ובהשוואות.</para>
    /// <para>נקרא מ-: <see cref="ApplyTransfer"/>, <see cref="ApplySwap"/>,
    /// <see cref="RuntimeState.VisitedStateTracker"/>.</para>
    /// </remarks>
    public ulong Value { get; }

    /// <summary>
    /// מחשב תרומת hash לזוג (משתתף, קבוצה) בודד.
    /// </summary>
    /// <param name="participantId">מזהה המשתתף.</param>
    /// <param name="groupId">מזהה הקבוצה.</param>
    /// <returns>ערך FNV-1a 64-bit של המפתח המורכב.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="ComputeFromState"/>, <see cref="ApplyTransfer"/>, <see cref="ApplySwap"/>.</para>
    /// </remarks>
    public static ulong ComputeContribution(ParticipantId participantId, GroupId groupId)
    {
        // מפתח טקסטואלי ייחודי לזוג — מפריד | מונע התנגשויות (למשל "1|23" לעומת "12|3").
        var key = FormContributionKey(participantId, groupId);
        // FNV-1a 64-bit — hash מהיר ויציב לכל תרומה בודדת.
        return ComputeFnv1a64(key);
    }

    /// <summary>
    /// מחשב hash מלא ממיפוי משתתף-לקבוצה.
    /// </summary>
    /// <param name="participantToGroup">מיפוי השיבוץ הנוכחי.</param>
    /// <returns>hash משולב (XOR) של כל התרומות.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="Initialization.AssignmentStateFactory.CreateFromAssignment"/>,
    /// <see cref="Compute"/>.</para>
    /// </remarks>
    public static AssignmentHash ComputeFromState(
        IReadOnlyDictionary<ParticipantId, GroupId> participantToGroup)
    {
        if (participantToGroup is null)
        {
            throw new ArgumentNullException(nameof(participantToGroup));
        }

        // XOR של כל התרומות — סדר העברה לא משנה (תכונה מתמטית של XOR).
        var combined = 0UL;

        foreach (var entry in participantToGroup)
        {
            combined ^= ComputeContribution(entry.Key, entry.Value);
        }

        return new AssignmentHash(combined);
    }

    /// <summary>
    /// מעדכן hash incrementally לאחר העברת משתתף בין קבוצות.
    /// </summary>
    /// <param name="currentHash">hash לפני השינוי.</param>
    /// <param name="participantId">המשתתף שהועבר.</param>
    /// <param name="oldGroupId">קבוצת המקור.</param>
    /// <param name="newGroupId">קבוצת היעד.</param>
    /// <returns>hash מעודכן — XOR מחיקת תרומה ישנה והוספת חדשה.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="LocalSearch.State.AssignmentStateUpdater.ApplyTransferInPlace"/>.</para>
    /// </remarks>
    public static AssignmentHash ApplyTransfer(
        AssignmentHash currentHash,
        ParticipantId participantId,
        GroupId oldGroupId,
        GroupId newGroupId)
    {
        // XOR פעמיים: מוחקים תרומה ישנה, מוסיפים חדשה — בלי לעבור על כל המשתתפים.
        var updated = currentHash.Value;
        updated ^= ComputeContribution(participantId, oldGroupId);
        updated ^= ComputeContribution(participantId, newGroupId);
        return new AssignmentHash(updated);
    }

    /// <summary>
    /// מעדכן hash incrementally לאחר החלפה (swap) בין שני משתתפים.
    /// </summary>
    /// <param name="currentHash">hash לפני השינוי.</param>
    /// <param name="firstParticipantId">משתתף ראשון (מקבוצת מקור).</param>
    /// <param name="firstGroupId">קבוצת המקור של המשתתף הראשון.</param>
    /// <param name="secondParticipantId">משתתף שני (מקבוצת יעד).</param>
    /// <param name="secondGroupId">קבוצת המקור של המשתתף השני.</param>
    /// <returns>hash מעודכן לאחר ארבע פעולות XOR.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="LocalSearch.State.AssignmentStateUpdater.ApplySwapInPlace"/>.</para>
    /// </remarks>
    public static AssignmentHash ApplySwap(
        AssignmentHash currentHash,
        ParticipantId firstParticipantId,
        GroupId firstGroupId,
        ParticipantId secondParticipantId,
        GroupId secondGroupId)
    {
        // ארבע פעולות XOR — כל משתתף עוזב קבוצה אחת ונכנס לשנייה.
        var updated = currentHash.Value;
        updated ^= ComputeContribution(firstParticipantId, firstGroupId);
        updated ^= ComputeContribution(firstParticipantId, secondGroupId);
        updated ^= ComputeContribution(secondParticipantId, secondGroupId);
        updated ^= ComputeContribution(secondParticipantId, firstGroupId);
        return new AssignmentHash(updated);
    }

    /// <summary>
    /// מחשב hash מלא מ-<see cref="Assignment"/> — לתאימות ובדיקות.
    /// </summary>
    /// <param name="assignment">חלוקה מלאה מ-Core.</param>
    /// <returns>hash יציב של החלוקה.</returns>
    /// <remarks>
    /// <para>נקרא מ-: בדיקות יחידה ואימות תוצאות — אין שימוש בזרימת Local Search בזמן ריצה.</para>
    /// </remarks>
    public static AssignmentHash Compute(Assignment assignment)
    {
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        // בונים מילון participant→group מתוך Assignment (מבנה קבוצות) לפני חישוב XOR.
        var participantToGroup = new Dictionary<ParticipantId, GroupId>();

        foreach (var group in assignment.Groups)
        {
            foreach (var participantId in group.ParticipantIds)
            {
                participantToGroup[participantId] = group.Id;
            }
        }

        return ComputeFromState(participantToGroup);
    }

    private static string FormContributionKey(ParticipantId participantId, GroupId groupId) =>
        $"{participantId.Value}|{groupId.Value}";

    private static ulong ComputeFnv1a64(string value)
    {
        // אלגוריתם FNV-1a: XOR תו → כפל ב-prime — סטנדרט ל-hash מהיר של מחרוזות.
        var hash = FnvOffsetBasis;

        foreach (var character in value)
        {
            hash ^= character;
            hash *= FnvPrime;
        }

        return hash;
    }

    /// <summary>
    /// מחזיר ייצוג הקסadecimal של ערך ה-hash (16 תווים).
    /// </summary>
    /// <returns>מחרוזת hex של Value.</returns>
    /// <remarks>
    /// <para>נקרא מ-: לוגים ודיבוג — אין שימוש בלוגיקת האלגוריתם.</para>
    /// </remarks>
    public override string ToString() => Value.ToString("X16");
}
