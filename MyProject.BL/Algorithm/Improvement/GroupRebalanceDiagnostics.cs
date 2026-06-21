using MyProject.BL.Algorithm.LocalSearch.Engine;

namespace MyProject.BL.Algorithm.Improvement;

/// <summary>
/// נתוני דיבוג לשלב איזון קבוצות בתוך מתזמר השיפור — לניטור ולוג.
/// </summary>
public sealed class GroupRebalanceDiagnostics
{
    // האם שלב איזון הקבוצות הופעל בכלל (יכול להיות מושבת לצורך השוואה)
    public bool EnableLocalSearch { get; init; }

    // סיבה לעצירת הלולאה הראשונה של חיפוש מקומי (אם הופעל)
    public LocalSearchStopReason FirstLocalSearchStopReason { get; init; }

    // סיבה לעצירת הלולאה הסופית של חיפוש מקומי (אם הופעל)
    public LocalSearchStopReason FinalLocalSearchStopReason { get; init; }

    // האם ניסה לבצע איזון קבוצות בכלל (יכול להיות שלא נדרש אם הפתרון כבר טוב)
    public bool GroupRebalanceAttempted { get; init; }

    // כמה מעבר בין קבוצות (עם שיפור ציון) נמצא במהלך שלב האיזון
    public int SplitClusterProfilesFoundOnLastPass { get; init; }

    // כמה מועמדים להחלפה בין קבוצות הוערכו במהלך שלב האיזון
    public int TotalCandidatesEvaluated { get; init; }

    // האם הפתרון הטוב ביותר שנמצא במהלך שלב האיזון היה null (לא נמצא פתרון טוב יותר)
    public bool BestResultWasNullOnLastPass { get; init; }

    // השינוי בציון (delta) של הפתרון הטוב ביותר שנמצא במהלך שלב האיזון האחרון, אם נמצא פתרון טוב יותר
    public double? BestScoreDeltaOnLastPass { get; init; }

    // האם בסופו של דבר הוחלף משתתף בין קבוצות (כלומר, האם איזון הקבוצות יושם בפועל)
    public bool GroupRebalanceWasApplied { get; init; }

    // כמה מעבר בין קבוצות (עם שיפור ציון) יושמו בפועל במהלך שלב האיזון
    public int GroupRebalancePassesApplied { get; init; }

    // כמה פעמים הופעלה לולאת חיפוש מקומי לאחר המעבר הראשון בין קבוצות (אם בכלל הופעל)
    public int LocalSearchRunsAfterFirst { get; init; }

    // פירוט מפורט של כל מעבר בין קבוצות שנמצא במהלך שלב האיזון, כולל סיבות עצירת חיפוש מקומי, מספר מועמדים שהוערכו, שיפור הציון שנמצא והאם הוחלף בפועל
    public IReadOnlyList<GroupRebalancePassDiagnostics> Passes { get; init; } =
        Array.Empty<GroupRebalancePassDiagnostics>();
}

/// <summary>
/// פירוט ניסיון איזון בודד — מעבר אחד בלולאת ה-fallback.
/// </summary>
public sealed class GroupRebalancePassDiagnostics
{
    // אינדקס המעבר בלולאה (מתחיל מ-1) — יכול להיות שימושי לניתוח סדר האירועים
    public int PassIndex { get; init; }

    // סיבה לעצירת חיפוש מקומי במהלך מעבר זה (אם הופעל)
    public LocalSearchStopReason LocalSearchStopReasonBeforePass { get; init; }

    // כמה מעבר בין קבוצות (עם שיפור ציון) נמצא במהלך מעבר זה
    public int SplitClusterProfilesFound { get; init; }

    // כמה מועמדים להחלפה בין קבוצות הוערכו במהלך מעבר זה
    public int CandidatesEvaluated { get; init; }

    // האם הפתרון הטוב ביותר שנמצא במהלך מעבר זה היה null (לא נמצא פתרון טוב יותר)
    public bool BestResultWasNull { get; init; }

    // השינוי בציון (delta) של הפתרון הטוב ביותר שנמצא במהלך מעבר זה, אם נמצא פתרון טוב יותר
    public double? BestScoreDelta { get; init; }

    // האם בסופו של דבר הוחלף משתתף בין קבוצות (כלומר, האם איזון הקבוצות יושם בפועל) במהלך מעבר זה
    public bool Applied { get; init; }
}
