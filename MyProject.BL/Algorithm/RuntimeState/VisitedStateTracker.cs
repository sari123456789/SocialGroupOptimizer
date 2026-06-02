using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.RuntimeState;

/// <summary>
/// עוקב אחר מצבי חלוקה שכבר נבדקו במהלך החיפוש.
/// </summary>
/// <remarks>
/// <para>תפקיד: מניעת חזרה על מצבי שיבוץ זהים (לפי hash) בלולאת Local Search.</para>
/// <para>נקרא מ-: <see cref="RuntimeStateManager"/> בלבד.</para>
/// </remarks>
public sealed class VisitedStateTracker
{
    private readonly HashSet<AssignmentHash> _visitedHashes = new();

    /// <summary>
    /// בודק האם מצב hash כבר נבדק בעבר.
    /// </summary>
    /// <param name="hash">hash של מצב החלוקה.</param>
    /// <returns>true אם המצב כבר נראה; אחרת false.</returns>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="RuntimeStateManager.HasSeenCurrentState"/>.</para>
    /// </remarks>
    public bool HasSeen(AssignmentHash hash) =>
        // Contains — O(1) ב-HashSet.
        _visitedHashes.Contains(hash);

    /// <summary>
    /// מסמן מצב hash כמצב שכבר נבדק.
    /// </summary>
    /// <param name="hash">hash של מצב החלוקה לסימון.</param>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="RuntimeStateManager.MarkCurrentAsVisited"/>.</para>
    /// </remarks>
    public void MarkSeen(AssignmentHash hash) => _visitedHashes.Add(hash);

    /// <summary>
    /// מספר המצבים הייחודיים שסומנו עד כה.
    /// </summary>
    /// <remarks>
    /// <para>תפקיד: מדד לעומק/כיסוי החיפוש — לוגים ודיבוג.</para>
    /// <para>נקרא מ-: מתזמר Local Search עתידי — אין שימוש חיצוני כרגע.</para>
    /// </remarks>
    public int Count => _visitedHashes.Count;
}
