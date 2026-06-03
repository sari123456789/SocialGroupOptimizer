namespace MyProject.BL.Algorithm.LocalSearch.Engine;

/// <summary>
/// סיבת עצירת חיפוש מקומי — מדויקת ומדידה.
/// </summary>
public enum LocalSearchStopReason
{
    /// <summary>לא נוצרו מועמדי move באיטרציה.</summary>
    NoCandidates,

    /// <summary>אין תוצאות הערכה חוקיות.</summary>
    NoValidMoves,

    /// <summary>יש חוקיות אך אין שיפור בציון.</summary>
    NoImprovingMoves,

    /// <summary>שיפורים חוקיים מובילים רק למצבים שכבר בוקרו.</summary>
    OnlyVisitedMoves,

    /// <summary>הגיע למספר המקסימלי של איטרציות.</summary>
    MaxIterationsReached,
}
