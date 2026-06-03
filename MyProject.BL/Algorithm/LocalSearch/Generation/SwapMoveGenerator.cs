using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.BL.Algorithm.LocalSearch.RuntimeData;
using MyProject.BL.Algorithm.SolutionState;
using MyProject.BL.Logic.Configuration;

namespace MyProject.BL.Algorithm.LocalSearch.Generation;

/// <summary>
/// מחולל החלפות מרכזי — מפעיל אסטרטגיות, מאחד, מנקה כפילויות, מדרג ומגביל.
/// </summary>
/// <remarks>
/// <para>תפקיד: מרכז ייצור מועמדים בלבד — אינו מעריך ציון, אינו מאמת חוקיות ואינו מבצע moves.</para>
/// <para>נקרא מ-: מתזמר Local Search עתידי — לפני שלב ההערכה (MoveEvaluationBatch).</para>
/// <para>בחירת האסטרטגיות מואצלת ל-<see cref="MoveGenerationPolicy"/>.</para>
/// </remarks>
public sealed class SwapMoveGenerator
{
    private readonly MoveGenerationPolicy _policy;
    private readonly int _candidateLimit;

    /// <summary>
    /// יוצר מחולל עם מדיניות בחירה והגדרות אלגוריתם.
    /// </summary>
    /// <param name="policy">מדיניות הבוחרת אילו אסטרטגיות ירוצו.</param>
    /// <param name="settings">הגדרות — מספק את מגבלת המועמדים (CandidateCount).</param>
    public SwapMoveGenerator(MoveGenerationPolicy policy, AlgorithmSettings settings)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));

        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        // CandidateCount > 0 מובטח ב-AlgorithmSettings — שומרים מקומית למגבלת Take.
        _candidateLimit = settings.CandidateCount;
    }

    /// <summary>
    /// מייצר רשימת מועמדי move מדורגת ומוגבלת למצב הנוכחי.
    /// </summary>
    /// <param name="state">מצב החלוקה הנוכחי — לקריאה בלבד; לא משתנה כאן.</param>
    /// <param name="snapshot">נתוני הריצה (פרופילים ואינדקסים) — נבנה מחוץ, לא בכל איטרציה.</param>
    /// <param name="context">מצב החיפוש (איטרציה, סטגנציה) — לבחירת אסטרטגיות.</param>
    /// <returns>עד CandidateCount הצעות מלאות (Move + Source + Reason + Priority).</returns>
    public IReadOnlyList<CandidateProposal> Generate(
        AssignmentState state,
        RuntimeDataSnapshot snapshot,
        MoveGenerationContext context)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // שלב 1: המדיניות מחזירה תת-רשימה של IMoveCandidateStrategy — לא מייצרת moves.
        var strategies = _policy.SelectStrategies(snapshot, context);

        // שלב 2: איחוד — List + AddRange מוסיף את כל האלמנטים מכל אסטרטגיה לרשימה אחת.
        var merged = new List<CandidateProposal>();
        foreach (var strategy in strategies)
        {
            // Generate מחזיר IReadOnlyList — AddRange מקבל IEnumerable ומעתיק.
            merged.AddRange(strategy.Generate(state, snapshot, context));
        }

        if (merged.Count == 0)
        {
            return Array.Empty<CandidateProposal>();
        }

        // שלב 3: Deduplicate — HashSet מפתחות; שומר הצעה ראשונה לכל move זהה.
        var unique = CandidateDeduplicator.Deduplicate(merged);

        // שלב 4–5: LINQ על IEnumerable — לא משנה את unique, יוצר רצף חדש.
        // OrderByDescending — Priority גבוה קודם.
        // ThenBy — שובר שוויון: קודם לפי enum Source, אחר כך לפי מזהה משתתף (מיון יציב).
        // Take — לוקח רק את _candidateLimit הראשונים אחרי המיון.
        // ToList — materialize: מבצע את כל השאילתה ומחזיר List (ממומש כ-IReadOnlyList).
        // חשוב: מחזירים CandidateProposal מלא — לא .Select(p => p.Move) שמאבד מטא-דאטה.
        return unique
            .OrderByDescending(proposal => proposal.Priority)
            .ThenBy(proposal => proposal.Source)
            .ThenBy(proposal => proposal.Move.FirstParticipantId.Value, StringComparer.Ordinal)
            .Take(_candidateLimit)
            .ToList();
    }
}
