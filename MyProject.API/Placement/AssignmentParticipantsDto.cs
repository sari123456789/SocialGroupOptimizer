namespace MyProject.API.Placement;

// מחלקה סגורה (sealed) — אובייקט העברה לנתוני משתתפים בחלוקה; לא ניתן לירוש
public sealed class AssignmentParticipantsDto
{
    // מזהה מספרי של החלוקה 
    public int AssignmentId { get; set; }

    // שם החלוקה כמחרוזת   
    public string AssignmentName { get; set; } = string.Empty;

    // רשימת קבוצות סיווג לקריאה בלבד 
    public IReadOnlyList<ClassificationGroupDto> ClassificationGroups { get; set; } =
        // Array.Empty — מערך ריק בלי הקצאת זיכרון מיותרת
        Array.Empty<ClassificationGroupDto>();

    // רשימת משתתפים לקריאה בלבד; שורת המשך — אתחול למערך ריק
    public IReadOnlyList<ParticipantListItemDto> Participants { get; set; } =
        // Array.Empty — מערך ריק כברירת מחדל עד טעינת נתונים
        Array.Empty<ParticipantListItemDto>();
}
