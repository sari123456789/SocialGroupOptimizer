using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.Initialization;

/// <summary>
/// אתחול נתונים סטטיים — בונה <see cref="AssignmentState"/> ראשוני מ-<see cref="Assignment"/> חוקי.
/// </summary>
/// <remarks>
/// <para>תפקיד: גשר בין תוצאת Initial Placement (Assignment ב-Core) לבין מבנה מצב החיפוש המקומי.</para>
/// <para>נקרא מ-: מתזמר Local Search  נקודת כניסה לבניית מצב ראשוני לפני לולאת החיפוש.</para>
/// </remarks>
public static class AssignmentStateFactory
{
    /// <summary>
    /// בונה <see cref="AssignmentState"/> מחלוקה חוקית קיימת.
    /// </summary>
    /// <param name="assignment">חלוקה מ-Initial Placement — כל משתתף מופיע בקבוצה אחת בלבד.</param>
    /// <returns>מצב חלוקה עם מיפויים, hash ראשוני וציון 0.</returns>
    /// <remarks>
    /// <para>נקרא מ-: מתזמר Local Search</para>
    /// </remarks>
    public static AssignmentState CreateFromAssignment(Assignment assignment)
    {
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        var participantToGroup = new Dictionary<ParticipantId, GroupId>(); //משתתף לקבוצה
        var groupToParticipants = new Dictionary<GroupId, List<ParticipantId>>();//קבוצה למשתתפים

        foreach (var group in assignment.Groups)
        {
            // OrderBy על Value — סדר יציב לרשימת משתתפים בכל קבוצה.
            var sortedParticipants = group.ParticipantIds
                .OrderBy(participantId => participantId.Value, StringComparer.Ordinal)
                .ToList();

            groupToParticipants[group.Id] = sortedParticipants;

            foreach (var participantId in group.ParticipantIds)
            {
                if (participantToGroup.ContainsKey(participantId))
                {
                    // משתתף ביותר מקבוצה אחת — קלט לא חוקי.
                    throw new ArgumentException(
                        $"Participant '{participantId.Value}' appears in more than one group.",
                        nameof(assignment));
                }

                participantToGroup[participantId] = group.Id;
            }
        }


        // מחשב Hash ראשוני לפי מיפוי המשתתפים לקבוצות.
        // הציון מאותחל ל-0 בלבד כערך זמני, ובהמשך מנגנון הניקוד מחשב ומעדכן את הציון האמיתי של החלוקה.
        var initialHash = AssignmentHash.ComputeFromState(participantToGroup);
        var initialScore = new Score(0);

        return new AssignmentState(
            participantToGroup,
            groupToParticipants,
            initialScore,
            initialHash);
    }
}
