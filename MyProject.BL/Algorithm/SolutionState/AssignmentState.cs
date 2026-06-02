using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.SolutionState;

/// <summary>
/// מצב פתרון — מיפוי שיבוץ, hash ייחודי וציון נוכחי.
/// </summary>
/// <remarks>
/// <para>תפקיד: ייצוג מוטable של חלוקה במהלך חיפוש מקומי — מילוני שיבוץ, ציון ו-hash לזיהוי מצבים.</para>
/// <para>נקרא מ-: <see cref="Initialization.AssignmentStateFactory"/>,
/// <see cref="LocalSearch.State.AssignmentStateCloner"/>,
/// <see cref="LocalSearch.State.AssignmentStateUpdater"/>,
/// <see cref="LocalSearch.State.AssignmentStateConverter"/>,
/// <see cref="LocalSearch.RuntimeData.RuntimeDataBuilder"/>,
/// <see cref="RuntimeState.RuntimeStateManager"/>.</para>
/// <para>אין לשנות את המילונים ישירות — יש להשתמש ב-<see cref="LocalSearch.State.AssignmentStateUpdater"/>.</para>
/// </remarks>
public sealed class AssignmentState
{
    /// <summary>
    /// יוצר מצב חלוקה — שני מילונים מirror + ציון + hash.
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

        // שמירת references למילונים — AssignmentStateUpdater משנה אותם in-place.
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
