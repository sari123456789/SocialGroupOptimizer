using System;
using System.Collections.Generic;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Logic.Scoring;

/// <summary>
/// תפקיד המחלקה: חישוב אחוז מימוש ההעדפות החברתיות בחלוקה.
/// המחלקה משתתפת בשלב ניקוד — דרך ScoringManager.
/// </summary>
public sealed class SocialConnectionScorer
{
    /// <summary>
    /// תפקיד הפונקציה: מחשבת אחוז מימוש משקל העדפות מול המקסימום האפשרי.
    /// קלט עיקרי: חלוקה, משתתפים, משקל (לתאימות חוזה).
    /// פלט עיקרי: ציון בטווח 0–100.
    /// </summary>
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
        var actualScore = 0.0;
        var maxPossibleScore = 0.0;

        // שלב 1: עוברים על כל העדפה — מצטברים משקל מקסימלי ומשקל ממומש.
        foreach (var participant in participants)
        {
            foreach (var preference in participant.Preferences)
            {
                // דירוג 1 חזק יותר מדירוג 2 — משקל = 1/דירוג.
                var preferenceScore = PreferenceRankToScore(preference.Rank);
                maxPossibleScore += preferenceScore;

                if (!participantGroupLookup.TryGetValue(participant.Id, out var participantGroupId))
                {
                    continue;
                }

                if (!participantGroupLookup.TryGetValue(preference.PreferredParticipantId, out var preferredGroupId))
                {
                    continue;
                }

                if (participantGroupId == preferredGroupId)
                {
                    actualScore += preferenceScore;
                }
            }
        }

        if (maxPossibleScore == 0)
        {
            return new Score(0);
        }

        var percentage = actualScore / maxPossibleScore * 100.0;
        return new Score(Math.Clamp(percentage, 0.0, 100.0));
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
    /// תפקיד הפונקציה: ממיר דירוג העדפה למשקל — העדפה ראשונה חזקה יותר.
    /// קלט עיקרי: דירוג (Rank) חיובי.
    /// פלט עיקרי: משקל 1/דירוג.
    /// </summary>
    private static double PreferenceRankToScore(int rank) => 1.0 / rank;
}
