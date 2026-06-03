using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.BL.Algorithm.SolutionState;
using MyProject.BL.Logic.Configuration;

namespace MyProject.BL.Algorithm.LocalSearch.Evaluation;

/// <summary>
/// הערכת אוסף מהלכים — מריץ IMoveEvaluator על כל CandidateProposal.
/// </summary>
/// <remarks>
/// <para>תפקיד: לולאת הערכה בלבד; לא בוחר move, לא מבצע move, לא מסנן תוצאות.</para>
/// <para>נקרא מ-: מתזמר Local Search עתידי, אחרי SwapMoveGenerator.</para>
/// <para>הפרדה: SearchStrategy יסנן ויבחר; MoveExecutor יבצע על המצב הראשי.</para>
/// </remarks>
public sealed class MoveEvaluationBatch : IMoveEvaluationBatch
{
    // sealed — אין ירושה; המחלקה מיועדת לשימוש ישיר בלבד.
    // readonly — הערכים נקבעים בבנאי ולא משתנים (immutable behavior).

    private readonly IMoveEvaluator _evaluator;
    private readonly int _maxEvaluations;

    /// <summary>
    /// יוצר batch עם מעריך בודד והגדרות אלגוריתם למגבלה הגנתית.
    /// </summary>
    /// <param name="evaluator">מעריך מהלך בודד — מוזרק מ-DI.</param>
    /// <param name="settings">הגדרות — CandidateCount כמגבלת הערכות.</param>
    public MoveEvaluationBatch(IMoveEvaluator evaluator, AlgorithmSettings settings)
    {
        // ?? throw — אם evaluator הוא null, זורקים מיד ArgumentNullException (fail-fast).
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));

        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        // תנאי תלתי: אם CandidateCount חיובי — משתמשים בו; אחרת int.MaxValue = "בלי מגבלה פרקטית".
        // זה מגן ממקרה שבו ההגדרה שגויה, בלי לזרוק חריגה על כל האיטרציה.
        _maxEvaluations = settings.CandidateCount > 0
            ? settings.CandidateCount
            : int.MaxValue;
    }

    /// <inheritdoc />
    public IReadOnlyList<MoveEvaluationResult> EvaluateAll(
        AssignmentState currentState,
        IReadOnlyList<CandidateProposal> proposals,
        MoveEvaluationContext context)
    {
        // בדיקות null — מונעות NullReferenceException עמוק בתוך MoveEvaluator.
        if (currentState is null)
        {
            throw new ArgumentNullException(nameof(currentState));
        }

        if (proposals is null)
        {
            throw new ArgumentNullException(nameof(proposals));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // proposals.Count == 0 — אין מה להעריך; מחזירים מערך ריק (לא null).
        // Array.Empty<T>() — מערך ריק משותף בזיכרון, ללא הקצאה חדשה.
        if (proposals.Count == 0)
        {
            return Array.Empty<MoveEvaluationResult>();
        }

        // List עם קיבולת מוקדמת — פחות הקצאות זיכרון בזמן Add.
        // Math.Min — לכל היותר כמות התוצאות שבאמת נוסיף (לא יותר מ-_maxEvaluations).
        var results = new List<MoveEvaluationResult>(Math.Min(proposals.Count, _maxEvaluations));
        var evaluatedCount = 0;

        // foreach על IReadOnlyList — עובר לפי סדר האינדקסים 0..Count-1 (סדר הקלט נשמר).
        foreach (var proposal in proposals)
        {
            // מגבלה הגנתית: אם כבר הערכנו מספיק, עוצרים (break יוצא מהלולאה).
            // SwapMoveGenerator כבר מגביל, אבל כאן שכבת הגנה שנייה.
            if (evaluatedCount >= _maxEvaluations)
            {
                break;
            }

            // כל קריאה ל-Evaluate יוצרת clone נפרד — המצב הראשי currentState לא משתנה.
            // אין סינון: גם תוצאה לא חוקית (IsValid=false) נכנסת לרשימה.
            results.Add(_evaluator.Evaluate(currentState, proposal, context));
            evaluatedCount++;
        }

        // IReadOnlyList — החוזה מחזיר ממשק לקריאה בלבד; List ממומש את הממשק אוטומטית.
        return results;
    }
}
