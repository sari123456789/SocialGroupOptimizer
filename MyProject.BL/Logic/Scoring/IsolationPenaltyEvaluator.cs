using System;
using System.Collections.Generic;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Logic.Scoring;

/// <summary>
/// מזהה משתתפים מבודדים חברתית בהקצאה ומחשב את הקנס המתאים.
/// </summary>
/// <remarks>
/// משתתף מבודד הוא משתתף שאף אחת מהעדפותיו החברתיות לא סופקה בהקצאה.
/// </remarks>
public sealed class IsolationPenaltyEvaluator
{
    /// <summary>
    /// מחשב את קנס הבידוד הכולל עבור ההקצאה.
    /// </summary>
    /// <param name="assignment">ההקצאה לניתוח.</param>
    /// <param name="participants">כלל המשתתפים כולל העדפותיהם.</param>
    /// <param name="penaltyPerIsolatedParticipant">ערך הקנס לכל משתתף מבודד.</param>
    /// <param name="weight">משקל הרכיב הזה בניקוד הכולל.</param>
    /// <returns>קנס הבידוד הכולל.</returns>
    /// <exception cref="ArgumentNullException">נזרק כאשר <paramref name="assignment"/> או <paramref name="participants"/> הוא null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">נזרק כאשר <paramref name="weight"/> אינו חיובי.</exception>
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

            if (participant.Preferences.Count == 0)
            {
                continue;
            }

            var hasAnySatisfiedPreference = false;
            foreach (var preference in participant.Preferences)
            {
                if (participantGroupLookup.TryGetValue(preference.PreferredParticipantId, out var preferredGroupId)
                    && participantGroupId == preferredGroupId)
                {
                    hasAnySatisfiedPreference = true;
                    break;
                }
            }

            if (!hasAnySatisfiedPreference)
            {
                isolatedCount++;
            }
        }

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
