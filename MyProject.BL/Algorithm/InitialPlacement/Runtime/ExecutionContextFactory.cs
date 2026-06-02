using System.Security.Cryptography;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.InitialPlacement.Runtime;

/// <summary>
/// תפקיד: יוצר הקשרי ריצה (ExecutionContext) עם מזהה חדש ו-seed.
/// </summary>
/// <remarks>נקרא מ- API loaders; Create() עם seed אקראי או Create(int) עם seed מפורש.</remarks>
public static class ExecutionContextFactory
{
    /// <summary>
    /// תפקיד: יוצר הקשר ריצה עם seed אקראי.
    /// </summary>
    /// <returns>ExecutionContext חדש.</returns>
    /// <remarks>אין שימושים בייצור כרגע — API יוצר ExecutionContext ישירות.</remarks>
    public static ExecutionContext Create()
    {
        return Create(GenerateRunSeed());
    }

    /// <summary>
    /// תפקיד: יוצר הקשר ריצה עם seed שסופק.
    /// </summary>
    /// <param name="runSeed">ערך בסיס לשחזור התנהגות.</param>
    /// <returns>ExecutionContext עם AlgorithmRunId.New().</returns>
    /// <remarks>נקרא מ- <see cref="Create"/>; API יוצר ישירות ב-AssignmentPlacementLoader.</remarks>
    public static ExecutionContext Create(int runSeed)
    {
        // AlgorithmRunId.New() — מזהה ייחודי לריצה; runSeed לשחזור אקראיות.
        return new ExecutionContext(AlgorithmRunId.New(), runSeed);
    }

    private static int GenerateRunSeed()
    {
        // RandomNumberGenerator — מקור אקראי מאובטח ל-seed.
        return RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
    }
}
