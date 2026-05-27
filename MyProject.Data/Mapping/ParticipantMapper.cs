using System;
using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.ValueObjects;
using MyProject.Data.Models;
using CoreParticipant = MyProject.Core.Domain.Entities.Participant;
using CorePreference = MyProject.Core.Domain.Entities.Preference;
using DataParticipant = MyProject.Data.Models.Participant;

namespace MyProject.Data.Mapping;

/// <summary>
/// מיפוי משורות מסד לישות <see cref="CoreParticipant"/> בליבה.
/// </summary>
/// <remarks>
/// שרשרת סיווג: Participant (מסד) → ParticipantAssignment (שיבוץ) → ParticipantClassification → מילון מימד→רמה.
/// מספר זהות: IsraeliIdentityNumber → ParticipantId בליבה (לא מפתח ParticipantId במסד).
/// </remarks>
public static class ParticipantMapper
{
    /// <summary>
    /// בונה משתתף ליבה אחד לריצת שיבוץ: זהות, סיווגים (מילון), העדפות מדורגות.
    /// </summary>
    public static CoreParticipant MapToCoreParticipant(
        DataParticipant participant,
        IEnumerable<ParticipantClassification> participantClassifications,
        ClassificationCatalog catalog,
        IEnumerable<SocialPreference> socialPreferences,
        ParticipantAssignmentContext context,
        IReadOnlyDictionary<int, ParticipantId> participantIdentityByDbParticipantId,
        int assignmentDbId)
    {
        if (participant is null)
        {
            throw new ArgumentNullException(nameof(participant));
        }

        if (participantClassifications is null)
        {
            throw new ArgumentNullException(nameof(participantClassifications));
        }

        if (catalog is null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        if (socialPreferences is null)
        {
            throw new ArgumentNullException(nameof(socialPreferences));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (participantIdentityByDbParticipantId is null)
        {
            throw new ArgumentNullException(nameof(participantIdentityByDbParticipantId));
        }

        var dbParticipantId = participant.ParticipantId;
        if (dbParticipantId <= 0)
        {
            throw new InvalidOperationException("ParticipantId must be greater than zero.");
        }

        if (!participantIdentityByDbParticipantId.TryGetValue(dbParticipantId, out var participantId))
        {
            throw new InvalidOperationException($"Missing domain participant identity for DbParticipantId {dbParticipantId}.");
        }

        // אותו אדם יכול להופיע בכמה שיבוצים — סיווגים נלקחים רק מהשורה של השיבוץ הנוכחי.
        var participantAssignmentId = context.GetParticipantAssignmentId(dbParticipantId, assignmentDbId);
        var classifications = new Dictionary<ClassificationDimensionCode, ClassificationLevelCode>();

        foreach (var row in participantClassifications.Where(c => c.ParticipantAssignmentId == participantAssignmentId))
        {
            // כל שורה = מימד אחד + רמה אחת; TryAdd נכשל אם יש שתי שורות לאותו מימד.
            var (dimension, level) = catalog.ResolveParticipantClassification(row);
            if (!classifications.TryAdd(dimension, level))
            {
                throw new InvalidOperationException(
                    $"Duplicate classification dimension '{dimension}' for ParticipantAssignmentId {participantAssignmentId}.");
            }
        }

        if (classifications.Count == 0)
        {
            throw new InvalidOperationException("Participant must have at least one mapped classification.");
        }

        var prefsForParticipant = socialPreferences
            .Where(p => p.AssignmentId == assignmentDbId)
            .Where(p => context.GetDbParticipantId(p.FromParticipantAssignmentId) == dbParticipantId)
            .ToList();

        var preferences = MapPreferences(prefsForParticipant, context, participantIdentityByDbParticipantId, participantId);

        return new CoreParticipant(participantId, classifications, preferences);
    }

    /// <summary>
    /// מילון: מפתח משתתף במסד (int) → מספר זהות בליבה. נדרש לפני כל מיפוי זוגות/העדפות/סיווגים.
    /// </summary>
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
        IReadOnlyDictionary<int, ParticipantId> participantIdentityByDbParticipantId,
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

        var preferredIds = new HashSet<ParticipantId>();
        var preferences = new List<CorePreference>(ordered.Count);
        var rank = 1;

        foreach (var pref in ordered)
        {
            var toDbParticipantId = context.GetDbParticipantId(pref.ToParticipantAssignmentId);
            if (!participantIdentityByDbParticipantId.TryGetValue(toDbParticipantId, out var preferredParticipantId))
            {
                throw new InvalidOperationException($"Missing domain participant identity for DbParticipantId {toDbParticipantId}.");
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
}
