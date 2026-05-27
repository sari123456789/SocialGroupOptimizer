using System.Security.Cryptography;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.InitialPlacement.Runtime;

/// <summary>
/// יוצר הקשרי ריצה עבור ריצות של ההצבה הראשונית.
/// </summary>
public static class ExecutionContextFactory
{
    /// <summary>
    /// יוצר הקשר ריצה חדש עם ערך בסיס שנוצר אוטומטית.
    /// </summary>
    /// <returns>הקשר ריצה חדש.</returns>
    public static ExecutionContext Create()
    {
        return Create(GenerateRunSeed());
    }

    /// <summary>
    /// יוצר הקשר ריצה חדש עם ערך בסיס שסופק מבחוץ.
    /// </summary>
    /// <param name="runSeed">ערך בסיס להתנהגות אקראית ניתנת לשחזור.</param>
    /// <returns>הקשר ריצה חדש.</returns>
    public static ExecutionContext Create(int runSeed)
    {
        return new ExecutionContext(AlgorithmRunId.New(), runSeed);
    }

    private static int GenerateRunSeed()
    {
        return RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
    }
}
