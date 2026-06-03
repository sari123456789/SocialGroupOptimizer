namespace MyProject.BL.Algorithm.LocalSearch.Moves;

/// <summary>
/// הצעת מועמד — עוטף MoveCandidate עם מטא-דאטה של שלב הייצור.
/// </summary>
/// <remarks>
/// <para>תפקיד: נושא מידע ייצור (מקור, סיבה, עדיפות) בלי לזהם את ה-move עצמו.</para>
/// <para>נקרא מ-: Generation.Strategies (יצירה), SwapMoveGenerator (דירוג), MoveEvaluator (הערכה).</para>
/// <para>המטא-דאטה משמשת לדירוג ולוגים — לא משפיעה על ApplyToClone / אימות.</para>
/// </remarks>
public sealed class CandidateProposal
{
    /// <summary>
    /// יוצר הצעת מועמד עם מטא-דאטה.
    /// </summary>
    /// <param name="move">ה-move עצמו — Swap או Transfer (factory על MoveCandidate).</param>
    /// <param name="source">enum — איזו אסטרטגיה ייצרה (למדיניות ולדירוג משני).</param>
    /// <param name="reason">מחרוזת לדיבוג — לא משמשת באלגוריתם.</param>
    /// <param name="priority">double — גבוה יותר = עדיף ב-OrderByDescending ב-SwapMoveGenerator.</param>
    public CandidateProposal(
        MoveCandidate move,
        CandidateSource source,
        string reason,
        double priority)
    {
        Move = move ?? throw new ArgumentNullException(nameof(move));
        Source = source;
        // ?? string.Empty — מונע null ב-Reason; property תמיד non-null.
        Reason = reason ?? string.Empty;
        Priority = priority;
    }

    /// <summary>ה-move המוצע — Swap או Transfer.</summary>
    public MoveCandidate Move { get; }

    /// <summary>האסטרטגיה שייצרה את ההצעה.</summary>
    public CandidateSource Source { get; }

    /// <summary>סיבה קריאה לאדם — לדיבוג ולוגים בלבד.</summary>
    public string Reason { get; }

    /// <summary>עדיפות לדירוג; גבוה יותר = עדיף יותר.</summary>
    public double Priority { get; }
}
