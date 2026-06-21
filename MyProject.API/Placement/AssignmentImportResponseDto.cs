namespace MyProject.API.Placement;

// DTO לתשובת ייבוא מאקסל — מחלקה סגורה שלא ניתנת לירושה
public sealed class AssignmentImportResponseDto
{
    // האם הייבוא הצליח — בוליאני לסטטוס כללי
    public bool Success { get; set; }

    // מזהה החלוקה שנוצרה; nullable כי בכשלון אין מזהה
    public int? AssignmentId { get; set; }

    // שם החלוקה; nullable כי עשוי להיות חסר בכשלון
    public string? AssignmentName { get; set; }

    // מספר המשתתפים שיובאו
    public int ParticipantCount { get; set; }

    // מספר זוגות חובה שזוהו בייבוא
    public int MandatoryPairCount { get; set; }

    // מספר זוגות אסורים שזוהו בייבוא
    public int ForbiddenPairCount { get; set; }

    // מספר כללי סיווג שיובאו
    public int ClassificationRuleCount { get; set; }

    // רשימת הודעות שגיאה; new() יוצר רשימה ריקה כברירת מחדל
    public List<string> Errors { get; set; } = new();

    // מתודה סטטית — ממפה תוצאת ייבוא מהשכבת Data ל-DTO של ה-API
    public static AssignmentImportResponseDto FromResult(MyProject.Data.Import.ExcelImportResult result) =>
        //  new() — יצירת אובייקט עם אתחול מאפיינים
        new()
        {
            // העתקת דגל הצלחה מתוצאת הייבוא
            Success = result.Success,
            // העתקת מזהה החלוקה (אם קיים)
            AssignmentId = result.AssignmentId,
            // העתקת שם החלוקה
            AssignmentName = result.AssignmentName,
            // העתקת מונה משתתפים
            ParticipantCount = result.ParticipantCount,
            // העתקת מונה זוגות חובה
            MandatoryPairCount = result.MandatoryPairCount,
            // העתקת מונה זוגות אסורים
            ForbiddenPairCount = result.ForbiddenPairCount,
            // העתקת מונה כללי סיווג
            ClassificationRuleCount = result.ClassificationRuleCount,
            // המרת אוסף השגיאות לרשימה (ToList)
            Errors = result.Errors.ToList(),
        };
}
