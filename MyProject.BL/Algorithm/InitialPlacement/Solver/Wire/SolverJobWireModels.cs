namespace MyProject.BL.Algorithm.InitialPlacement.Solver.Wire;

/// <summary>
/// תפקיד: סטטוס job בפרוטокול wire של שירות הפותר.
/// </summary>
/// <remarks>מתורגם ל-<see cref="Models.SolverResponseStatus"/> ב-<see cref="ExternalSolverClient"/>.</remarks>
public enum SolverJobStatusWire
{
    /// <summary>ממתין לעיבוד.</summary>
    Pending,

    /// <summary>בעיבוד.</summary>
    Running,

    /// <summary>הושלם בהצלחה.</summary>
    Success,

    /// <summary>בעיה לא ניתנת לפתרון.</summary>
    Infeasible,

    /// <summary>קלט לא תקין.</summary>
    InvalidInput,

    /// <summary>חריגת זמן.</summary>
    Timeout,

    /// <summary>כישלון כללי.</summary>
    Failed,
}

/// <summary>
/// תפקיד: סוג יחידת שיבוץ בפורמט wire.
/// </summary>
public enum PlacementUnitKindWire
{
    /// <summary>משתתף בודד.</summary>
    SingleParticipant,

    /// <summary>יחידת חובה.</summary>
    MandatoryUnit,
}

/// <summary>
/// תפקיד: גוף בקשה ליצירת job פותר — נשלח ב-POST לשירות החיצוני.
/// </summary>
/// <remarks>נוצר ע"י <see cref="SolverJobWireMapper.ToSubmitWire"/>.</remarks>
public sealed class SolverJobSubmitWire
{
    /// <summary>מזהה בקשה ייחודי (אופציונלי).</summary>
    public Guid? RequestId { get; set; }

    /// <summary>מגבלת זמן במילישניות.</summary>
    public int TimeoutMs { get; set; }

    /// <summary>משתתפים עם סיווגים.</summary>
    public List<SolverParticipantWire> Participants { get; set; } = new();

    /// <summary>יחידות שיבוץ.</summary>
    public List<PlacementUnitWire> PlacementUnits { get; set; } = new();

    /// <summary>קבוצות יעד.</summary>
    public List<SolverGroupWire> Groups { get; set; } = new();

    /// <summary>זוגות יחידות אסורים.</summary>
    public List<ForbiddenUnitPairWire> ForbiddenUnitPairs { get; set; } = new();

    /// <summary>אילוצי סיווג.</summary>
    public List<ClassificationConstraintWire> ClassificationConstraints { get; set; } = new();
}

/// <summary>
/// תפקיד: משתתף בפורמט wire.
/// </summary>
public sealed class SolverParticipantWire
{
    /// <summary>מזהה משתתף.</summary>
    public string ParticipantId { get; set; } = string.Empty;

    /// <summary>מפת סיווגים.</summary>
    public Dictionary<string, string> Classifications { get; set; } = new();
}

/// <summary>
/// תפקיד: יחידת שיבוץ בפורמט wire.
/// </summary>
public sealed class PlacementUnitWire
{
    /// <summary>מזהה יחידה.</summary>
    public int UnitId { get; set; }

    /// <summary>משתתפי היחידה.</summary>
    public List<string> ParticipantIds { get; set; } = new();

    /// <summary>סוג היחידה.</summary>
    public PlacementUnitKindWire Kind { get; set; }
}

/// <summary>
/// תפקיד: קבוצת יעד בפורמט wire.
/// </summary>
public sealed class SolverGroupWire
{
    /// <summary>מזהה קבוצה.</summary>
    public int GroupId { get; set; }

    /// <summary>גודל מינימלי.</summary>
    public int MinSize { get; set; }

    /// <summary>גודל מקסימלי.</summary>
    public int MaxSize { get; set; }
}

/// <summary>
/// תפקיד: זוג יחידות אסור בפורמט wire.
/// </summary>
public sealed class ForbiddenUnitPairWire
{
    /// <summary>מזהה יחידה ראשונה.</summary>
    public int FirstUnitId { get; set; }

    /// <summary>מזהה יחידה שנייה.</summary>
    public int SecondUnitId { get; set; }
}

/// <summary>
/// תפקיד: אילוץ סיווג בפורמט wire.
/// </summary>
public sealed class ClassificationConstraintWire
{
    /// <summary>שם סוג האילוץ.</summary>
    public string ConstraintKind { get; set; } = string.Empty;

    /// <summary>מימד הסיווג.</summary>
    public string TargetDimension { get; set; } = string.Empty;

    /// <summary>רמה יעד.</summary>
    public string? TargetLevel { get; set; }

    /// <summary>מינימום לקבוצה.</summary>
    public int? MinCountPerGroup { get; set; }

    /// <summary>מקסימום לקבוצה.</summary>
    public int? MaxCountPerGroup { get; set; }

    /// <summary>רמות מותרות.</summary>
    public List<string>? AllowedLevels { get; set; }

    /// <summary>סטייה מקסימלית מותרת.</summary>
    public long? MaxScaledDeviation { get; set; }
}

/// <summary>
/// תפקיד: תשובה ליצירת job — מזהה job וסטטוס ראשוני.
/// </summary>
/// <remarks>מתקבל מ-POST submit ב-<see cref="ExternalSolverClient"/>.</remarks>
public sealed class SolverJobCreatedWire
{
    /// <summary>מזהה ה-job ל-polling.</summary>
    public Guid JobId { get; set; }

    /// <summary>סטטוס ראשוני (בדרך כלל Pending).</summary>
    public SolverJobStatusWire Status { get; set; }
}

/// <summary>
/// תפקיד: תשובת polling לסטטוס job — כולל שיוכים כשהושלם.
/// </summary>
/// <remarks>מתקבל מ-GET status ב-<see cref="ExternalSolverClient"/>.</remarks>
public sealed class SolverJobStatusResponseWire
{
    /// <summary>מזהה ה-job.</summary>
    public Guid JobId { get; set; }

    /// <summary>סטטוס נוכחי.</summary>
    public SolverJobStatusWire Status { get; set; }

    /// <summary>דגל timeout מהשירות.</summary>
    public bool IsTimeout { get; set; }

    /// <summary>שיוך יחידות לקבוצות — מולא ב-Success.</summary>
    public Dictionary<int, int> UnitGroupAssignments { get; set; } = new();

    /// <summary>שגיאות מהשירות.</summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// תפקיד: תשובת שגיאה (HTTP 400) מהשירות.
/// </summary>
/// <remarks>מתקבל ב-<see cref="ExternalSolverClient"/> כשהבקשה נדחית.</remarks>
public sealed class SolverErrorWire
{
    /// <summary>סטטוס השגיאה.</summary>
    public SolverJobStatusWire Status { get; set; }

    /// <summary>דגל timeout.</summary>
    public bool IsTimeout { get; set; }

    /// <summary>הודעות שגיאה.</summary>
    public List<string> Errors { get; set; } = new();
}
