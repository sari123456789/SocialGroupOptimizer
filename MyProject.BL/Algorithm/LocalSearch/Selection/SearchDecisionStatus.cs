namespace MyProject.BL.Algorithm.LocalSearch.Selection;

/// <summary>
/// סטטוס החלטת חיפוש — האם נבחר מהלך ומדוע לא אם לא.
/// </summary>
public enum SearchDecisionStatus
{
    /// <summary>נבחר מהלך לביצוע.</summary>
    MoveSelected,

    /// <summary>אין אף תוצאת הערכה חוקית.</summary>
    NoValidMoves,

    /// <summary>יש חוקיות אך אין שיפור בציון (ScoreDelta &lt;= 0).</summary>
    NoImprovingMoves,

    /// <summary>יש שיפורים חוקיים אך כולם מובילים למצב שכבר בוקר.</summary>
    OnlyVisitedMoves,
}
