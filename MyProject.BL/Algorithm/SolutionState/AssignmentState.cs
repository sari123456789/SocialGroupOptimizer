using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.SolutionState;

/// <summary>
/// תפקיד המחלקה: מצב חלוקה פנימי לזמן ריצה — שני מיפויים מראים זה את זה.
/// המחלקה משתתפת בכל שלבי החיפוש המקומי.
/// </summary>
/// <remarks>
/// <para>ParticipantToGroup — משתתף לקבוצה: שליפה מהירה O(1) לפי משתתף.</para>
/// <para>GroupToParticipants — קבוצה למשתתפים: עדכון יעיל של רשימת חברי קבוצה.</para>
/// <para>שני הכיוונים נדרשים כי כל מהלך מעדכן גם את המשתתף וגם את רשימת הקבוצה.</para>
/// </remarks>
public sealed class AssignmentState
{
    /// <summary>

    /// </summary>
    public AssignmentState(
        Dictionary<ParticipantId, GroupId> participantToGroup,
        Dictionary<GroupId, List<ParticipantId>> groupToParticipants,
        Score currentScore,
        AssignmentHash currentHash)
    {
        if (participantToGroup is null)
        {
            throw new ArgumentNullException(nameof(participantToGroup));
        }

        if (groupToParticipants is null)
        {
            throw new ArgumentNullException(nameof(groupToParticipants));
        }

        // חלוקה ריקה אינה מצב חוקי ל-Local Search.
        if (participantToGroup.Count == 0)
        {
            throw new ArgumentException("Assignment state must contain at least one participant.", nameof(participantToGroup));
        }

        
        ParticipantToGroup = participantToGroup;
        GroupToParticipants = groupToParticipants;
        CurrentScore = currentScore;
        CurrentHash = currentHash;
    }

    /// <summary>
    /// משתתף → קבוצה. lookup O(1).
    /// </summary>
    /// <remarks>אין לערוך ישירות — רק דרך <see cref="LocalSearch.State.AssignmentStateUpdater"/>.</remarks>
    public Dictionary<ParticipantId, GroupId> ParticipantToGroup { get; }

    /// <summary>
    /// קבוצה → רשימת משתתפים.
    /// </summary>
    /// <remarks>אין לערוך ישירות — רק דרך <see cref="LocalSearch.State.AssignmentStateUpdater"/>.</remarks>
    public Dictionary<GroupId, List<ParticipantId>> GroupToParticipants { get; }

    /// <summary>
    /// ציון נוכחי — יתעדכן ע"י מנוע ניקוד אחרי כל move (עתידי).
    /// </summary>
    public Score CurrentScore { get; set; }

    /// <summary>
    /// hash לזיהוי מצב — מתעדכן incrementally ב-AssignmentStateUpdater.
    /// </summary>
    public AssignmentHash CurrentHash { get; set; }
}
