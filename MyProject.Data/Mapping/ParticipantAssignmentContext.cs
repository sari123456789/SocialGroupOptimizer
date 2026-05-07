using System;
using System.Collections.Generic;
using System.Linq;
using MyProject.Data.Models;

namespace MyProject.Data.Mapping;

/// <summary>
/// הקשר זיהוי לשורות שיבוץ משתתף בריצת שיבוץ מסוימת.
/// </summary>
public sealed class ParticipantAssignmentContext
{
    private readonly IReadOnlyDictionary<int, int> _dbParticipantIdByParticipantAssignmentId;

    /// <summary>
    /// מאתחל הקשר מתוך רשימת שורות שיבוץ משתתף.
    /// </summary>
    /// <param name="participantAssignments">שורות שיבוץ משתתף.</param>
    /// <exception cref="ArgumentNullException">נזרק כאשר <paramref name="participantAssignments"/> הוא null.</exception>
    /// <exception cref="InvalidOperationException">נזרק כאשר יש כפילות במפתחות או מזהה לא חוקי.</exception>
    public ParticipantAssignmentContext(IEnumerable<ParticipantAssignment> participantAssignments)
    {
        if (participantAssignments is null)
        {
            throw new ArgumentNullException(nameof(participantAssignments));
        }

        var rows = participantAssignments.ToList();
        if (rows.Any(r => r is null))
        {
            throw new InvalidOperationException("ParticipantAssignments cannot contain null rows.");
        }

        if (rows.Select(r => r.ParticipantAssignmentId).Distinct().Count() != rows.Count)
        {
            throw new InvalidOperationException("ParticipantAssignmentId values must be unique.");
        }

        if (rows.Any(r => r.ParticipantId <= 0))
        {
            throw new InvalidOperationException("ParticipantId must be greater than zero for all participant assignments.");
        }

        _dbParticipantIdByParticipantAssignmentId = rows.ToDictionary(
            r => r.ParticipantAssignmentId,
            r => r.ParticipantId);
    }

    /// <summary>
    /// מחזיר את מזהה המשתתף הפנימי במסד לפי מזהה שיבוץ משתתף.
    /// </summary>
    /// <param name="participantAssignmentId">מזהה שיבוץ משתתף.</param>
    /// <returns>מזהה משתתף פנימי במסד.</returns>
    /// <exception cref="InvalidOperationException">נזרק כאשר המזהה לא קיים בהקשר.</exception>
    public int GetDbParticipantId(int participantAssignmentId)
    {
        if (!_dbParticipantIdByParticipantAssignmentId.TryGetValue(participantAssignmentId, out var dbParticipantId))
        {
            throw new InvalidOperationException($"Unknown ParticipantAssignmentId: {participantAssignmentId}");
        }

        return dbParticipantId;
    }
}
