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
/// <remarks>
/// המחלקה מיועדת לבניית מודל תצוגה מלא למסך פרטי החלוקה. היא אינה מפעילה
/// אלגוריתם ואינה משנה נתונים, אלא אוספת נתונים ממספר טבלאות ומחזירה DTO
/// אחד שמכיל את כל מה שהלקוח צריך: משתתפים, אילוצים, הגדרות, סטטוס, קבוצות
/// אחרונות וציוני חלוקה. בכך היא חוסכת מהלקוח לבצע מספר רב של קריאות API.
///
/// ההפרדה בין AssignmentDetailLoader לבין AssignmentPlacementLoader חשובה:
/// הראשון בונה מידע לתצוגה, והשני בונה קלט לאלגוריתם. למרות ששניהם קוראים
/// מהמסד, מבנה הפלט שלהם שונה לחלוטין ולכן האחריות שלהם מופרדת.
/// </remarks>
// מחלקה sealed — טוענת פירוט חלוקה ל-DTO אחד
public sealed class AssignmentDetailLoader
{
    // שדה — הקשר מסד נתונים
    private readonly ApplicationDbContext _db;
    // שדה — טוען לבדיקת בעלות חלוקה
    private readonly AssignmentPlacementLoader _assignmentLoader;
    // שדה — טוען משתתפים וסיווגים
    private readonly AssignmentParticipantsLoader _participantsLoader;

    /// <summary>
    /// מאתחל עם DbContext וטועני עזר.
    /// </summary>
    // בנאי — הזרקת שלוש תלויות
    public AssignmentDetailLoader(
        // DbContext לאפליקציה
        ApplicationDbContext db,
        // טוען בעלות וקלט אלגוריתם
        AssignmentPlacementLoader assignmentLoader,
        // טוען רשימת משתתפים
        AssignmentParticipantsLoader participantsLoader)
    {
        // שמירת DbContext; null זורק חריגה
        _db = db ?? throw new ArgumentNullException(nameof(db));
        // שמירת טוען הבעלות; null זורק חריגה
        _assignmentLoader = assignmentLoader ?? throw new ArgumentNullException(nameof(assignmentLoader));
        // שמירת טוען המשתתפים; null זורק חריגה
        _participantsLoader = participantsLoader ?? throw new ArgumentNullException(nameof(participantsLoader));
    }

    /// <summary>
    /// מחזיר DTO מלא של החלוקה, או null אם לא שייכת למנהל.
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="managerId">מזהה המנהל — בדיקת הרשאה.</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <returns>AssignmentDetailDto מלא; null אם אין גישה או שהרשומה לא קיימת.</returns>
    /// <remarks>
    /// המתודה מתחילה בבדיקת הרשאה, לאחר מכן טוענת את רשומת החלוקה, משתתפים,
    /// הגדרות קבוצות, אילוצי זוגות, אילוצי סיווג ותוצאות הרצה אחרונות.
    /// תוצאות ההרצה האחרונות נשמרות במסד בחלקן כ-JSON, ולכן המתודה מפרקת
    /// אותן לרשימות DTO שנוחות להצגה ב-React.
    /// </remarks>
    // מתודה ציבורית אסינכרונית — פירוט מלא של חלוקה
    public async Task<AssignmentDetailDto?> GetDetailAsync(
        // מזהה החלוקה ב-DB
        int assignmentId,
        // מזהה המנהל לבדיקת הרשאה
        int managerId,
        // טוקן ביטול
        CancellationToken cancellationToken = default)
    {
        // שלב 1 — בדיקת הרשאה: האם החלוקה שייכת למנהל
        var belongsToManager = await _assignmentLoader.AssignmentBelongsToManagerAsync(
            // מזהה חלוקה
            assignmentId,
            // מזהה מנהל
            managerId,
            // טוקן ביטול
            cancellationToken);

        // תנאי — אין הרשאה
        if (!belongsToManager)
        {
            // null — הבקר יחזיר 404
            return null;
        }

        // שלב 2 — טעינת כותרת החלוקה מטבלת Assignments
        var assignment = await _db.Assignments
            // AsNoTracking — קריאה בלבד
            .AsNoTracking()
            // שורה ראשונה לפי מזהה או null
            .FirstOrDefaultAsync(entry => entry.AssignmentId == assignmentId, cancellationToken);

        // תנאי — רשומת חלוקה לא קיימת
        if (assignment is null)
        {
            // יציאה עם null
            return null;
        }

        // שלב 3 — טעינת משתתפים, סיווגים וסיכומים דרך טוען ייעודי
        var participantsDto = await _participantsLoader.GetParticipantsAsync(
            // מזהה חלוקה
            assignmentId,
            // מזהה מנהל (בדיקה כפולה בתוך הטוען)
            managerId,
            // טוקן ביטול
            cancellationToken);

        // שלב 4 — אילוץ מספר קבוצות (שורה אחת לחלוקה)
        var groupCount = await _db.GroupCountConstraints
            // קריאה בלבד
            .AsNoTracking()
            // FirstOrDefault לפי AssignmentId
            .FirstOrDefaultAsync(entry => entry.AssignmentId == assignmentId, cancellationToken);

        // שלב 4 — אילוצי גודל קבוצה (שורה לכל קבוצה)
        var groupSizes = await _db.GroupSizeConstraints
            // ללא מעקב
            .AsNoTracking()
            // סינון לחלוקה הנוכחית
            .Where(entry => entry.AssignmentId == assignmentId)
            // מיון לפי GroupId לסדר יציב
            .OrderBy(entry => entry.GroupId)
            // רשימה בזיכרון
            .ToListAsync(cancellationToken);

        // בניית DTO הגדרות מטבלאות GroupCount ו-GroupSize
        var settings = BuildSettings(groupCount, groupSizes);

        // שלב 5 — טעינת שיוכי משתתפים לבניית lookup ת.ז.
        var participantAssignments = await _db.ParticipantAssignments
            // קריאה בלבד
            .AsNoTracking()
            // כל השיוכים לחלוקה
            .Where(entry => entry.AssignmentId == assignmentId)
            // רשימה
            .ToListAsync(cancellationToken);

        // בניית מילון ParticipantAssignmentId → תעודת זהות
        var identityByAssignmentId = await BuildIdentityLookupAsync(
            // רשימת השיוכים
            participantAssignments,
            // טוקן ביטול
            cancellationToken);

        // שלב 6 — טעינת זוגות חובה עם תרגום לת.ז.
        var mandatoryPairs = await LoadMandatoryPairsAsync(
            // מזהה חלוקה
            assignmentId,
            // מילון lookup
            identityByAssignmentId,
            // טוקן ביטול
            cancellationToken);

        // שלב 6 — טעינת זוגות איסור
        var forbiddenPairs = await LoadForbiddenPairsAsync(
            // מזהה חלוקה
            assignmentId,
            // מילון lookup
            identityByAssignmentId,
            // טוקן ביטול
            cancellationToken);

        // שלב 7 — אילוצי סיווג (איזון/הפרדה)
        var classificationConstraints = await LoadClassificationConstraintsAsync(
            // מזהה חלוקה
            assignmentId,
            // טוקן ביטול
            cancellationToken);

        // שלב 8 — מימדים זמינים ל-UI מתוך סיווגי המשתתפים
        var availableDimensions = participantsDto?.Participants
            // פיצול כל מפתחות הסיווג מכל משתתף
            .SelectMany(entry => entry.Classifications.Keys)
            // מימדים ייחודיים
            .Distinct(StringComparer.Ordinal)
            // מיון אלפביתי
            .OrderBy(entry => entry, StringComparer.Ordinal)
            // רשימה; אם participantsDto null — רשימה ריקה
            .ToList() ?? new List<string>();

        // שלב 9 — הרכבת DTO הפירוט המלא
        return new AssignmentDetailDto
        {
            // מזהה החלוקה
            AssignmentId = assignment.AssignmentId,
            // שם החלוקה
            AssignmentName = assignment.AssignmentName,
            // סטטוס אימות (ValidationStatus)
            Status = assignment.ValidationStatus,
            // מספר משתתפים; 0 אם participantsDto null
            ParticipantCount = participantsDto?.Participants.Count ?? 0,
            // סטטוס שיבוץ אחרון
            LastPlacementStatus = assignment.LastPlacementStatus,
            // פענוח JSON שגיאות אימות לרשימת מחרוזות
            ValidationErrors = ParseValidationErrors(assignment.LastValidationErrors),
            // פענוח JSON קבוצות שיבוץ אחרונות
            PlacementGroups = ParsePlacementGroups(assignment.LastPlacementGroupsJson),
            // ציון שיבוץ אחרון
            PlacementScore = assignment.LastPlacementScore,
            // ציון שיבוץ ראשוני אחרון
            InitialPlacementScore = assignment.LastInitialPlacementScore,
            // הגדרות קבוצות (מינימום/מקסימום)
            Settings = settings,
            // רשימת משתתפים; ריקה אם null
            Participants = participantsDto?.Participants.ToList() ?? new List<ParticipantListItemDto>(),
            // זוגות חובה
            MandatoryPairs = mandatoryPairs,
            // זוגות איסור
            ForbiddenPairs = forbiddenPairs,
            // אילוצי סיווג
            ClassificationConstraints = classificationConstraints,
            // מימדים זמינים לבחירה ב-UI
            AvailableDimensions = availableDimensions,
        };
    }

    /// <summary>
    /// בונה DTO הגדרות מטבלאות GroupCount + GroupSize.
    /// </summary>
    /// <remarks>
    /// כל הקבוצות חולקות אותו Min/Max גודל — לוקחים את השורה הראשונה.
    /// </remarks>
    // מתודה פרטית סטטית — המרת אילוצי קבוצות ל-DTO
    private static AssignmentSettingsDto BuildSettings(
        // אילוץ מספר קבוצות (אופציונלי)
        GroupCountConstraint? groupCount,
        // רשימת אילוצי גודל לפי קבוצה
        IReadOnlyList<GroupSizeConstraint> groupSizes)
    {
        // תנאי — אין אילוץ מספר קבוצות
        if (groupCount is null)
        {
            // DTO ריק — ברירת מחדל ב-UI
            return new AssignmentSettingsDto();
        }

        // FirstOrDefault — גודל קבוצה מהשורה הראשונה (כולם זהים)
        var firstSize = groupSizes.FirstOrDefault();

        // החזרת DTO עם ערכי מינימום ומקסימום
        return new AssignmentSettingsDto
        {
            // מינימום קבוצות
            MinGroups = groupCount.MinGroups,
            // מקסימום קבוצות
            MaxGroups = groupCount.MaxGroups,
            // מינימום גודל קבוצה; 0 אם אין שורת גודל
            MinGroupSize = firstSize?.MinGroupSize ?? 0,
            // מקסימום גודל קבוצה; 0 אם אין
            MaxGroupSize = firstSize?.MaxGroupSize ?? 0,
        };
    }

    /// <summary>
    /// ממפה ParticipantAssignmentId → תעודת זהות ישראלית.
    /// </summary>
    // מתודה פרטית אסינכרונית — בניית lookup ת.ז.
    private async Task<Dictionary<int, string>> BuildIdentityLookupAsync(
        // רשימת שיוכי משתתפים לחלוקה
        IReadOnlyList<ParticipantAssignment> participantAssignments,
        // טוקן ביטול
        CancellationToken cancellationToken)
    {
        // תנאי — אין שיוכים
        if (participantAssignments.Count == 0)
        {
            // מילון ריק
            return new Dictionary<int, string>();
        }

        // LINQ — מזהי Participant ייחודיים ב-DB
        var participantIds = participantAssignments
            // בחירת ParticipantId
            .Select(entry => entry.ParticipantId)
            // הסרת כפילויות
            .Distinct()
            // רשימה ל-Contains
            .ToList();

        // שאילתה — טעינת משתתפים לפי מזהי DB
        var participants = await _db.Participants
            // קריאה בלבד
            .AsNoTracking()
            // סינון למזהים הרלוונטיים
            .Where(entry => participantIds.Contains(entry.ParticipantId))
            // מילון ParticipantId → Participant
            .ToDictionaryAsync(entry => entry.ParticipantId, cancellationToken);

        // המרת רשימת שיוכים למילון ParticipantAssignmentId → ת.ז.
        return participantAssignments.ToDictionary(
            // מפתח — מזהה שיוך
            entry => entry.ParticipantAssignmentId,
            // ערך — ת.ז. או מחרוזת ריקה אם משתתף חסר
            entry => participants.TryGetValue(entry.ParticipantId, out var participant)
                ? participant.IsraeliIdentityNumber
                : string.Empty);
    }

    /// <summary>
    /// טוען זוגות חובה ומתרגם ל-DTO עם ת.ז.
    /// </summary>
    // מתודה פרטית — טעינת MandatoryPairConstraints
    private async Task<List<PairConstraintItemDto>> LoadMandatoryPairsAsync(
        // מזהה חלוקה
        int assignmentId,
        // מילון ת.ז. לפי מזהה שיוך
        IReadOnlyDictionary<int, string> identityByAssignmentId,
        // טוקן ביטול
        CancellationToken cancellationToken)
    {
        // שאילתה — כל זוגות החובה של החלוקה
        var rows = await _db.MandatoryPairConstraints
            // ללא מעקב
            .AsNoTracking()
            // סינון לפי חלוקה
            .Where(entry => entry.AssignmentId == assignmentId)
            // רשימה
            .ToListAsync(cancellationToken);

        // LINQ — המרה, סינון ומיון
        return rows
            // MapPair — תרגום מזהי שיוך לת.ז. עם סדר קבוע
            .Select(row => MapPair(row.MandatoryPairConstraintId, row.FirstParticipantAssignmentId, row.SecondParticipantAssignmentId, identityByAssignmentId))
            // סינון זוגות עם צד חסר במיפוי
            .Where(entry => !string.IsNullOrEmpty(entry.ParticipantA) && !string.IsNullOrEmpty(entry.ParticipantB))
            // מיון לפי ת.ז. A
            .OrderBy(entry => entry.ParticipantA, StringComparer.Ordinal)
            // מיון משני לפי ת.ז. B
            .ThenBy(entry => entry.ParticipantB, StringComparer.Ordinal)
            // רשימה סופית
            .ToList();
    }

    /// <summary>
    /// טוען זוגות איסור — אותה לוגיקה כמו Mandatory.
    /// </summary>
    // מתודה פרטית — טעינת ForbiddenPairConstraints
    private async Task<List<PairConstraintItemDto>> LoadForbiddenPairsAsync(
        // מזהה חלוקה
        int assignmentId,
        // מילון lookup ת.ז.
        IReadOnlyDictionary<int, string> identityByAssignmentId,
        // טוקן ביטול
        CancellationToken cancellationToken)
    {
        // שאילתה — זוגות איסור
        var rows = await _db.ForbiddenPairConstraints
            // קריאה בלבד
            .AsNoTracking()
            // לפי חלוקה
            .Where(entry => entry.AssignmentId == assignmentId)
            // רשימה
            .ToListAsync(cancellationToken);

        // אותה שרשרת המרה כמו בזוגות חובה
        return rows
            // MapPair עם מזהה אילוץ איסור
            .Select(row => MapPair(row.ForbiddenPairConstraintId, row.FirstParticipantAssignmentId, row.SecondParticipantAssignmentId, identityByAssignmentId))
            // דילוג על זוגות לא שלמים
            .Where(entry => !string.IsNullOrEmpty(entry.ParticipantA) && !string.IsNullOrEmpty(entry.ParticipantB))
            // מיון A
            .OrderBy(entry => entry.ParticipantA, StringComparer.Ordinal)
            // מיון B
            .ThenBy(entry => entry.ParticipantB, StringComparer.Ordinal)
            // רשימה
            .ToList();
    }

    /// <summary>
    /// ממיר שורת DB ל-DTO זוג — עם סדר אלפביתי קבוע (A ≤ B).
    /// </summary>
    // מתודה סטטית — מיפוי שורת זוג ל-DTO
    private static PairConstraintItemDto MapPair(
        // מזהה האילוץ ב-DB
        int constraintId,
        // מזהה שיוך משתתף ראשון
        int firstParticipantAssignmentId,
        // מזהה שיוך משתתף שני
        int secondParticipantAssignmentId,
        // מילון ת.ז.
        IReadOnlyDictionary<int, string> identityByAssignmentId)
    {
        // TryGetValue — ת.ז. של צד A; ריק אם חסר
        identityByAssignmentId.TryGetValue(firstParticipantAssignmentId, out var participantA);
        // TryGetValue — ת.ז. של צד B
        identityByAssignmentId.TryGetValue(secondParticipantAssignmentId, out var participantB);

        // OrderPair — סידור אלפביתי קבוע למניעת (A,B)/(B,A)
        var ordered = OrderPair(participantA ?? string.Empty, participantB ?? string.Empty);

        // החזרת DTO זוג
        return new PairConstraintItemDto
        {
            // מזהה האילוץ
            ConstraintId = constraintId,
            // ת.ז. קטנה יותר (או שווה) — צד A
            ParticipantA = ordered.A,
            // ת.ז. גדולה יותר — צד B
            ParticipantB = ordered.B,
        };
    }

    /// <summary>
    /// טוען אילוצי סיווג (איזון/הפרדה) עם קוד מימד.
    /// </summary>
    // מתודה פרטית — טעינת AssignmentClassificationConstraints
    private async Task<List<ClassificationConstraintItemDto>> LoadClassificationConstraintsAsync(
        // מזהה חלוקה
        int assignmentId,
        // טוקן ביטול
        CancellationToken cancellationToken)
    {
        // שאילתה — שורות אילוץ סיווג
        var rows = await _db.AssignmentClassificationConstraints
            // ללא מעקב
            .AsNoTracking()
            // לפי חלוקה
            .Where(entry => entry.AssignmentId == assignmentId)
            // רשימה
            .ToListAsync(cancellationToken);

        // תנאי — אין אילוצי סיווג
        if (rows.Count == 0)
        {
            // רשימה ריקה
            return new List<ClassificationConstraintItemDto>();
        }

        // LINQ — מזהי מימד ייחודיים מהשורות
        var dimensionIds = rows
            // בחירת ClassificationDimensionId
            .Select(entry => entry.ClassificationDimensionId)
            // ייחוד
            .Distinct()
            // רשימה
            .ToList();

        // שאילתה — טעינת מימדים לפי מזהים
        var dimensions = await _db.ClassificationDimensions
            // קריאה בלבד
            .AsNoTracking()
            // סינון למימדים הרלוונטיים
            .Where(entry => dimensionIds.Contains(entry.ClassificationDimensionId))
            // מילון מזהה → מימד
            .ToDictionaryAsync(entry => entry.ClassificationDimensionId, cancellationToken);

        // LINQ — המרת שורות ל-DTO
        return rows
            // Select עם בלוק — בניית DTO לכל שורה
            .Select(row =>
            {
                // חיפוש מימד; dimension עשוי להיות null
                dimensions.TryGetValue(row.ClassificationDimensionId, out var dimension);
                // יצירת DTO אילוץ סיווג
                return new ClassificationConstraintItemDto
                {
                    // קוד מימד; ריק אם מימד חסר
                    DimensionCode = dimension?.DimensionCode ?? string.Empty,
                    // IsBalanceOrSeparation — true=Balance, false=Separation
                    RuleType = row.IsBalanceOrSeparation ? "Balance" : "Separation",
                };
            })
            // סינון שורות ללא קוד מימד תקף
            .Where(entry => !string.IsNullOrEmpty(entry.DimensionCode))
            // מיון לפי קוד מימד
            .OrderBy(entry => entry.DimensionCode, StringComparer.Ordinal)
            // רשימה
            .ToList();
    }

    /// <summary>
    /// מסדר זוג ת.ז. בסדר אלפביתי — לייצוג עקבי.
    /// </summary>
    // מתודה פנימית סטטית — ביטוי גוף (expression-bodied)
    internal static (string A, string B) OrderPair(string participantA, string participantB) =>
        // CompareOrdinal — השוואה לפי סדר יוניקוד
        string.CompareOrdinal(participantA, participantB) <= 0
            // אם A קטן או שווה — סדר מקורי
            ? (participantA, participantB)
            // אחרת — החלפת סדר
            : (participantB, participantA);

    /// <summary>
    /// מפענח JSON של שגיאות אימות — עם fallback למחרוזת גולמית.
    /// </summary>
    // מתודה סטטית — פענוח LastValidationErrors
    private static List<string> ParseValidationErrors(string? raw)
    {
        // תנאי — null, ריק או רווחים בלבד
        if (string.IsNullOrWhiteSpace(raw))
        {
            // רשימה ריקה
            return new List<string>();
        }

        // try — ניסיון Deserialize לרשימת מחרוזות
        try
        {
            // JsonSerializer.Deserialize; ?? רשימה ריקה אם null
            return JsonSerializer.Deserialize<List<string>>(raw) ?? new List<string>();
        }
        // catch — JSON לא תקין
        catch (JsonException)
        {
            // fallback — מחרוזת גולמית כפריט יחיד
            return new List<string> { raw };
        }
    }

    /// <summary>
    /// מפענח JSON של קבוצות שיבוץ אחרונות.
    /// </summary>
    // מתודה סטטית — פענוח LastPlacementGroupsJson
    private static List<PlacementGroupDto> ParsePlacementGroups(string? raw)
    {
        // תנאי — אין תוכן
        if (string.IsNullOrWhiteSpace(raw))
        {
            // רשימה ריקה
            return new List<PlacementGroupDto>();
        }

        // try — Deserialize לרשימת PlacementGroupDto
        try
        {
            // פענוח JSON; ?? רשימה ריקה
            return JsonSerializer.Deserialize<List<PlacementGroupDto>>(raw) ?? new List<PlacementGroupDto>();
        }
        // catch — JSON פגום
        catch (JsonException)
        {
            // לא שוברים את המסך — רשימה ריקה
            return new List<PlacementGroupDto>();
        }
    }
}