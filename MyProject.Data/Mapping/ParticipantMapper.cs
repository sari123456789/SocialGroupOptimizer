using System;
using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Enums;
using MyProject.Core.Domain.ValueObjects;
using MyProject.Data.Models;
using CoreParticipant = MyProject.Core.Domain.Entities.Participant;
using CorePreference = MyProject.Core.Domain.Entities.Preference;
using DataParticipant = MyProject.Data.Models.Participant;

namespace MyProject.Data.Mapping;

/// <summary>
/// מיפוי מפורש משורות נתונים לישויות ליבה של משתתף.
/// </summary>
public static class ParticipantMapper
{
    /// <summary>
    /// ממפה משתתף משורות נתונים למשתתף בשכבת הליבה.
    /// </summary>
    /// <param name="participant">שורת משתתף.</param>
    /// <param name="participantClassifications">שורות סיווג משתתף.</param>
    /// <param name="classificationAttributes">מאפייני סיווג לצורך המרת שם למבנה הליבה.</param>
    /// <param name="socialPreferences">העדפות חברתיות הקשורות לאותה ריצת שיבוץ.</param>
    /// <param name="context">הקשר הממפה בין שיבוץ משתתף למזהה משתתף (מפתח מספרי בשכבת הנתונים).</param>
    /// <param name="participantIdentityByParticipantId">מיפוי ממפתח משתתף מספרי למזהה ליבה (מספר זהות).</param>
    /// <param name="assignmentId">מזהה ריצת השיבוץ לסינון העדפות.</param>
    /// <returns>משתתף בשכבת הליבה.</returns>
    /// <exception cref="ArgumentNullException">נזרק כאשר אחד מהפרמטרים הנדרשים הוא null.</exception>
    /// <exception cref="InvalidOperationException">נזרק כאשר הנתונים אינם ניתנים למיפוי חד משמעי.</exception>
    public static CoreParticipant MapToCoreParticipant(
        DataParticipant participant,
        IEnumerable<ParticipantClassification> participantClassifications,
        IEnumerable<ClassificationAttribute> classificationAttributes,
        IEnumerable<SocialPreference> socialPreferences,
        ParticipantAssignmentContext context,
        IReadOnlyDictionary<int, ParticipantId> participantIdentityByParticipantId,
        int assignmentId)
    {
        if (participant is null)
        {
            throw new ArgumentNullException(nameof(participant));
        }

        if (participantClassifications is null)
        {
            throw new ArgumentNullException(nameof(participantClassifications));
        }

        if (classificationAttributes is null)
        {
            throw new ArgumentNullException(nameof(classificationAttributes));
        }

        if (socialPreferences is null)
        {
            throw new ArgumentNullException(nameof(socialPreferences));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (participantIdentityByParticipantId is null)
        {
            throw new ArgumentNullException(nameof(participantIdentityByParticipantId));
        }

        if (participant.ParticipantId <= 0)
        {
            throw new InvalidOperationException("ParticipantId must be greater than zero.");
        }

        if (!participantIdentityByParticipantId.TryGetValue(participant.ParticipantId, out var participantId))
        {
            throw new InvalidOperationException($"Missing domain participant identity for ParticipantId {participant.ParticipantId}.");
        }

        var attributeById = classificationAttributes.ToDictionary(a => a.ClassificationAttributeId);
        var classifications = new List<ClassificationType>();

        foreach (var row in participantClassifications.Where(c => c.ParticipantId == participant.ParticipantId))
        {
            if (!attributeById.TryGetValue(row.ClassificationAttributeId, out var attribute))
            {
                throw new InvalidOperationException($"Missing ClassificationAttribute for id {row.ClassificationAttributeId}.");
            }

            classifications.Add(MapClassificationAttributeNameToCore(attribute.AttributeName));
        }

        if (classifications.Count == 0)
        {
            throw new InvalidOperationException("Participant must have at least one mapped classification.");
        }

        var prefsForParticipant = socialPreferences
            .Where(p => p.AssignmentId == assignmentId)
            .Where(p => context.GetParticipantId(p.FromParticipantAssignmentId) == participant.ParticipantId)
            .ToList();

        var preferences = MapPreferences(prefsForParticipant, context, participantIdentityByParticipantId, participantId);

        return new CoreParticipant(participantId, classifications, preferences);
    }

    /// <summary>
    /// בונה מילון מזהי משתתף מספריים (שכבת נתונים) ל-<see cref="ParticipantId"/> לאחר אימות מספר הזהות.
    /// </summary>
    /// <param name="participants">כל משתתפי הריצה או המערכת הדרושים לפתרון מזהים.</param>
    public static IReadOnlyDictionary<int, ParticipantId> CreateParticipantIdentityLookup(IEnumerable<DataParticipant> participants)
    {
        if (participants is null)
        {
            throw new ArgumentNullException(nameof(participants));
        }

        var dict = new Dictionary<int, ParticipantId>();
        foreach (var p in participants)
        {
            if (p is null)
            {
                throw new ArgumentException("Participants collection cannot contain null entries.", nameof(participants));
            }

            if (p.ParticipantId <= 0)
            {
                throw new InvalidOperationException("ParticipantId must be greater than zero for all participants.");
            }

            var coreId = new ParticipantId(
                p.IsraeliIdentityNumber ?? throw new InvalidOperationException(
                    $"IsraeliIdentityNumber is required for ParticipantId {p.ParticipantId}."));

            if (!dict.TryAdd(p.ParticipantId, coreId))
            {
                throw new InvalidOperationException($"Duplicate ParticipantId {p.ParticipantId} in participants list.");
            }
        }

        return dict;
    }

    private static List<CorePreference> MapPreferences(
        IReadOnlyList<SocialPreference> prefsForParticipant,
        ParticipantAssignmentContext context,
        IReadOnlyDictionary<int, ParticipantId> participantIdentityByParticipantId,
        ParticipantId selfId)
    {
        if (prefsForParticipant.Count == 0)
        {
            return new List<CorePreference>();
        }

        var ordered = prefsForParticipant
            .OrderBy(p => p.PreferenceWeight)
            .ThenBy(p => p.ToParticipantAssignmentId)
            .ToList();

        var ranks = new HashSet<int>();
        var preferredIds = new HashSet<ParticipantId>();

        var preferences = new List<CorePreference>(ordered.Count);
        var rank = 1;

        foreach (var pref in ordered)
        {
            if (ranks.Contains(rank))
            {
                throw new InvalidOperationException("Internal error: duplicate rank while mapping preferences.");
            }

            ranks.Add(rank);

            var toDbParticipantId = context.GetParticipantId(pref.ToParticipantAssignmentId);
            if (!participantIdentityByParticipantId.TryGetValue(toDbParticipantId, out var preferredParticipantId))
            {
                throw new InvalidOperationException($"Missing domain participant identity for ParticipantId {toDbParticipantId}.");
            }
            if (preferredParticipantId == selfId)
            {
                throw new InvalidOperationException("SocialPreference cannot target the same participant as the source.");
            }

            if (!preferredIds.Add(preferredParticipantId))
            {
                throw new InvalidOperationException("Duplicate preferred participant in social preferences for the same source.");
            }

            preferences.Add(new CorePreference(preferredParticipantId, rank));
            rank++;
        }

        return preferences;
    }

    private static ClassificationType MapClassificationAttributeNameToCore(string attributeName)
    {
        if (string.IsNullOrWhiteSpace(attributeName))
        {
            throw new InvalidOperationException("Classification attribute name cannot be empty.");
        }

        // מיפוי מבוסס שם בדיוק כמו שם הערך ב enum של הליבה.
        if (!Enum.TryParse<ClassificationType>(attributeName, ignoreCase: true, out var parsed))
        {
            throw new InvalidOperationException($"Unknown classification attribute mapping for '{attributeName}'.");
        }

        if (parsed == ClassificationType.Unspecified)
        {
            throw new InvalidOperationException("Mapped classification cannot be Unspecified.");
        }

        return parsed;
    }
}
