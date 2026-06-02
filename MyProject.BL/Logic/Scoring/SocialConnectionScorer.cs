using System;
using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Logic.Scoring;

/// <summary>
/// מחשב ציון חיובי לפי מידת סיפוק העדפות החברתיות בחלוקה.
/// </summary>
/// <remarks>נקרא מ-: <see cref="ScoringManager"/> בלבד.</remarks>
public sealed class SocialConnectionScorer
{
    /// <summary>
    /// מחשב ציון חיבורים חברתיים מוכפל במשקל.
    /// </summary>
    /// <param name="assignment">החלוקה לניתוח — ממנה נבנה מיפוי משתתף→קבוצה.</param>
    /// <param name="participants">משתתפים והעדפותיהם.</param>
    /// <param name="weight">מכפיל חיובי לציון הגולמי לפני החזרה.</param>
    /// <returns>ציון חברתי כ-<see cref="Score"/> (סכום תרומות × משקל).</returns>
    /// <exception cref="ArgumentNullException">כאשר <paramref name="assignment"/> או <paramref name="participants"/> הוא null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">כאשר <paramref name="weight"/> אינו חיובי.</exception>
    public Score Calculate(
        Assignment assignment,
        IReadOnlyList<Participant> participants,
        double weight)
    {
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        if (participants is null)
        {
            throw new ArgumentNullException(nameof(participants));
        }

        // weight חייב להיות חיובי — משקל שלילי היה הופך את הציון לקנס.
        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be greater than zero.");
        }

        // מילון O(1) לחיפוש קבוצה לפי ParticipantId — נבנה פעם אחת לפני הלולאות.
        var participantGroupLookup = BuildParticipantGroupLookup(assignment);
        var totalScore = 0.0;

        foreach (var participant in participants)
        {
            // TryGetValue — אם המשתתף לא משובץ, מדלגים (אין לו העדפות לבדוק בקבוצה).
            if (!participantGroupLookup.TryGetValue(participant.Id, out var participantGroupId))
            {
                continue;
            }

            foreach (var preference in participant.Preferences)
            {
                // בודקים אם המועדף גם משובץ; אם לא — ההעדפה לא יכולה להתממש.
                if (!participantGroupLookup.TryGetValue(preference.PreferredParticipantId, out var preferredGroupId))
                {
                    continue;
                }

                // == — אותה קבוצה = העדפה מומשה; מוסיפים תרומה לפי דרגת העדפה.
                if (participantGroupId == preferredGroupId)
                {
                    totalScore += PreferenceRankToScore(preference.Rank);
                }
            }
        }

        // Score — Value Object ב-Core; totalScore * weight — ציון גולמי כפול משקל.
        return new Score(totalScore * weight);
    }

    private static Dictionary<ParticipantId, GroupId> BuildParticipantGroupLookup(Assignment assignment)
    {
        var lookup = new Dictionary<ParticipantId, GroupId>();

        // לולאה כפולה: לכל קבוצה, לכל משתתף — רושמים את הקבוצה שלו במילון.
        foreach (var group in assignment.Groups)
        {
            foreach (var participantId in group.ParticipantIds)
            {
                lookup[participantId] = group.Id;
            }
        }

        return lookup;
    }

    // דרגה 1 תורמת 1.0, דרגה 2 תורמת 0.5, וכן הלאה (1/rank).
    private static double PreferenceRankToScore(int rank)
    {
        return 1.0 / rank;
    }
}
