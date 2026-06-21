using System;
using System.Collections.Generic;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Logic.Scoring;

/// <summary>
/// תפקיד המחלקה: מדד עזר לזיהוי בידוד חברתי וחישוב קנס.
/// מדד תומך — אינו חלק מהציון הסופי הראשי (ScoringManager לא משתמש בו כרגע).
/// </summary>
/// <remarks>נקרא מ-: <see cref="ScoringManager"/> בלבד — שמור לשימוש עתידי.</remarks>
public sealed class IsolationPenaltyEvaluator
{
    /// <summary>
    /// מחשב קנס בידוד כולל להקצאה.
    /// </summary>
    /// <param name="assignment">החלוקה לניתוח.</param>
    /// <param name="participants">משתתפים והעדפות — לזיהוי בידוד.</param>
    /// <param name="penaltyPerIsolatedParticipant">ערך בסיס לכל משתתף מבודד (לפני משקל).</param>
    /// <param name="weight">מכפיל חיובי מ-<see cref="Configuration.ScoringWeights"/>.</param>
    /// <returns>קנס כולל כ-<see cref="Penalty"/>.</returns>
    /// <exception cref="ArgumentNullException">כאשר <paramref name="assignment"/> או <paramref name="participants"/> הוא null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">כאשר <paramref name="weight"/> אינו חיובי או <paramref name="penaltyPerIsolatedParticipant"/> שלילי.</exception>
    public Penalty Evaluate(
        Assignment assignment,
        IReadOnlyList<Participant> participants,
        double penaltyPerIsolatedParticipant,
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

        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be greater than zero.");
        }

        if (penaltyPerIsolatedParticipant < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(penaltyPerIsolatedParticipant), "Penalty per isolated participant cannot be negative.");
        }

        var participantGroupLookup = BuildParticipantGroupLookup(assignment);
        var isolatedCount = 0;

        foreach (var participant in participants)
        {
            if (!participantGroupLookup.TryGetValue(participant.Id, out var participantGroupId))
            {
                continue;
            }

            // משתתף ללא העדפות — לא נחשב מבודד (אין ציפייה חברתית).
            if (participant.Preferences.Count == 0)
            {
                continue;
            }

            // דגל: האם לפחות העדפה אחת מומשה בקבוצתו.
            var hasAnySatisfiedPreference = false;

            foreach (var preference in participant.Preferences)
            {
                // && — גם המועדף משובץ וגם באותה קבוצה.
                if (participantGroupLookup.TryGetValue(preference.PreferredParticipantId, out var preferredGroupId)
                    && participantGroupId == preferredGroupId)
                {
                    hasAnySatisfiedPreference = true;
                    break; // מספיק העדפה אחת — יוצאים מהלולאה הפנימית.
                }
            }

            // אין אף העדפה מומשה — משתתף מבודד חברתית.
            if (!hasAnySatisfiedPreference)
            {
                isolatedCount++;
            }
        }

        // נוסחה: מספר מבודדים × קנס בסיס × משקל.
        var totalPenalty = isolatedCount * penaltyPerIsolatedParticipant * weight;
        return new Penalty(totalPenalty);
    }

    private static Dictionary<Core.Domain.ValueObjects.ParticipantId, Core.Domain.ValueObjects.GroupId> BuildParticipantGroupLookup(Assignment assignment)
    {
        var lookup = new Dictionary<Core.Domain.ValueObjects.ParticipantId, Core.Domain.ValueObjects.GroupId>();

        foreach (var group in assignment.Groups)
        {
            foreach (var participantId in group.ParticipantIds)
            {
                lookup[participantId] = group.Id;
            }
        }

        return lookup;
    }
}