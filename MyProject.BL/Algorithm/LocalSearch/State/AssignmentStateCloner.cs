using MyProject.Core.Domain.ValueObjects;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.LocalSearch.State;

/// <summary>
/// יוצר עותק עמוק של <see cref="AssignmentState"/>.
/// </summary>
/// <remarks>
/// נקרא מ-: <see cref="AssignmentStateUpdater.ApplyToClone"/> בלבד.
/// </remarks>
public static class AssignmentStateCloner
{
    /// <summary>
    /// שכפול עמוק — מילונים חדשים ורשימות משתתפים מועתקות.
    /// </summary>
    /// <param name="state">מצב המקור.</param>
    /// <returns>AssignmentState עצמאי — שינוי בו לא ישפיע על state.</returns>
    public static AssignmentState Clone(AssignmentState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        // ToDictionary על ParticipantToGroup:
        // יוצר מילון חדש; ערכי GroupId (struct) מועתקים בשכפול שטחי — זה תקין.
        var participantToGroup = state.ParticipantToGroup
            .ToDictionary(entry => entry.Key, entry => entry.Value);

        // ToDictionary + ToList על כל רשימת משתתפים:
        // חובה ToList — אחרת שני המילונים יחלקו את אותה List (שינוי ב-one ישפיע על ה-other).
        var groupToParticipants = state.GroupToParticipants
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value.ToList());

        // CurrentScore ו-CurrentHash מועתקים כמו שהם — struct/hash immutable.
        return new AssignmentState(
            participantToGroup,
            groupToParticipants,
            state.CurrentScore,
            state.CurrentHash);
    }
}
