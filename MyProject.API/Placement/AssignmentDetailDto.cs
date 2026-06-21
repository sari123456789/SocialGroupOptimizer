namespace MyProject.API.Placement;

// DTO מפורט לחלוקה — כולל הגדרות, משתתפים, אילוצים ותוצאות שיבוץ
public sealed class AssignmentDetailDto
{
    // מזהה ייחודי של החלוקה במערכת
    public int AssignmentId { get; set; }

    // שם תצוגה של החלוקה; ברירת מחדל מחרוזת ריקה
    public string AssignmentName { get; set; } = string.Empty;

    // סטטוס החלוקה; ברירת מחדל "ממתין לאימות"
    public string Status { get; set; } = "PendingValidation";

    // מספר המשתתפים בחלוקה
    public int ParticipantCount { get; set; }

    // סטטוס השיבוץ האחרון; nullable אם טרם בוצע שיבוץ
    public string? LastPlacementStatus { get; set; }

    // שגיאות אימות שנאספו; רשימה ריקה כברירת מחדל
    public List<string> ValidationErrors { get; set; } = new();

    // קבוצות השיבוץ הנוכחיות; רשימה ריקה עד טעינה
    public List<PlacementGroupDto> PlacementGroups { get; set; } = new();

    // ציון השיבוץ הנוכחי; double? מאפשר null כשאין ציון
    public double? PlacementScore { get; set; }

    // ציון השיבוץ הראשוני לפני שיפורים; nullable
    public double? InitialPlacementScore { get; set; }

    // הגדרות האלגוריתם והאילוצים של החלוקה; אובייקט חדש כברירת מחדל
    public AssignmentSettingsDto Settings { get; set; } = new();

    // רשימת כל המשתתפים עם פרטיהם
    public List<ParticipantListItemDto> Participants { get; set; } = new();

    // זוגות שחייבים להיות באותה קבוצה
    public List<PairConstraintItemDto> MandatoryPairs { get; set; } = new();

    // זוגות שאסור שיהיו באותה קבוצה
    public List<PairConstraintItemDto> ForbiddenPairs { get; set; } = new();

    // אילוצי איזון/הומוגניות לפי ממדי סיווג
    public List<ClassificationConstraintItemDto> ClassificationConstraints { get; set; } = new();

    // שמות ממדי הסיווג הזמינים בחלוקה זו
    public List<string> AvailableDimensions { get; set; } = new();
}