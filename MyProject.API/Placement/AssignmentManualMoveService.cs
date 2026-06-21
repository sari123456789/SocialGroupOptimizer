using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyProject.BL.ManualMoves;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.ValueObjects;
using MyProject.Data;
using CoreAssignment = MyProject.Core.Domain.Entities.Assignment;
using CoreParticipant = MyProject.Core.Domain.Entities.Participant;
using DataAssignment = MyProject.Data.Models.Assignment;


namespace MyProject.API.Placement;

/// <summary>
/// מנהל מהלכים ידניים (Swap/Transfer) של מנהל על חלוקה קיימת — Preview ו-Apply.
/// </summary>
/// <remarks>
/// <para>Preview הוא Read Only. Apply שומר את התוצאה רק לפי הכללים: שינוי חוקי נשמר רגיל;
/// שינוי שמפר אילוצים עסקיים נשמר רק עם אישור חריגה מפורש; שגיאת תקינות מערכת לעולם
/// אינה נשמרת.</para>
/// <para>ההערכה נעשית בשכבת BL (<see cref="IManualMoveEvaluator"/>); כאן מתבצעות טעינת
/// הנתונים, אכיפת הכללים והשמירה.</para>
/// </remarks>
//  שירות מהלכים ידניים ב-API
public sealed class AssignmentManualMoveService
{
    private readonly ApplicationDbContext _db;
    // שדה — טוען קלט אלגוריתם ובדיקת בעלות
    private readonly AssignmentPlacementLoader _placementLoader;
    // שדה — טוען פירוט חלוקה אחרי Apply
    private readonly AssignmentDetailLoader _detailLoader;
    // שדה — מעריך חוקיות מהלך בשכבת BL
    private readonly IManualMoveEvaluator _evaluator;

    public AssignmentManualMoveService(
        ApplicationDbContext db,
        AssignmentPlacementLoader placementLoader,
        AssignmentDetailLoader detailLoader,
        IManualMoveEvaluator evaluator)
    {
         _db = db ?? throw new ArgumentNullException(nameof(db));
         _placementLoader = placementLoader ?? throw new ArgumentNullException(nameof(placementLoader));
         _detailLoader = detailLoader ?? throw new ArgumentNullException(nameof(detailLoader));
         _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    /// <summary>
    /// תצוגה מקדימה של מהלך ידני. מחזיר null אם החלוקה אינה שייכת למנהל/לא קיימת.
    /// </summary>
    // מתודה ציבורית — Preview ללא שמירה
    public async Task<ManualMovePreviewResult?> PreviewAsync(
        
        int assignmentId,
        int managerId,
         ManualMoveRequestDto request,
         CancellationToken cancellationToken = default)
    {
        // טעינת הקשר: בעלות, חלוקה נוכחית, משתתפים, אילוצים
        var load = await LoadAsync(assignmentId, managerId, cancellationToken);
        // תנאי — חלוקה לא נמצאה או לא שייכת למנהל
        if (!load.Found)
        {
             return null;
        }

        // תנאי — נמצאה אך אין הקשר תקף (למשל אין שיבוץ)
        if (!load.HasContext)
        {
            // תוצאה חוסמת עם הודעת חסימה
            return ManualMovePreviewResult.Blocking(new[] { load.BlockingMessage! });
        }

        // רשימה לאיסוף שגיאות בניית מהלך
        var moveErrors = new List<string>();
        // ניסיון לבנות מהלך דומיין מבקשת הלקוח
        var move = TryBuildMove(request, moveErrors);
        // תנאי — בקשה פסולה
        if (move is null)
        {
            // החזרת תוצאה חוסמת עם שגיאות הקלט
            return ManualMovePreviewResult.Blocking(moveErrors);
        }

        // הערכת המהלך בשכבת BL — ללא שינוי במסד
        var evaluation = _evaluator.Evaluate(
            // חלוקה נוכחית משוחזרת מ-JSON
            load.Current!,
            // מהלך דומיין (Swap/Transfer)
            move,
            // משתתפים מקלט האלגוריתם
            load.Participants!,
            // אילוצים מקלט האלגוריתם
            load.Constraints!);

        // המרת תוצאת הערכה ל-DTO תצוגה מקדימה
        return ManualMovePreviewResult.FromEvaluation(evaluation);
    }

    /// <summary>
    /// ביצוע מהלך ידני בכפוף לכללי החוקיות והחריגה.
    /// </summary>
    // מתודה ציבורית — Apply עם אפשרות שמירה
    public async Task<ManualMoveApplyOutcome> ApplyAsync(
        // מזהה חלוקה
        int assignmentId,
        // מזהה מנהל
        int managerId,
        // DTO בקשה כולל אישור חריגה
        ManualMoveApplyRequestDto request,
        // טוקן ביטול
        CancellationToken cancellationToken = default)
    {
        // טעינת הקשר מלא כמו ב-Preview
        var load = await LoadAsync(assignmentId, managerId, cancellationToken);
        // תנאי — לא נמצא
        if (!load.Found)
        {
            // תוצאת NotFound
            return ManualMoveApplyOutcome.NotFound();
        }

        // תנאי — חסום (אין שיבוץ מאומת וכו')
        if (!load.HasContext)
        {
            // Blocked עם הודעת חסימה
            return ManualMoveApplyOutcome.Blocked(
                ManualMovePreviewResult.Blocking(new[] { load.BlockingMessage! }));
        }

        // רשימת שגיאות לבניית מהלך
        var moveErrors = new List<string>();
        // בניית מהלך מתוך request.Move
        var move = TryBuildMove(request.Move, moveErrors);
        // תנאי — מהלך לא תקין
        if (move is null)
        {
            // Blocked עם שגיאות קלט
            return ManualMoveApplyOutcome.Blocked(ManualMovePreviewResult.Blocking(moveErrors));
        }

        // הערכת המהלך — אותה לוגיקה כמו Preview
        var evaluation = _evaluator.Evaluate(
            // חלוקה נוכחית
            load.Current!,
            // מהלך
            move,
            // משתתפים
            load.Participants!,
            // אילוצים
            load.Constraints!);

        // תנאי — מהלך חוקי לחלוטין
        if (evaluation.IsLegal)
        {
            // שמירה רגילה ללא חריגה
            await SaveAsync(load.Entity!, evaluation, isOverride: false, cancellationToken);
            // החזרת פירוט מעודכן
            return await AppliedAsync(assignmentId, managerId, cancellationToken);
        }

        // תנאי — הפרות עסקיות בלבד; ניתן לאשר חריגה
        if (evaluation.CanOverride)
        {
            // תנאי — המשתמש לא אישר חריגה
            if (!request.OverrideConfirmed)
            {
                // NeedsOverride — הלקוח יציג דיאלוג אישור
                return ManualMoveApplyOutcome.NeedsOverride(
                    ManualMovePreviewResult.FromEvaluation(evaluation));
            }

            // שמירה עם דגל חריגה
            await SaveAsync(load.Entity!, evaluation, isOverride: true, cancellationToken);
            // פירוט מעודכן
            return await AppliedAsync(assignmentId, managerId, cancellationToken);
        }

        // שגיאת תקינות מערכת — לעולם לא נשמר
        return ManualMoveApplyOutcome.Blocked(ManualMovePreviewResult.FromEvaluation(evaluation));
    }

    // מתודה פרטית — טעינת פירוט אחרי Apply מוצלח
    private async Task<ManualMoveApplyOutcome> AppliedAsync(
        // מזהה חלוקה
        int assignmentId,
        // מזהה מנהל
        int managerId,
        // טוקן ביטול
        CancellationToken cancellationToken)
    {
        // טעינת DTO פירוט מלא לאחר השמירה
        var detail = await _detailLoader.GetDetailAsync(assignmentId, managerId, cancellationToken);
        // עטיפה בתוצאת Applied
        return ManualMoveApplyOutcome.Applied(detail);
    }

    // מתודה פרטית — עדכון ישות Assignment במסד ושמירה
    private async Task SaveAsync(
        // ישות EF של החלוקה (עם change tracking)
        DataAssignment entity,
        // תוצאת הערכת המהלך
        ManualMoveEvaluation evaluation,
        // האם נשמר עם חריגה מאושרת
        bool isOverride,
        // טוקן ביטול
        CancellationToken cancellationToken)
    {
        // סריאליזציה של החלוקה המעודכנת ל-JSON
        entity.LastPlacementGroupsJson = PlacementGroupSerialization.SerializeGroups(
            // החלוקה לאחר המהלך
            evaluation.ResultingAssignment!);
        // עדכון ציון לאחר המהלך
        entity.LastPlacementScore = evaluation.ScoreAfter;
        // סטטוס אימות — מאומת
        entity.ValidationStatus = "Validated";
        // סטטוס שיבוץ — הצלחה
        entity.LastPlacementStatus = "Success";
        // חותמת זמן UTC של האימות/שמירה
        entity.LastValidatedAtUtc = DateTime.UtcNow;

        // תנאי — שמירה עם חריגה מאושרת
        if (isOverride)
        {
            // רשימת שורות להודעות הפרה
            var lines = new List<string> { "שינוי ידני נשמר עם חריגה מהאילוצים הבאים:" };
            // הוספת הודעת כל אילוץ שנשבר
            lines.AddRange(evaluation.BrokenConstraints.Select(broken => broken.Message));
            // סריאליזציה ל-JSON בשדה LastValidationErrors
            entity.LastValidationErrors = JsonSerializer.Serialize(lines);
        }
        else
        {
            // מהלך חוקי — ניקוי שגיאות אימות קודמות
            entity.LastValidationErrors = null;
        }

        // שמירת שינויים ל-DB
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// טוען את כל ההקשר הדרוש: בעלות, ישות, חלוקה משוחזרת, משתתפים ואילוצים.
    /// </summary>
    // מתודה פרטית — טעינת LoadResult לפני Preview/Apply
    private async Task<LoadResult> LoadAsync(
        // מזהה חלוקה
        int assignmentId,
        // מזהה מנהל
        int managerId,
        // טוקן ביטול
        CancellationToken cancellationToken)
    {
        // בדיקת שייכות חלוקה למנהל
        var belongsToManager = await _placementLoader.AssignmentBelongsToManagerAsync(
            // מזהה חלוקה
            assignmentId,
            // מזהה מנהל
            managerId,
            // טוקן ביטול
            cancellationToken);

        // תנאי — אין הרשאה
        if (!belongsToManager)
        {
            // NotFound בתוצאת הטעינה
            return LoadResult.NotFound();
        }

        // טעינת ישות Assignment עם change tracking (לשמירה עתידית)
        var entity = await _db.Assignments
            // FirstOrDefault לפי מזהה
            .FirstOrDefaultAsync(item => item.AssignmentId == assignmentId, cancellationToken);

        // תנאי — רשומה לא קיימת
        if (entity is null)
        {
            // NotFound
            return LoadResult.NotFound();
        }

        // שחזור חלוקת דומיין מ-JSON האחרון
        var current = PlacementGroupSerialization.DeserializeToAssignment(entity.LastPlacementGroupsJson);
        // תנאי — אין תוצאת שיבוץ לשחזור
        if (current is null)
        {
            // Blocked עם הודעה למשתמש
            return LoadResult.Blocked(
                // ישות לשמירה עתידית אם נדרש
                entity,
                // הודעת חסימה בעברית
                "אין תוצאת שיבוץ מאומתת עבור חלוקה זו. יש להריץ אימות לפני מהלך ידני.");
        }

        // try — טעינת קלט אלגוריתם (משתתפים + אילוצים)
        try
        {
            // LoadInputAsync — בונה קלט מלא מהמסד
            var input = await _placementLoader.LoadInputAsync(assignmentId, managerId, cancellationToken);
            // Ready — כל ההקשר זמין
            return LoadResult.Ready(entity, current, input.Participants, input.Constraints);
        }
        // catch — קלט לא תקין (ArgumentException)
        catch (ArgumentException ex)
        {
            // Blocked עם הודעת החריגה
            return LoadResult.Blocked(entity, ex.Message);
        }
        // catch — מצב לא חוקי (InvalidOperationException)
        catch (InvalidOperationException ex)
        {
            // Blocked עם הודעה
            return LoadResult.Blocked(entity, ex.Message);
        }
    }

    /// <summary>
    /// בונה מהלך דומיין מבקשת הלקוח. מוסיף שגיאות קלט ל-<paramref name="errors"/>
    /// ומחזיר null אם הקלט פסול.
    /// </summary>
    // מתודה סטטית — המרת ManualMoveRequestDto ל-ManualMove
    private static ManualMove? TryBuildMove(ManualMoveRequestDto request, List<string> errors)
    {
        // תנאי — בקשה null
        if (request is null)
        {
            // הוספת שגיאה לרשימה
            errors.Add("בקשת מהלך חסרה.");
            // null — לא ניתן לבנות מהלך
            return null;
        }

        // משתנה — ParticipantId מליבה
        ParticipantId participant;
        // try — יצירת Value Object מת.ז.
        try
        {
            // בנאי ParticipantId — זורק אם לא תקין
            participant = new ParticipantId(request.ParticipantId);
        }
        // catch — ת.ז. לא חוקית
        catch (ArgumentException)
        {
            // שגיאת קלט
            errors.Add("מזהה משתתף לא תקין.");
            return null;
        }

        // תנאי — סוג מהלך Transfer (השוואה case-insensitive)
        if (string.Equals(request.MoveType, "Transfer", StringComparison.OrdinalIgnoreCase))
        {
            // pattern matching — TargetGroupId חייב להיות > 0
            if (request.TargetGroupId is not { } targetGroupValue || targetGroupValue <= 0)
            {
                // שגיאה — קבוצת יעד חסרה או לא חוקית
                errors.Add("יש לציין קבוצת יעד חוקית.");
                return null;
            }

            // יצירת מהלך העברה לקבוצה
            return ManualMove.Transfer(participant, new GroupId(targetGroupValue));
        }

        // תנאי — סוג מהלך Swap
        if (string.Equals(request.MoveType, "Swap", StringComparison.OrdinalIgnoreCase))
        {
            // משתנה — משתתף שני
            ParticipantId second;
            // try — יצירת ParticipantId למשתתף השני
            try
            {
                // ת.ז. שני; מחרוזת ריקה אם null
                second = new ParticipantId(request.SecondParticipantId ?? string.Empty);
            }
            // catch — ת.ז. שנייה לא תקינה
            catch (ArgumentException)
            {
                errors.Add("מזהה המשתתף השני לא תקין.");
                return null;
            }

            // תנאי — אותו משתתף פעמיים
            if (second == participant)
            {
                errors.Add("לא ניתן להחליף משתתף עם עצמו.");
                return null;
            }

            // יצירת מהלך החלפה
            return ManualMove.Swap(participant, second);
        }

        // סוג מהלך לא מזוהה
        errors.Add("סוג מהלך לא נתמך.");
        // null — כשל בבנייה
        return null;
    }

    // מחלקה פנימית sealed — תוצאת טעינת הקשר
    private sealed class LoadResult
    {
        // האם החלוקה נמצאה (גם אם חסומה)
        public bool Found { get; private init; }

        // האם יש הקשר מלא לביצוע מהלך
        public bool HasContext { get; private init; }

        // ישות EF של החלוקה
        public DataAssignment? Entity { get; private init; }

        // חלוקת דומיין משוחזרת מ-JSON
        public CoreAssignment? Current { get; private init; }

        // רשימת משתתפים מקלט האלגוריתם
        public IReadOnlyList<CoreParticipant>? Participants { get; private init; }

        // רשימת אילוצים
        public IReadOnlyList<IConstraint>? Constraints { get; private init; }

        // הודעת חסימה כשאין הקשר
        public string? BlockingMessage { get; private init; }

        // factory — חלוקה לא נמצאה / אין הרשאה
        public static LoadResult NotFound() => new() { Found = false };

        // factory — נמצאה אך חסומה (אין שיבוץ וכו')
        public static LoadResult Blocked(DataAssignment entity, string message) => new()
        {
            // נמצאה ברשומה
            Found = true,
            // אין הקשר לביצוע
            HasContext = false,
            // שמירת הישות
            Entity = entity,
            // הודעה ללקוח
            BlockingMessage = message,
        };

        // factory — הקשר מלא מוכן להערכה
        public static LoadResult Ready(
            // ישות חלוקה
            DataAssignment entity,
            // חלוקה נוכחית
            CoreAssignment current,
            // משתתפים
            IReadOnlyList<CoreParticipant> participants,
            // אילוצים
            IReadOnlyList<IConstraint> constraints) => new()
        {
            // נמצאה
            Found = true,
            // הקשר מלא
            HasContext = true,
            // ישות
            Entity = entity,
            // חלוקה
            Current = current,
            // משתתפים
            Participants = participants,
            // אילוצים
            Constraints = constraints,
        };
    }
}

/// <summary>
/// תוצאת Apply — מתורגמת בבקר לקוד HTTP מתאים.
/// </summary>
// מחלקה sealed — עטיפת תוצאות Apply לבקר
public sealed class ManualMoveApplyOutcome
{
    // enum פנימי — סוג התוצאה
    public enum ResultKind
    {
        // חלוקה לא נמצאה
        NotFound,
        // חסום — לא ניתן לבצע
        Blocked,
        // דורש אישור חריגה
        NeedsOverride,
        // בוצע בהצלחה
        Applied
    }

    // בנאי פרטי — יצירה רק דרך factory methods
    private ManualMoveApplyOutcome(
        // סוג התוצאה
        ResultKind status,
        // פירוט מעודכן 
        AssignmentDetailDto? detail,
        // תצוגה מקדימה  
        ManualMovePreviewResult? preview)
    {
         Status = status;
         Detail = detail;
         Preview = preview;
    }

    // מאפיין קריאה בלבד — סוג התוצאה
    public ResultKind Status { get; }

    // מאפיין — DTO פירוט לאחר Apply מוצלח
    public AssignmentDetailDto? Detail { get; }

    // מאפיין — תוצאת preview לחסימה או בקשת override
    public ManualMovePreviewResult? Preview { get; }

    // factory — NotFound
    public static ManualMoveApplyOutcome NotFound() =>
        // new עם Status=NotFound, ללא detail ו-preview
        new(ResultKind.NotFound, null, null);

    // factory — Blocked עם preview
    public static ManualMoveApplyOutcome Blocked(ManualMovePreviewResult preview) =>
        // Status=Blocked, preview מלא
        new(ResultKind.Blocked, null, preview);

    // factory — NeedsOverride
    public static ManualMoveApplyOutcome NeedsOverride(ManualMovePreviewResult preview) =>
        // Status=NeedsOverride
        new(ResultKind.NeedsOverride, null, preview);

    // factory — Applied עם פירוט
    public static ManualMoveApplyOutcome Applied(AssignmentDetailDto? detail) =>
        // Status=Applied, detail מלא
        new(ResultKind.Applied, detail, null);
}