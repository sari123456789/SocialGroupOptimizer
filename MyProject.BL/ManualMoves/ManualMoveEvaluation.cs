using System.Collections.Generic;
using MyProject.Core.Domain.Entities;

namespace MyProject.BL.ManualMoves;

/// <summary>
/// תוצאת הערכת מהלך ידני — חוקיות, אפשרות חריגה, ציונים ואילוצים שנשברו.
/// </summary>
/// <remarks>
/// הערכה בלבד; אינה משנה ואינה שומרת דבר. <see cref="ResultingAssignment"/> מיועד
/// לשכבת ה-API לצורך שמירה לאחר אישור (כולל override), ואינו נשלח ללקוח.
/// </remarks>
public sealed class ManualMoveEvaluation
{
    public ManualMoveEvaluation(
        bool isLegal,
        bool requiresOverride,
        bool canOverride,
        double scoreBefore,
        double scoreAfter,
        IReadOnlyList<BrokenConstraint> brokenConstraints,
        IReadOnlyList<string> blockingErrors,
        string message,
        Assignment? resultingAssignment)
    {
        IsLegal = isLegal;
        RequiresOverride = requiresOverride;
        CanOverride = canOverride;
        ScoreBefore = scoreBefore;
        ScoreAfter = scoreAfter;
        BrokenConstraints = brokenConstraints ?? new List<BrokenConstraint>();
        BlockingErrors = blockingErrors ?? new List<string>();
        Message = message ?? string.Empty;
        ResultingAssignment = resultingAssignment;
    }

    /// <summary>
    /// האם המהלך חוקי לחלוטין (ללא הפרות).
    /// </summary>
    public bool IsLegal { get; }

    /// <summary>
    /// האם נדרש אישור חריגה כדי לשמור.
    /// </summary>
    public bool RequiresOverride { get; }

    /// <summary>
    /// האם המנהל רשאי לאשר חריגה (יש רק הפרות עסקיות).
    /// </summary>
    public bool CanOverride { get; }

    /// <summary>
    /// ציון החלוקה לפני המהלך.
    /// </summary>
    public double ScoreBefore { get; }

    /// <summary>
    /// ציון החלוקה לאחר המהלך (משוער).
    /// </summary>
    public double ScoreAfter { get; }

    /// <summary>
    /// שינוי הציון.
    /// </summary>
    public double ScoreDelta => ScoreAfter - ScoreBefore;

    /// <summary>
    /// אילוצים עסקיים שנשברו (ניתנים לאישור חריגה).
    /// </summary>
    public IReadOnlyList<BrokenConstraint> BrokenConstraints { get; }

    /// <summary>
    /// שגיאות תקינות מערכת (אינן ניתנות לאישור חריגה).
    /// </summary>
    public IReadOnlyList<string> BlockingErrors { get; }

    /// <summary>
    /// הודעת סיכום למנהל.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// החלוקה המתקבלת לאחר המהלך — לשימוש שמירה בשכבת API בלבד.
    /// </summary>
    public Assignment? ResultingAssignment { get; }
}
