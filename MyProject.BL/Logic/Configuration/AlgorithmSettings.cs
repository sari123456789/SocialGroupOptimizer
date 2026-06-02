using System;
using MyProject.Core.Domain.Constraints;

namespace MyProject.BL.Logic.Configuration;

/// <summary>
/// הגדרות תצורה לאלגוריתם החלוקה הראשונית והפותר החיצוני.
/// </summary>
/// <remarks>
/// <para>נקרא מ-: <see cref="Algorithm.InitialPlacement.Orchestration.InitialPlacementOrchestrator"/>,
/// <c>MyProject.API/Configuration/AlgorithmSettingsRegistration.cs</c>.</para>
/// </remarks>
public sealed class AlgorithmSettings
{
    /// <summary>
    /// מספר מקסימלי ברירת מחדל של איטרציות בחיפוש מקומי.
    /// </summary>
    public const int DefaultMaxIterations = 1000;

    /// <summary>
    /// מספר מועמדים ברירת מחדל להערכה בכל צעד חיפוש.
    /// </summary>
    public const int DefaultCandidateCount = 10;

    /// <summary>
    /// מספר ניסיונות תיקון ברירת מחדל.
    /// </summary>
    public const int DefaultRepairAttempts = 5;

    /// <summary>
    /// סף קושי ברירת מחדל (0–100) למעבר ממסלול חמדני לפותר חיצוני.
    /// </summary>
    public const int DefaultSolverDifficultyThreshold = 70;

    /// <summary>
    /// מגבלת זמן ברירת מחדל לפותר החיצוני (מילישניות).
    /// </summary>
    public const int DefaultSolverTimeoutMs = 30_000;

    /// <summary>
    /// כתובת בסיס ברירת מחדל לשירות הפותר החיצוני.
    /// </summary>
    public const string DefaultSolverBaseUrl = "http://localhost:5080";

    /// <summary>
    /// מרווח ברירת מחדל בין בדיקות סטטוס לפותר (מילישניות).
    /// </summary>
    public const int DefaultSolverPollIntervalMs = 500;

    /// <summary>
    /// סטייה מותרת באחוזים מאיזון סיווג יחסי (0 = מדויק, 10 = עד 10%).
    /// </summary>
    public const int DefaultClassificationBalanceTolerancePercent = 10;

    /// <summary>
    /// מאתחל עם כל ערכי ברירת המחדל.
    /// </summary>
    public AlgorithmSettings()
        : this(
            DefaultMaxIterations,
            DefaultCandidateCount,
            DefaultRepairAttempts,
            DefaultSolverDifficultyThreshold,
            DefaultSolverTimeoutMs,
            DefaultSolverBaseUrl,
            DefaultSolverPollIntervalMs,
            DefaultClassificationBalanceTolerancePercent)
    {
    }

    /// <summary>
    /// מאתחל עם פרמטרי ליבה; שאר הגדרות הפותר נשארות בברירת מחדל.
    /// </summary>
    /// <param name="maxIterations">מקסימום איטרציות — חייב להיות &gt; 0.</param>
    /// <param name="candidateCount">מועמדים לצעד — חייב להיות &gt; 0.</param>
    /// <param name="repairAttempts">ניסיונות תיקון — חייב להיות &gt; 0.</param>
    /// <param name="solverDifficultyThreshold">סף מעבר לפותר — בין 0 ל-100.</param>
    /// <exception cref="ArgumentOutOfRangeException">כאשר ערך אינו בטווח המותר.</exception>
    public AlgorithmSettings(int maxIterations, int candidateCount, int repairAttempts, int solverDifficultyThreshold)
        : this(
            maxIterations,
            candidateCount,
            repairAttempts,
            solverDifficultyThreshold,
            DefaultSolverTimeoutMs,
            DefaultSolverBaseUrl,
            DefaultSolverPollIntervalMs,
            DefaultClassificationBalanceTolerancePercent)
    {
    }

    /// <summary>
    /// מאתחל עם כל הפרמטרים כולל הגדרות פותר חיצוני.
    /// </summary>
    /// <param name="maxIterations">מקסימום איטרציות.</param>
    /// <param name="candidateCount">מועמדים לצעד.</param>
    /// <param name="repairAttempts">ניסיונות תיקון.</param>
    /// <param name="solverDifficultyThreshold">סף קושי לפותר (0–100).</param>
    /// <param name="solverTimeoutMs">timeout לפותר (1–300000 מ"ש).</param>
    /// <param name="solverBaseUrl">כתובת בסיס לשירות הפותר — לא ריקה.</param>
    /// <param name="solverPollIntervalMs">מרווח polling לסטטוס פותר.</param>
    /// <param name="classificationBalanceTolerancePercent">סטייה מותרת באיזון סיווג (0–50).</param>
    /// <exception cref="ArgumentOutOfRangeException">כאשר מספר שלילי או מחוץ לטווח.</exception>
    /// <exception cref="ArgumentException">כאשר כתובת הפותר ריקה.</exception>
    public AlgorithmSettings(
        int maxIterations,
        int candidateCount,
        int repairAttempts,
        int solverDifficultyThreshold,
        int solverTimeoutMs,
        string solverBaseUrl,
        int solverPollIntervalMs,
        int classificationBalanceTolerancePercent)
    {
        // כל בדיקה זורקת ArgumentOutOfRangeException — fail-fast לפני שמירת ערכים לא חוקיים.
        if (maxIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxIterations), "Max iterations must be greater than zero.");
        }

        if (candidateCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(candidateCount), "Candidate count must be greater than zero.");
        }

        if (repairAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(repairAttempts), "Repair attempts must be greater than zero.");
        }

        // סף קושי — אחוז 0–100; מחוץ לטווח = הגדרה לא הגיונית.
        if (solverDifficultyThreshold < 0 || solverDifficultyThreshold > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(solverDifficultyThreshold), "Solver difficulty threshold must be between 0 and 100.");
        }

        // "is < 1 or > 300_000" — pattern matching לטווח timeout (1 מ"ש עד 5 דקות).
        if (solverTimeoutMs is < 1 or > 300_000)
        {
            throw new ArgumentOutOfRangeException(nameof(solverTimeoutMs), "Solver timeout must be between 1 and 300000 milliseconds.");
        }

        // IsNullOrWhiteSpace — דוחה null, ריק, ורווחים בלבד.
        if (string.IsNullOrWhiteSpace(solverBaseUrl))
        {
            throw new ArgumentException("Solver base URL must not be empty.", nameof(solverBaseUrl));
        }

        if (solverPollIntervalMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(solverPollIntervalMs), "Solver poll interval must be greater than zero.");
        }

        if (classificationBalanceTolerancePercent < 0 || classificationBalanceTolerancePercent > 50)
        {
            throw new ArgumentOutOfRangeException(
                nameof(classificationBalanceTolerancePercent),
                "Classification balance tolerance must be between 0 and 50 percent.");
        }

        // get-only properties — נקבעים פעם אחת בבנאי (immutable object).
        MaxIterations = maxIterations;
        CandidateCount = candidateCount;
        RepairAttempts = repairAttempts;
        SolverDifficultyThreshold = solverDifficultyThreshold;
        SolverTimeoutMs = solverTimeoutMs;
        SolverBaseUrl = solverBaseUrl;
        SolverPollIntervalMs = solverPollIntervalMs;
        ClassificationBalanceTolerancePercent = classificationBalanceTolerancePercent;
    }

    /// <summary>
    /// מספר מקסימלי של איטרציות לחיפוש מקומי.
    /// </summary>
    public int MaxIterations { get; }

    /// <summary>
    /// מספר מועמדים שייבחנו בכל שלב הערכה.
    /// </summary>
    public int CandidateCount { get; }

    /// <summary>
    /// מספר ניסיונות תיקון לפתרון לא חוקי.
    /// </summary>
    public int RepairAttempts { get; }

    /// <summary>
    /// סף קושי למעבר למסלול פותר חיצוני.
    /// </summary>
    public int SolverDifficultyThreshold { get; }

    /// <summary>
    /// מגבלת זמן לקריאה לפותר החיצוני (מילישניות).
    /// </summary>
    public int SolverTimeoutMs { get; }

    /// <summary>
    /// כתובת בסיס לשירות הפותר החיצוני.
    /// </summary>
    public string SolverBaseUrl { get; }

    /// <summary>
    /// מרווח בין בדיקות סטטוס לפותר החיצוני (מילישניות).
    /// </summary>
    public int SolverPollIntervalMs { get; }

    /// <summary>
    /// אחוז סטייה מותרת מאיזון סיווג יחסי בין קבוצות.
    /// </summary>
    public int ClassificationBalanceTolerancePercent { get; }

    /// <summary>
    /// מחשב סטייה מקסימלית בקנה מידה לפי מספר משתתפים ואחוז סובלנות.
    /// </summary>
    public long ComputeMaxScaledDeviation(int participantCount)
    {
        if (participantCount <= 0 || ClassificationBalanceTolerancePercent <= 0)
        {
            return ClassificationProportionalBalanceConstraint.DefaultMaxScaledDeviation;
        }

        return (long)Math.Ceiling(
            participantCount * (double)participantCount * ClassificationBalanceTolerancePercent / 100.0);
    }
}
