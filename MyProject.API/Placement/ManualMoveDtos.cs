using System.Collections.Generic;
using System.Linq;
using MyProject.BL.ManualMoves;

namespace MyProject.API.Placement;

/// <summary>
/// בקשת מהלך ידני מהלקוח — העברה או החלפה.
/// </summary>
// בקשת מהלך ידני מהלקוח
public sealed class ManualMoveRequestDto
{
    /// <summary>
    /// סוג המהלך: "Transfer" או "Swap".
    /// </summary>
    // סוג המהלך — העברה או החלפה
    public string MoveType { get; set; } = string.Empty;

    /// <summary>
    /// המשתתף הראשי (מועבר / ראשון בהחלפה) — תעודת זהות.
    /// </summary>
    // תעודת זהות של המשתתף הראשי
    public string ParticipantId { get; set; } = string.Empty;

    /// <summary>
    /// קבוצת היעד — ל-Transfer בלבד.
    /// </summary>
    // קבוצת יעד — רק בהעברה
    public int? TargetGroupId { get; set; }

    /// <summary>
    /// המשתתף השני — ל-Swap בלבד.
    /// </summary>
    // תעודת זהות של המשתתף השני — רק בהחלפה
    public string? SecondParticipantId { get; set; }
}

/// <summary>
/// בקשת ביצוע מהלך ידני — כולל אישור חריגה מפורש.
/// </summary>
// בקשה לביצוע מהלך עם אישור חריגה
public sealed class ManualMoveApplyRequestDto
{
    /// <summary>
    /// פרטי המהלך.
    /// </summary>
    // פרטי המהלך לביצוע
    public ManualMoveRequestDto Move { get; set; } = new();

    /// <summary>
    /// אישור חריגה מפורש מהמנהל. ברירת מחדל: false.
    /// </summary>
    // האם המנהל אישר הפרת אילוצים
    public bool OverrideConfirmed { get; set; }
}

/// <summary>
/// תוצאת תצוגה מקדימה של מהלך ידני — נשלחת ללקוח.
/// </summary>
// תוצאת תצוגה מקדימה של מהלך
public sealed class ManualMovePreviewResult
{
    // האם המהלך חוקי בלי חריגה
    public bool IsLegal { get; set; }

    // האם אפשר לאשר חריגה
    public bool CanOverride { get; set; }

    // האם חייבים אישור חריגה לפני שמירה
    public bool RequiresOverride { get; set; }

    // ציון לפני המהלך
    public double ScoreBefore { get; set; }

    // ציון אחרי המהלך
    public double ScoreAfter { get; set; }

    // הפרש הציונים
    public double ScoreDelta { get; set; }

    // אילוצים שנשברו
    public List<BrokenConstraintDto> BrokenConstraints { get; set; } = new();

    // שגיאות שחוסמות ביצוע
    public List<string> BlockingErrors { get; set; } = new();

    // הודעה כללית ללקוח
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// ממיר הערכת BL ל-DTO תצוגה.
    /// </summary>
    // בונה DTO מהערכת BL
    public static ManualMovePreviewResult FromEvaluation(ManualMoveEvaluation evaluation)
    {
        return new ManualMovePreviewResult
        {
            IsLegal = evaluation.IsLegal,
            CanOverride = evaluation.CanOverride,
            RequiresOverride = evaluation.RequiresOverride,
            ScoreBefore = evaluation.ScoreBefore,
            ScoreAfter = evaluation.ScoreAfter,
            ScoreDelta = evaluation.ScoreDelta,
            BrokenConstraints = evaluation.BrokenConstraints
                .Select(BrokenConstraintDto.FromBrokenConstraint)
                .ToList(),
            BlockingErrors = evaluation.BlockingErrors.ToList(),
            Message = evaluation.Message,
        };
    }

    /// <summary>
    /// בונה תוצאת חסימה מרשימת שגיאות תקינות (ללא הערכה מלאה).
    /// </summary>
    // בונה תוצאת חסימה כשיש רק שגיאות
    public static ManualMovePreviewResult Blocking(IReadOnlyList<string> blockingErrors)
    {
        return new ManualMovePreviewResult
        {
            IsLegal = false,
            CanOverride = false,
            RequiresOverride = false,
            ScoreBefore = 0,
            ScoreAfter = 0,
            ScoreDelta = 0,
            BrokenConstraints = new List<BrokenConstraintDto>(),
            BlockingErrors = blockingErrors.ToList(),
            Message = "לא ניתן לבצע שינוי זה.",
        };
    }
}

/// <summary>
/// DTO לאילוץ עסקי שנשבר.
/// </summary>
// אילוץ שנשבר במהלך
public sealed class BrokenConstraintDto
{
    // סוג האילוץ
    public string ConstraintType { get; set; } = string.Empty;

    /// <summary>
    /// מזהה אילוץ במסד — אינו זמין כיום ברמת הדומיין (null).
    /// </summary>
    // מזהה במסד — כרגע תמיד null
    public int? ConstraintId { get; set; }

    // הודעה על ההפרה
    public string Message { get; set; } = string.Empty;

    // חומרת ההפרה
    public string Severity { get; set; } = string.Empty;

    // האם אפשר לאשר חריגה
    public bool CanOverride { get; set; }

    // בונה DTO ממודל BL
    public static BrokenConstraintDto FromBrokenConstraint(BrokenConstraint broken)
    {
        return new BrokenConstraintDto
        {
            ConstraintType = broken.ConstraintType.ToString(),
            ConstraintId = null,
            Message = broken.Message,
            Severity = broken.Severity,
            CanOverride = broken.CanOverride,
        };
    }
}