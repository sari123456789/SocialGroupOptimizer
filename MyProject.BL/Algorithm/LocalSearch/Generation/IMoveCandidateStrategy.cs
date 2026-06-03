using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.BL.Algorithm.LocalSearch.RuntimeData;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.LocalSearch.Generation;

/// <summary>
/// חוזה לאסטרטגיית יצירת מועמדים.
/// </summary>
/// <remarks>
/// <para>תפקיד: כל מימוש מקבל מצב, snapshot והקשר ומחזיר הצעות move — בלי הערכה, אימות או ביצוע.</para>
/// <para>נקרא מ-: <see cref="SwapMoveGenerator"/> (הפעלה), <see cref="MoveGenerationPolicy"/> (בחירה).</para>
/// <para>אסור: קריאות ל-IAssignmentScorer, IAssignmentValidator או constraint.IsSatisfied.</para>
/// <para>כל מימוש: sealed class עם Source קבוע; Generate מחזיר Array.Empty על null — לא זורק.</para>
/// </remarks>
public interface IMoveCandidateStrategy
{
    /// <summary>
    /// מקור האסטרטגיה — property ללא set; ממופה ל-CandidateSource ב-MoveGenerationPolicy.
    /// </summary>
    CandidateSource Source { get; }

    /// <summary>
    /// בודק האם האסטרטגיה רלוונטית למצב הנוכחי — בלי לייצר מועמדים.
    /// </summary>
    /// <param name="snapshot">נתוני הריצה הנוכחיים.</param>
    /// <param name="context">מצב החיפוש הנוכחי.</param>
    /// <returns>true אם כדאי להפעיל את האסטרטגיה; אחרת false.</returns>
    bool IsApplicable(RuntimeDataSnapshot snapshot, MoveGenerationContext context);

    /// <summary>
    /// מייצר הצעות move מהמצב הנוכחי.
    /// </summary>
    /// <param name="state">מצב החלוקה הנוכחי — לקריאה בלבד.</param>
    /// <param name="snapshot">נתוני הריצה (פרופילים ואינדקסים).</param>
    /// <param name="context">מצב החיפוש הנוכחי.</param>
    /// <returns>רשימת הצעות; ריקה אם אין מועמדים או חסר נתון — לעולם לא זורק על חוסר נתונים.</returns>
    IReadOnlyList<CandidateProposal> Generate(
        AssignmentState state,
        RuntimeDataSnapshot snapshot,
        MoveGenerationContext context);
}
