using System;
using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Logic.Scoring;

/// <summary>
/// מחשב כמה העדפות חברתיות של משתתפים סופקו בהקצאה.
/// </summary>
public sealed class SocialConnectionScorer
{
    /// <summary>
    /// מחשב ציון חיבורים חברתיים עבור ההקצאה.
    /// </summary>
    /// <remarks>
    /// ציון גבוה יותר מייצג סיפוק רב יותר של העדפות חברתיות.
    /// העדפות בדרגה נמוכה (גבוה יותר בחשיבות) תורמות יותר לציון.
    /// </remarks>
    /// <param name="assignment">ההקצאה לניתוח.</param>
    /// <param name="participants">כלל המשתתפים כולל העדפותיהם.</param>
    /// <param name="weight">משקל הרכיב הזה בניקוד הכולל.</param>
    /// <returns>ציון חיבורים חברתיים לפני שקלול.</returns>
    /// <exception cref="ArgumentNullException">נזרק כאשר <paramref name="assignment"/> או <paramref name="participants"/> הוא null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">נזרק כאשר <paramref name="weight"/> אינו חיובי.</exception>
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

        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be greater than zero.");
        }

        var participantGroupLookup = BuildParticipantGroupLookup(assignment);
        var totalScore = 0.0;

        foreach (var participant in participants)
        {
            if (!participantGroupLookup.TryGetValue(participant.Id, out var participantGroupId))
            {
                continue;
            }

            foreach (var preference in participant.Preferences)
            {
                if (!participantGroupLookup.TryGetValue(preference.PreferredParticipantId, out var preferredGroupId))
                {
                    continue;
                }

                if (participantGroupId == preferredGroupId)
                {
                    totalScore += PreferenceRankToScore(preference.Rank);
                }
            }
        }

        return new Score(totalScore * weight);
    }

    private static Dictionary<ParticipantId, GroupId> BuildParticipantGroupLookup(Assignment assignment)
    {
        var lookup = new Dictionary<ParticipantId, GroupId>();

        foreach (var group in assignment.Groups)
        {
            foreach (var participantId in group.ParticipantIds)
            {
                lookup[participantId] = group.Id;
            }
        }

        return lookup;
    }

    /// <summary>
    /// ממיר דרגת העדפה לציון: דרגה 1 שווה יותר מדרגה 2 וכו'.
    /// </summary>
    private static double PreferenceRankToScore(int rank)
    {
        return 1.0 / rank;
    }
}
