using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyProject.Data;
using MyProject.Data.Models;

namespace MyProject.API.Placement;

/// <summary>
/// טוען תצוגת פירוט מלאה של חלוקה — משתתפים, אילוצים, הגדרות ותוצאת שיבוץ אחרונה.
/// </summary>
/// <remarks>
/// נקרא מ-: <see cref="AssignmentsController.GetDetail"/>, <see cref="AssignmentEditorService"/> (אחרי כל עריכה).
/// </remarks>
public sealed class AssignmentDetailLoader
{
    private readonly ApplicationDbContext _db;
    private readonly AssignmentPlacementLoader _assignmentLoader;
    private readonly AssignmentParticipantsLoader _participantsLoader;

    /// <summary>
    /// מאתחל עם DbContext וטועני עזר.
    /// </summary>
    public AssignmentDetailLoader(
        ApplicationDbContext db,
        AssignmentPlacementLoader assignmentLoader,
        AssignmentParticipantsLoader participantsLoader)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _assignmentLoader = assignmentLoader ?? throw new ArgumentNullException(nameof(assignmentLoader));
        _participantsLoader = participantsLoader ?? throw new ArgumentNullException(nameof(participantsLoader));
    }

    /// <summary>
    /// מחזיר DTO מלא של החלוקה, או null אם לא שייכת למנהל.
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="managerId">מזהה המנהל — בדיקת הרשאה.</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <returns>AssignmentDetailDto מלא; null אם אין גישה או שהרשומה לא קיימת.</returns>
    public async Task<AssignmentDetailDto?> GetDetailAsync(
        int assignmentId,
        int managerId,
        CancellationToken cancellationToken = default)
    {
        // ===== שלב 1: הרשאה =====
        // AssignmentBelongsToManagerAsync — JOIN Assignment↔Manager; false = 404 ללקוח.
        var belongsToManager = await _assignmentLoader.AssignmentBelongsToManagerAsync(
            assignmentId,
            managerId,
            cancellationToken);

        if (!belongsToManager)
        {
            return null;
        }

        // ===== שלב 2: כותרת החלוקה =====
        // AsNoTracking — קריאה בלבד, בלי change tracker של EF.
        var assignment = await _db.Assignments
            .AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.AssignmentId == assignmentId, cancellationToken);

        if (assignment is null)
        {
            return null;
        }

        // ===== שלב 3: משתתפים + סיווגים =====
        var participantsDto = await _participantsLoader.GetParticipantsAsync(
            assignmentId,
            managerId,
            cancellationToken);

        // ===== שלב 4: הגדרות קבוצות (מספר + גודל) =====
        var groupCount = await _db.GroupCountConstraints
            .AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.AssignmentId == assignmentId, cancellationToken);

        var groupSizes = await _db.GroupSizeConstraints
            .AsNoTracking()
            .Where(entry => entry.AssignmentId == assignmentId)
            .OrderBy(entry => entry.GroupId)
            .ToListAsync(cancellationToken);

        var settings = BuildSettings(groupCount, groupSizes);

        // ===== שלב 5: מיפוי ת.ז. לזוגות אילוץ =====
        // ParticipantAssignmentId (מפתח DB) ≠ ParticipantId (ת.ז.) — צריך lookup לפני MapPair.
        var participantAssignments = await _db.ParticipantAssignments
            .AsNoTracking()
            .Where(entry => entry.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        var identityByAssignmentId = await BuildIdentityLookupAsync(
            participantAssignments,
            cancellationToken);

        // ===== שלב 6: אילוצי זוגות =====
        var mandatoryPairs = await LoadMandatoryPairsAsync(
            assignmentId,
            identityByAssignmentId,
            cancellationToken);

        var forbiddenPairs = await LoadForbiddenPairsAsync(
            assignmentId,
            identityByAssignmentId,
            cancellationToken);

        // ===== שלב 7: אילוצי סיווג =====
        var classificationConstraints = await LoadClassificationConstraintsAsync(
            assignmentId,
            cancellationToken);

        // ===== שלב 8: מימדים זמינים (ל-UI) =====
        // Distinct על כל מפתחות הסיווג של כל המשתתפים — מיון אלפביתי יציב.
        var availableDimensions = participantsDto?.Participants
            .SelectMany(entry => entry.Classifications.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(entry => entry, StringComparer.Ordinal)
            .ToList() ?? new List<string>();

        // ===== שלב 9: הרכבת DTO =====
        return new AssignmentDetailDto
        {
            AssignmentId = assignment.AssignmentId,
            AssignmentName = assignment.AssignmentName,
            Status = assignment.ValidationStatus,
            ParticipantCount = participantsDto?.Participants.Count ?? 0,
            LastPlacementStatus = assignment.LastPlacementStatus,
            // JSON שנשמר ב-DB — מפורקים לרשימות לתצוגה.
            ValidationErrors = ParseValidationErrors(assignment.LastValidationErrors),
            PlacementGroups = ParsePlacementGroups(assignment.LastPlacementGroupsJson),
            Settings = settings,
            Participants = participantsDto?.Participants.ToList() ?? new List<ParticipantListItemDto>(),
            MandatoryPairs = mandatoryPairs,
            ForbiddenPairs = forbiddenPairs,
            ClassificationConstraints = classificationConstraints,
            AvailableDimensions = availableDimensions,
        };
    }

    /// <summary>
    /// בונה DTO הגדרות מטבלאות GroupCount + GroupSize.
    /// </summary>
    /// <remarks>
    /// כל הקבוצות חולקות אותו Min/Max גודל — לוקחים את השורה הראשונה.
    /// </remarks>
    private static AssignmentSettingsDto BuildSettings(
        GroupCountConstraint? groupCount,
        IReadOnlyList<GroupSizeConstraint> groupSizes)
    {
        if (groupCount is null)
        {
            // אין אילוץ מספר קבוצות — DTO ריק (ברירת מחדל ב-UI).
            return new AssignmentSettingsDto();
        }

        var firstSize = groupSizes.FirstOrDefault();

        return new AssignmentSettingsDto
        {
            MinGroups = groupCount.MinGroups,
            MaxGroups = groupCount.MaxGroups,
            MinGroupSize = firstSize?.MinGroupSize ?? 0,
            MaxGroupSize = firstSize?.MaxGroupSize ?? 0,
        };
    }

    /// <summary>
    /// ממפה ParticipantAssignmentId → תעודת זהות ישראלית.
    /// </summary>
    private async Task<Dictionary<int, string>> BuildIdentityLookupAsync(
        IReadOnlyList<ParticipantAssignment> participantAssignments,
        CancellationToken cancellationToken)
    {
        if (participantAssignments.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        // Distinct — מונע IN clause כפול ב-SQL.
        var participantIds = participantAssignments
            .Select(entry => entry.ParticipantId)
            .Distinct()
            .ToList();

        var participants = await _db.Participants
            .AsNoTracking()
            .Where(entry => participantIds.Contains(entry.ParticipantId))
            .ToDictionaryAsync(entry => entry.ParticipantId, cancellationToken);

        // מפתח = ParticipantAssignmentId (שורת שיוך לחלוקה), לא ParticipantId.
        return participantAssignments.ToDictionary(
            entry => entry.ParticipantAssignmentId,
            entry => participants.TryGetValue(entry.ParticipantId, out var participant)
                ? participant.IsraeliIdentityNumber
                : string.Empty);
    }

    /// <summary>
    /// טוען זוגות חובה ומתרגם ל-DTO עם ת.ז.
    /// </summary>
    private async Task<List<PairConstraintItemDto>> LoadMandatoryPairsAsync(
        int assignmentId,
        IReadOnlyDictionary<int, string> identityByAssignmentId,
        CancellationToken cancellationToken)
    {
        var rows = await _db.MandatoryPairConstraints
            .AsNoTracking()
            .Where(entry => entry.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => MapPair(row.MandatoryPairConstraintId, row.FirstParticipantAssignmentId, row.SecondParticipantAssignmentId, identityByAssignmentId))
            // מסננים זוגות שאחד הצדדים נמחק/חסר במיפוי.
            .Where(entry => !string.IsNullOrEmpty(entry.ParticipantA) && !string.IsNullOrEmpty(entry.ParticipantB))
            .OrderBy(entry => entry.ParticipantA, StringComparer.Ordinal)
            .ThenBy(entry => entry.ParticipantB, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// טוען זוגות איסור — אותה לוגיקה כמו Mandatory.
    /// </summary>
    private async Task<List<PairConstraintItemDto>> LoadForbiddenPairsAsync(
        int assignmentId,
        IReadOnlyDictionary<int, string> identityByAssignmentId,
        CancellationToken cancellationToken)
    {
        var rows = await _db.ForbiddenPairConstraints
            .AsNoTracking()
            .Where(entry => entry.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => MapPair(row.ForbiddenPairConstraintId, row.FirstParticipantAssignmentId, row.SecondParticipantAssignmentId, identityByAssignmentId))
            .Where(entry => !string.IsNullOrEmpty(entry.ParticipantA) && !string.IsNullOrEmpty(entry.ParticipantB))
            .OrderBy(entry => entry.ParticipantA, StringComparer.Ordinal)
            .ThenBy(entry => entry.ParticipantB, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// ממיר שורת DB ל-DTO זוג — עם סדר אלפביתי קבוע (A ≤ B).
    /// </summary>
    private static PairConstraintItemDto MapPair(
        int constraintId,
        int firstParticipantAssignmentId,
        int secondParticipantAssignmentId,
        IReadOnlyDictionary<int, string> identityByAssignmentId)
    {
        identityByAssignmentId.TryGetValue(firstParticipantAssignmentId, out var participantA);
        identityByAssignmentId.TryGetValue(secondParticipantAssignmentId, out var participantB);

        // OrderPair — מונע כפילות (A,B) vs (B,A) בתצוגה ובחיפוש.
        var ordered = OrderPair(participantA ?? string.Empty, participantB ?? string.Empty);

        return new PairConstraintItemDto
        {
            ConstraintId = constraintId,
            ParticipantA = ordered.A,
            ParticipantB = ordered.B,
        };
    }

    /// <summary>
    /// טוען אילוצי סיווג (איזון/הפרדה) עם קוד מימד.
    /// </summary>
    private async Task<List<ClassificationConstraintItemDto>> LoadClassificationConstraintsAsync(
        int assignmentId,
        CancellationToken cancellationToken)
    {
        var rows = await _db.AssignmentClassificationConstraints
            .AsNoTracking()
            .Where(entry => entry.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return new List<ClassificationConstraintItemDto>();
        }

        var dimensionIds = rows
            .Select(entry => entry.ClassificationDimensionId)
            .Distinct()
            .ToList();

        var dimensions = await _db.ClassificationDimensions
            .AsNoTracking()
            .Where(entry => dimensionIds.Contains(entry.ClassificationDimensionId))
            .ToDictionaryAsync(entry => entry.ClassificationDimensionId, cancellationToken);

        return rows
            .Select(row =>
            {
                dimensions.TryGetValue(row.ClassificationDimensionId, out var dimension);
                return new ClassificationConstraintItemDto
                {
                    DimensionCode = dimension?.DimensionCode ?? string.Empty,
                    // IsBalanceOrSeparation=true → Balance; false → Separation.
                    RuleType = row.IsBalanceOrSeparation ? "Balance" : "Separation",
                };
            })
            .Where(entry => !string.IsNullOrEmpty(entry.DimensionCode))
            .OrderBy(entry => entry.DimensionCode, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// מסדר זוג ת.ז. בסדר אלפביתי — לייצוג עקבי.
    /// </summary>
    internal static (string A, string B) OrderPair(string participantA, string participantB) =>
        string.CompareOrdinal(participantA, participantB) <= 0
            ? (participantA, participantB)
            : (participantB, participantA);

    /// <summary>
    /// מפענח JSON של שגיאות אימות — עם fallback למחרוזת גולמית.
    /// </summary>
    private static List<string> ParseValidationErrors(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(raw) ?? new List<string>();
        }
        catch (JsonException)
        {
            // גרסה ישנה/פגומה — מציגים את הטקסט כפי שהוא.
            return new List<string> { raw };
        }
    }

    /// <summary>
    /// מפענח JSON של קבוצות שיבוץ אחרונות.
    /// </summary>
    private static List<PlacementGroupDto> ParsePlacementGroups(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<PlacementGroupDto>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<PlacementGroupDto>>(raw) ?? new List<PlacementGroupDto>();
        }
        catch (JsonException)
        {
            // JSON פגום — מחזירים רשימה ריקה (לא שוברים את מסך הפירוט).
            return new List<PlacementGroupDto>();
        }
    }
}
