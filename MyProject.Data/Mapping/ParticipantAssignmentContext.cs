using System;
using System.Collections.Generic;
using System.Linq;
using MyProject.Data.Models;

namespace MyProject.Data.Mapping;

/// <summary>
/// גשר בין מזהי מסד של "משתתף בשיבוץ" לבין משתתף (מפתח מסד) וריצת שיבוץ.
/// </summary>
/// <remarks>
/// <para>במסד, העדפות וסיווגים מצביעים על <c>ParticipantAssignmentId</c>, לא על מספר זהות.</para>
/// <para>בליבה, האלגוריתם עובד עם <c>ParticipantId</c> (מספר זהות) — לכן צריך תרגום דו־כיווני.</para>
/// <para>נבנה מכל שורות <c>ParticipantAssignments</c> של אותה ריצת שיבוץ.</para>
/// </remarks>
public sealed class ParticipantAssignmentContext
{
    // ParticipantAssignmentId → ParticipantId (מפתח מסד של אדם)
    private readonly IReadOnlyDictionary<int, int> _dbParticipantIdByParticipantAssignmentId;

    // (ParticipantId במסד, AssignmentId) → ParticipantAssignmentId
    private readonly IReadOnlyDictionary<(int DbParticipantId, int AssignmentId), int> _participantAssignmentIdByParticipantAndAssignment;

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

        if (rows.Any(r => r.AssignmentId <= 0))
        {
            throw new InvalidOperationException("AssignmentId must be greater than zero for all participant assignments.");
        }

        _dbParticipantIdByParticipantAssignmentId = rows.ToDictionary(
            r => r.ParticipantAssignmentId,
            r => r.ParticipantId);

        _participantAssignmentIdByParticipantAndAssignment = rows.ToDictionary(
            r => (r.ParticipantId, r.AssignmentId),
            r => r.ParticipantAssignmentId);
    }

    /// <summary>מזהה שיבוץ משתתף → מפתח משתתף פנימי במסד (לא מספר זהות).</summary>
    public int GetDbParticipantId(int participantAssignmentId)
    {
        if (!_dbParticipantIdByParticipantAssignmentId.TryGetValue(participantAssignmentId, out var dbParticipantId))
        {
            throw new InvalidOperationException($"Unknown ParticipantAssignmentId: {participantAssignmentId}");
        }

        return dbParticipantId;
    }

    /// <summary>מפתח משתתף במסד + מזהה שיבוץ → שורת ParticipantAssignment (לסינון סיווגים והעדפות).</summary>
    public int GetParticipantAssignmentId(int dbParticipantId, int assignmentDbId)
    {
        if (!_participantAssignmentIdByParticipantAndAssignment.TryGetValue((dbParticipantId, assignmentDbId), out var participantAssignmentId))
        {
            throw new InvalidOperationException(
                $"No ParticipantAssignment for DbParticipantId {dbParticipantId} in AssignmentId {assignmentDbId}.");
        }

        return participantAssignmentId;
    }
}
