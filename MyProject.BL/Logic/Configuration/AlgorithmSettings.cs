using System;

namespace MyProject.BL.Logic.Configuration;

/// <summary>
/// הגדרות לאלגוריתם ההקצאה.
/// </summary>
/// <remarks>
/// מחלקה זו מוכנה לשימוש עתידי בשכבת ה-Algorithm.
/// הגדרות אלו אינן פעילות עדיין.
/// </remarks>
public sealed class AlgorithmSettings
{
    /// <summary>
    /// מספר מקסימלי ברירת מחדל של איטרציות.
    /// </summary>
    public const int DefaultMaxIterations = 1000;

    /// <summary>
    /// מספר מועמדים ברירת מחדל להערכה בכל צעד.
    /// </summary>
    public const int DefaultCandidateCount = 10;

    /// <summary>
    /// מספר ניסיונות תיקון ברירת מחדל.
    /// </summary>
    public const int DefaultRepairAttempts = 5;

    /// <summary>
    /// מאתחל מופע חדש של <see cref="AlgorithmSettings"/> עם הגדרות ברירת מחדל.
    /// </summary>
    public AlgorithmSettings()
        : this(DefaultMaxIterations, DefaultCandidateCount, DefaultRepairAttempts)
    {
    }

    /// <summary>
    /// מאתחל מופע חדש של <see cref="AlgorithmSettings"/> עם ערכים מותאמים.
    /// </summary>
    /// <param name="maxIterations">מספר מקסימלי של איטרציות. חייב להיות גדול מאפס.</param>
    /// <param name="candidateCount">מספר מועמדים להערכה בכל צעד. חייב להיות גדול מאפס.</param>
    /// <param name="repairAttempts">מספר ניסיונות תיקון. חייב להיות גדול מאפס.</param>
    /// <exception cref="ArgumentOutOfRangeException">נזרק כאשר אחד מהערכים אינו חיובי.</exception>
    public AlgorithmSettings(int maxIterations, int candidateCount, int repairAttempts)
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

        MaxIterations = maxIterations;
        CandidateCount = candidateCount;
        RepairAttempts = repairAttempts;
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
}
