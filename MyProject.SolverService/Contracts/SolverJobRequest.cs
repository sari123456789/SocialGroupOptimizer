namespace MyProject.SolverService.Contracts;

/// <summary>
/// גוף הבקשה לשירות הפותר — מגיע מ-MyProject.BL אחרי תרגום מהדומיין.
/// </summary>
public sealed class SolverJobRequest
{
    /// <summary>מזהה אופציונלי לעקיבה — לא חובה.</summary>
    public Guid? RequestId { get; set; }

    /// <summary>מגבלת זמן לריצת CP-SAT במילישניות.</summary>
    public int TimeoutMs { get; set; } = 30_000;

    /// <summary>מספר הקבוצות המינימלי שחייבות להיות בשימוש; 0 = ללא חסם תחתון.</summary>
    public int MinGroups { get; set; }

    /// <summary>משתתפים עם מפת סיווגים — לחישוב אילוצי Class.</summary>
    public List<SolverParticipantWire> Participants { get; set; } = new();

    /// <summary>יחידות שיבוץ — כל יחידה נשלחת לקבוצה אחת בלבד.</summary>
    public List<PlacementUnitWire> PlacementUnits { get; set; } = new();

    /// <summary>הקבוצות האפשריות עם טווחי גודל.</summary>
    public List<SolverGroupWire> Groups { get; set; } = new();

    /// <summary>זוגות יחידות שלא יכולות לשבת באותה קבוצה.</summary>
    public List<ForbiddenUnitPairWire> ForbiddenUnitPairs { get; set; } = new();

    /// <summary>אילוצי סיווג — הפרדה או איזון יחסי.</summary>
    public List<ClassificationConstraintWire> ClassificationConstraints { get; set; } = new();
}

/// <summary>משתתף בודד עם מפת מימד→רמה.</summary>
public sealed class SolverParticipantWire
{
    public string ParticipantId { get; set; } = string.Empty;

    public Dictionary<string, string> Classifications { get; set; } = new();
}

/// <summary>
/// יחידת שיבוץ — או משתתף בודד או יחידת חובה (MustLink) שלמה.
/// </summary>
public sealed class PlacementUnitWire
{
    public int UnitId { get; set; }

    public List<string> ParticipantIds { get; set; } = new();

    public PlacementUnitKindWire Kind { get; set; }
}

/// <summary>הגדרת קבוצה — מזהה וטווח גודל.</summary>
public sealed class SolverGroupWire
{
    public int GroupId { get; set; }

    public int MinSize { get; set; }

    public int MaxSize { get; set; }
}

/// <summary>שתי יחידות שאסור לשבץ יחד באותה קבוצה.</summary>
public sealed class ForbiddenUnitPairWire
{
    public int FirstUnitId { get; set; }

    public int SecondUnitId { get; set; }
}

/// <summary>
/// אילוץ סיווג בפורמט wire — סוג נקבע לפי ConstraintKind.
/// </summary>
public sealed class ClassificationConstraintWire
{
    /// <summary>שם המחלקה בליבה, למשל ClassificationHomogeneousGroupConstraint.</summary>
    public string ConstraintKind { get; set; } = string.Empty;

    public string TargetDimension { get; set; } = string.Empty;

    public string? TargetLevel { get; set; }

    public int? MinCountPerGroup { get; set; }

    public int? MaxCountPerGroup { get; set; }

    /// <summary>רמות מותרות במימד — לבדיקת הומוגניות ואיזון.</summary>
    public List<string>? AllowedLevels { get; set; }

    /// <summary>סטייה מקסימלית מותרת באיזון יחסי (נוסחת g·N − G·n).</summary>
    public long? MaxScaledDeviation { get; set; }
}
