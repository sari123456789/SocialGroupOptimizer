using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.InitialPlacement.Runtime;

/// <summary>
/// נתוני ריצה עבור ריצה אחת של אלגוריתם ההצבה הראשונית.
/// </summary>
public sealed record ExecutionContext
{
    /// <summary>
    /// יוצר הקשר ריצה חדש.
    /// </summary>
    /// <param name="runId">מזהה ייחודי לריצת האלגוריתם.</param>
    /// <param name="runSeed">ערך בסיס להתנהגות אקראית ניתנת לשחזור.</param>
    public ExecutionContext(AlgorithmRunId runId, int runSeed)
    {
        RunId = runId;
        RunSeed = runSeed;
    }

    /// <summary>
    /// מזהה ייחודי לריצת האלגוריתם הזו.
    /// </summary>
    public AlgorithmRunId RunId { get; }

    /// <summary>
    /// ערך בסיס שניתן להשתמש בו בעתיד להתנהגות אקראית ניתנת לשחזור.
    /// </summary>
    public int RunSeed { get; }
}
