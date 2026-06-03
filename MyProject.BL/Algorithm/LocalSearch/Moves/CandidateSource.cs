namespace MyProject.BL.Algorithm.LocalSearch.Moves;

/// <summary>
/// מקור האסטרטגיה שיצרה הצעת move.
/// </summary>
/// <remarks>
/// <para>תפקיד: תיוג הצעת מועמד לפי האסטרטגיה שייצרה אותה — לדירוג, לוגים ודיבוג.</para>
/// <para>נקרא מ-: <see cref="CandidateProposal"/>, <see cref="Generation.MoveGenerationPolicy"/>.</para>
/// <para>enum — ערכים מספריים 0,1,2…; ThenBy על Source ב-SwapMoveGenerator ממיין לפי סדר ההגדרה.</para>
/// </remarks>
public enum CandidateSource
{
    /// <summary>אסטרטגיית משתתפים מבודדים — משתתף עם העדפות שאף מועדף לא בקבוצתו.</summary>
    IsolatedParticipant,

    /// <summary>אסטרטגיית תרומה נמוכה — משתתף שתורם מעט לקבוצתו הנוכחית.</summary>
    LowContribution,

    /// <summary>אסטרטגיית קבוצות חלשות — קבוצה עם לכידות חברתית נמוכה.</summary>
    LowScoreGroup,

    /// <summary>אסטרטגיית כמעט שיפור — חלק מהמועדפים בפנים וחלק בחוץ.</summary>
    NearMiss,

    /// <summary>אסטרטגיית בחירה אקראית מבוקרת — fallback ליציאה מסטגנציה.</summary>
    ControlledRandom,
}
