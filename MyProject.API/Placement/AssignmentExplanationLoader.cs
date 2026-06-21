using Microsoft.EntityFrameworkCore;
using MyProject.BL.Transparency;
using MyProject.Core.Domain.ValueObjects;
using MyProject.Data;

namespace MyProject.API.Placement;

/// <summary>
/// טוען נתוני חלוקה ומפעיל את שכבת השקיפות עבור משתתף יחיד.
/// </summary>
/// <remarks>
/// <para>Read Only מוחלט: אינו מריץ חלוקה, אינו משנה נתונים ואינו שומר דבר.</para>
/// <para>הוא מאחד את שלושת המקורות הדרושים להסבר: החלוקה השמורה (JSON של הקבוצות),
/// המשתתפים והאילוצים (דרך <see cref="AssignmentPlacementLoader"/>), ומעביר אותם
/// ל-<see cref="IAssignmentExplanationService"/>.</para>
/// </remarks>
// טוען נתונים ומפעיל שירות הסבר — בלי לשנות כלום
public sealed class AssignmentExplanationLoader
{
    // חיבור למסד לשמות תצוגה
    private readonly ApplicationDbContext _db;
    // טוען קלט ובודק הרשאות
    private readonly AssignmentPlacementLoader _placementLoader;
    // שירות BL שמחשב את ההסבר
    private readonly IAssignmentExplanationService _explanationService;

    // בונה את השירות עם שלוש תלויות
    public AssignmentExplanationLoader(
        ApplicationDbContext db,
        AssignmentPlacementLoader placementLoader,
        IAssignmentExplanationService explanationService)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _placementLoader = placementLoader ?? throw new ArgumentNullException(nameof(placementLoader));
        _explanationService = explanationService ?? throw new ArgumentNullException(nameof(explanationService));
    }

    /// <summary>
    /// מחזיר הסבר שיבוץ עבור משתתף, או null אם החלוקה אינה שייכת למנהל/לא קיימת.
    /// </summary>
    /// <exception cref="ArgumentException">מזהה משתתף לא תקין או שאינו חלק מהחלוקה.</exception>
    /// <exception cref="InvalidOperationException">לחלוקה אין תוצאת שיבוץ מאומתת להסביר.</exception>
    // מחזיר הסבר שיבוץ למשתתף או null
    public async Task<AssignmentExplanationDto?> GetExplanationAsync(
        int assignmentId,
        string participantIdentity,
        int managerId,
        CancellationToken cancellationToken = default)
    {
        // בודקים שהחלוקה שייכת למנהל
        var belongsToManager = await _placementLoader.AssignmentBelongsToManagerAsync(
            assignmentId,
            managerId,
            cancellationToken);

        // אין הרשאה — מחזירים null
        if (!belongsToManager)
        {
            return null;
        }

        // טוענים את רשומת החלוקה לקריאה בלבד
        var assignmentEntity = await _db.Assignments
            .AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.AssignmentId == assignmentId, cancellationToken);

        // החלוקה לא קיימת
        if (assignmentEntity is null)
        {
            return null;
        }

        // משחזרים חלוקה מ-JSON — צריך שיבוץ מאומת
        var coreAssignment = PlacementGroupSerialization.DeserializeToAssignment(
            assignmentEntity.LastPlacementGroupsJson);

        // אין שיבוץ לשחזור — לא ניתן להסביר
        if (coreAssignment is null)
        {
            throw new InvalidOperationException(
                "אין תוצאת שיבוץ מאומתת עבור חלוקה זו. יש להריץ אימות לפני בקשת הסבר.");
        }

        // מזהה המשתתף שאנחנו מסבירים
        ParticipantId participantId;
        // מנסים להמיר מחרוזת למזהה משתתף
        try
        {
            participantId = new ParticipantId(participantIdentity);
        }
        catch (ArgumentException)
        {
            throw new ArgumentException("מזהה משתתף לא תקין.");
        }

        // טוענים משתתפים ואילוצים כמו באלגוריתם
        var input = await _placementLoader.LoadInputAsync(assignmentId, managerId, cancellationToken);

        // מחשבים את ההסבר ב-BL
        var explanation = _explanationService.ExplainParticipantPlacement(
            coreAssignment,
            participantId,
            input.Participants,
            input.Constraints);

        // ממירים ל-DTO
        var dto = AssignmentExplanationDto.FromExplanation(explanation);
        // טוענים שמות תצוגה מהמסד
        var displayNames = await LoadDisplayNamesByIdentityAsync(assignmentId, cancellationToken);
        // מוסיפים שמות ל-DTO
        dto.EnrichWithDisplayNames(displayNames);

        return dto;
    }

    // טוען מילון תעודת זהות לשם תצוגה
    private async Task<IReadOnlyDictionary<string, string?>> LoadDisplayNamesByIdentityAsync(
        int assignmentId,
        CancellationToken cancellationToken)
    {
        // מחברים שיוכים עם משתתפים לפי מזהה חלוקה
        return await (
            from participantAssignment in _db.ParticipantAssignments.AsNoTracking()
            where participantAssignment.AssignmentId == assignmentId
            join participant in _db.Participants.AsNoTracking()
                on participantAssignment.ParticipantId equals participant.ParticipantId
            select new
            {
                participant.IsraeliIdentityNumber,
                participant.ParticipantName,
            })
            .ToDictionaryAsync(
                entry => entry.IsraeliIdentityNumber,
                entry => entry.ParticipantName,
                StringComparer.Ordinal,
                cancellationToken);
    }
}