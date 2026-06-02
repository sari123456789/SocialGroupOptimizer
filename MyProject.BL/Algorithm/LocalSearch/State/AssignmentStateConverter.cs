using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.LocalSearch.State;

/// <summary>
/// המרה מ-<see cref="AssignmentState"/> חזרה ל-<see cref="Assignment"/> ב-Core.
/// </summary>
/// <remarks>
/// נקרא מ-: מתזמר Local Search עתידי, שכבת API — אין שימוש חיצוני כרגע.
/// </remarks>
public static class AssignmentStateConverter
{
    /// <summary>
    /// ממיר מצב חלוקה ל-<see cref="Assignment"/> ב-Core.
    /// </summary>
    /// <param name="state">מצב החלוקה הנוכחי (מילונים דו-כיווניים + hash + score).</param>
    /// <returns>Assignment עם קבוצות ממוינות; קבוצות ריקות מסוננות.</returns>
    public static Assignment ToAssignment(AssignmentState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        // מעבירים רק את GroupToParticipants — זה מספיק לבניית Assignment.
        // ParticipantToGroup הוא מראה (mirror) לאותו מידע בכיוון ההפוך.
        return ToAssignment(state.GroupToParticipants);
    }

    /// <summary>
    /// גרסה פנימית — מקבלת ישירות את מילון הקבוצות.
    /// </summary>
    /// <param name="groupToParticipants">מפתח = GroupId, ערך = רשימת משתתפים בקבוצה.</param>
    /// <returns>Assignment חוקי ב-Core.</returns>
    internal static Assignment ToAssignment(
        IReadOnlyDictionary<GroupId, List<ParticipantId>> groupToParticipants)
    {
        if (groupToParticipants is null)
        {
            throw new ArgumentNullException(nameof(groupToParticipants));
        }

        // LINQ pipeline:
        // 1) Where — מסנן קבוצות ריקות (אחרי moves ייתכנו קבוצות בלי משתתפים).
        // 2) OrderBy — סדר יציב לפי מספר קבוצה (GroupId.Value).
        // 3) Select — כל entry הופך ל-Group(entry.Key, entry.Value).
        // 4) new Assignment(groups) — ctor ב-Core מקבל IEnumerable<Group>.
        var groups = groupToParticipants
            .Where(entry => entry.Value.Count > 0)
            .OrderBy(entry => entry.Key.Value)
            .Select(entry => new Group(entry.Key, entry.Value));

        return new Assignment(groups);
    }
}
