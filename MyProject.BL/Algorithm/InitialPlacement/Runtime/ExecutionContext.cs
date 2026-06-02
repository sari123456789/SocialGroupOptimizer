using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.InitialPlacement.Runtime;

/// <summary>
/// תפקיד: נתוני ריצה לריצה אחת של InitialPlacement — מזהה ייחודי ו-seed לשחזור התנהגות.
/// </summary>
/// <remarks>
/// נוצר ע"י <see cref="ExecutionContextFactory"/>; מוזרק ל-<see cref="InitialPlacementInput"/>.
/// </remarks>
public sealed record ExecutionContext
{
    /// <summary>
    /// תפקיד: מאתחל הקשר ריצה.
    /// </summary>
    /// <param name="runId">מזהה ייחודי לריצה.</param>
    /// <param name="runSeed">ערך בסיס להתנהגות אקראית.</param>
    /// <remarks>נקרא מ- <see cref="ExecutionContextFactory.Create"/>, API loaders.</remarks>
    public ExecutionContext(AlgorithmRunId runId, int runSeed)
    {
        // record עם init בלבד — שדות immutable לכל ריצה.
        RunId = runId;
        RunSeed = runSeed;
    }

    /// <summary>
    /// תפקיד: מזהה ייחודי לריצת האלגוריתם.
    /// </summary>
    /// <remarks>נקרא מ- InitialPlacementInput.ExecutionContext — ללוגים ומעקב.</remarks>
    public AlgorithmRunId RunId { get; }

    /// <summary>
    /// תפקיד: seed לשחזור התנהגות — לשימוש עתידי באקראיות.
    /// </summary>
    /// <remarks>נשמר ב-InitialPlacementInput; אין שימוש פעיל באלגוריתם כרגע.</remarks>
    public int RunSeed { get; }
}
