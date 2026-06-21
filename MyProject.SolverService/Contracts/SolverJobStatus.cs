namespace MyProject.SolverService.Contracts;

/// <summary>
/// מצב עבודת פותר לאורך מחזור החיים — ממתין → רץ → תוצאה סופית.
/// </summary>
public enum SolverJobStatus
{
    /// <summary>הוגשה וממתינה לריצה.</summary>
    Pending,

    /// <summary>CP-SAT רץ כרגע.</summary>
    Running,

    /// <summary>נמצאה חלוקה חוקית.</summary>
    Success,

    /// <summary>אין פתרון שעומד בכל האילוצים.</summary>
    Infeasible,

    /// <summary>הקלט נדחה בוולידציה לפני הרצת הפותר.</summary>
    InvalidInput,

    /// <summary>נגמר הזמן לפני שמצא פתרון.</summary>
    Timeout,

    /// <summary>כשל טכני או ביטול.</summary>
    Failed,
}
