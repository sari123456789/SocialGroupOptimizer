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
    /// סף ברירת מחדל ליחס מבודדים (0.0–1.0) להפעלת אסטרטגיית מבודדים.
    /// </summary>
    public const double DefaultHighIsolationRatioThreshold = 0.05;

    /// <summary>
    /// סף ברירת מחדל ליחס קבוצות חלשות (0.0–1.0).
    /// </summary>
    public const double DefaultHighWeakGroupRatioThreshold = 0.20;

    /// <summary>
    /// סף ברירת מחדל לקיפאון קל — איטרציות ללא שיפור.
    /// </summary>
    public const int DefaultLightStagnationThreshold = 8;

    /// <summary>
    /// סף ברירת מחדל לקיפאון כבד — איטרציות ללא שיפור.
    /// </summary>
    public const int DefaultHeavyStagnationThreshold = 15;

    /// <summary>
    /// ברירת מחדל — להפעיל חיפוש מקומי אחרי חלוקה ראשונית.
    /// </summary>
    public const bool DefaultEnableLocalSearch = true;

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
            DefaultClassificationBalanceTolerancePercent,
            DefaultHighIsolationRatioThreshold,
            DefaultHighWeakGroupRatioThreshold,
            DefaultLightStagnationThreshold,
            DefaultHeavyStagnationThreshold,
            DefaultEnableLocalSearch)
    {
    }

    /// <summary>
    /// מאתחל עם פרמטרי ליבה; שאר הגדרות הפותר נשארות בברירת מחדל.
    /// </summary>
    public AlgorithmSettings(int maxIterations, int candidateCount, int repairAttempts, int solverDifficultyThreshold)
        : this(
            maxIterations,
            candidateCount,
            repairAttempts,
            solverDifficultyThreshold,
            DefaultSolverTimeoutMs,
            DefaultSolverBaseUrl,
            DefaultSolverPollIntervalMs,
            DefaultClassificationBalanceTolerancePercent,
            DefaultHighIsolationRatioThreshold,
            DefaultHighWeakGroupRatioThreshold,
            DefaultLightStagnationThreshold,
            DefaultHeavyStagnationThreshold,
            DefaultEnableLocalSearch)
    {
    }

    /// <summary>
    /// מאתחל עם כל הפרמטרים כולל הגדרות פותר חיצוני וספי יצירת מועמדים.
    /// </summary>
    public AlgorithmSettings(
        int maxIterations,
        int candidateCount,
        int repairAttempts,
        int solverDifficultyThreshold,
        int solverTimeoutMs,
        string solverBaseUrl,
        int solverPollIntervalMs,
        int classificationBalanceTolerancePercent,
        double highIsolationRatioThreshold = DefaultHighIsolationRatioThreshold,
        double highWeakGroupRatioThreshold = DefaultHighWeakGroupRatioThreshold,
        int lightStagnationThreshold = DefaultLightStagnationThreshold,
        int heavyStagnationThreshold = DefaultHeavyStagnationThreshold,
        bool enableLocalSearch = DefaultEnableLocalSearch)
    {
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

        if (solverDifficultyThreshold < 0 || solverDifficultyThreshold > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(solverDifficultyThreshold), "Solver difficulty threshold must be between 0 and 100.");
        }

        if (solverTimeoutMs is < 1 or > 300_000)
        {
            throw new ArgumentOutOfRangeException(nameof(solverTimeoutMs), "Solver timeout must be between 1 and 300000 milliseconds.");
        }

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

        if (highIsolationRatioThreshold is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(highIsolationRatioThreshold),
                "High isolation ratio threshold must be between 0.0 and 1.0.");
        }

        if (highWeakGroupRatioThreshold is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(highWeakGroupRatioThreshold),
                "High weak group ratio threshold must be between 0.0 and 1.0.");
        }

        if (lightStagnationThreshold <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lightStagnationThreshold),
                "Light stagnation threshold must be greater than zero.");
        }

        if (heavyStagnationThreshold <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(heavyStagnationThreshold),
                "Heavy stagnation threshold must be greater than zero.");
        }

        if (lightStagnationThreshold > heavyStagnationThreshold)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lightStagnationThreshold),
                "Light stagnation threshold must not exceed heavy stagnation threshold.");
        }

        MaxIterations = maxIterations;
        CandidateCount = candidateCount;
        RepairAttempts = repairAttempts;
        SolverDifficultyThreshold = solverDifficultyThreshold;
        SolverTimeoutMs = solverTimeoutMs;
        SolverBaseUrl = solverBaseUrl;
        SolverPollIntervalMs = solverPollIntervalMs;
        ClassificationBalanceTolerancePercent = classificationBalanceTolerancePercent;
        HighIsolationRatioThreshold = highIsolationRatioThreshold;
        HighWeakGroupRatioThreshold = highWeakGroupRatioThreshold;
        LightStagnationThreshold = lightStagnationThreshold;
        HeavyStagnationThreshold = heavyStagnationThreshold;
        EnableLocalSearch = enableLocalSearch;
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
    /// סף יחס מבודדים (0.0–1.0) להפעלת אסטרטגיית משתתפים מבודדים.
    /// </summary>
    public double HighIsolationRatioThreshold { get; }

    /// <summary>
    /// סף יחס קבוצות חלשות (0.0–1.0) להפעלת אסטרטגיית קבוצות חלשות.
    /// </summary>
    public double HighWeakGroupRatioThreshold { get; }

    /// <summary>
    /// קיפאון קל — איטרציות ללא שיפור; מפעיל NearMiss.
    /// </summary>
    public int LightStagnationThreshold { get; }

    /// <summary>
    /// קיפאון כבד — איטרציות ללא שיפור; מפעיל ControlledRandom.
    /// </summary>
    public int HeavyStagnationThreshold { get; }

    /// <summary>
    /// האם להריץ חיפוש מקומי לשיפור חלוקה אחרי Initial Placement.
    /// </summary>
    public bool EnableLocalSearch { get; }

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
