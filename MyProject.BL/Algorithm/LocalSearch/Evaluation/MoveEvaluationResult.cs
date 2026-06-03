using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;
using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.BL.Algorithm.SolutionState;

namespace MyProject.BL.Algorithm.LocalSearch.Evaluation;

/// <summary>
/// תוצאת הערכת מהלך בודד — חוקיות, ציון והפרש.
/// </summary>
/// <remarks>
/// <para>תפקיד: DTO immutable שמחזיר MoveEvaluator אחרי ניסוי move על clone.</para>
/// <para>נקרא מ-: MoveEvaluationBatch, SearchStrategy עתידי (בחירת move).</para>
/// <para>בנאי פרטי + factories — יצירה רק דרך Invalid/Valid לשמירת עקביות שדות.</para>
/// </remarks>
public sealed class MoveEvaluationResult
{
    // בנאי פרטי — מחוץ למחלקה אי אפשר new MoveEvaluationResult(...) ישירות.
    private MoveEvaluationResult(
        CandidateProposal proposal,
        bool isValid,
        bool isImproving,
        Score scoreBefore,
        Score? scoreAfter,
        double scoreDelta,
        IReadOnlyList<string> constraintErrors,
        AssignmentState? resultingState,
        Assignment? resultingAssignment,
        AssignmentHash? resultingHash,
        bool wasAlreadyVisited)
    {
        Proposal = proposal ?? throw new ArgumentNullException(nameof(proposal));
        IsValid = isValid;
        IsImproving = isImproving;
        ScoreBefore = scoreBefore;
        ScoreAfter = scoreAfter;
        ScoreDelta = scoreDelta;
        // ?? Array.Empty — אם null הועבר, מנרמלים לרשימה ריקה (לא null בחוץ).
        ConstraintErrors = constraintErrors ?? Array.Empty<string>();
        ResultingState = resultingState;
        ResultingAssignment = resultingAssignment;
        ResultingHash = resultingHash;
        WasAlreadyVisited = wasAlreadyVisited;
    }

    /// <summary>ההצעה המקורית שנבדקה — כולל Source, Reason, Priority.</summary>
    public CandidateProposal Proposal { get; }

    /// <summary>האם המהלך עומד בכל האילוצים.</summary>
    public bool IsValid { get; }

    /// <summary>האם הציון אחרי המהלך גבוה מהציון לפני — רלוונטי רק כש-IsValid.</summary>
    public bool IsImproving { get; }

    /// <summary>ציון מצב החלוקה הנוכחי (לפני המהלך).</summary>
    public Score ScoreBefore { get; }

    /// <summary>ציון אחרי המהלך — null אם המהלך לא חוקי (Score? = nullable).</summary>
    public Score? ScoreAfter { get; }

    /// <summary>הפרש ציון (אחרי פחות לפני); 0 אם המהלך לא חוקי.</summary>
    public double ScoreDelta { get; }

    /// <summary>הודעות שגיאה מאימות האילוצים — ריקה אם חוקי.</summary>
    public IReadOnlyList<string> ConstraintErrors { get; }

    /// <summary>מצב החלוקה אחרי המהלך (clone) — null אם לא חוקי; לשימוש עתידי ב-Executor.</summary>
    public AssignmentState? ResultingState { get; }

    /// <summary>חלוקה ב-Core אחרי המהלך — null אם לא חוקי.</summary>
    public Assignment? ResultingAssignment { get; }

    /// <summary>Hash של המצב אחרי המהלך — null אם לא חוקי.</summary>
    public AssignmentHash? ResultingHash { get; }

    /// <summary>האם hash המצב החדש כבר נבדק בעבר — מידע בלבד; SearchStrategy יחליט אם לדלג.</summary>
    public bool WasAlreadyVisited { get; }

    /// <summary>
    /// יוצר תוצאה למהלך לא חוקי — ללא חישוב ציון.
    /// </summary>
    public static MoveEvaluationResult Invalid(
        CandidateProposal proposal,
        Score scoreBefore,
        IReadOnlyList<string> constraintErrors)
    {
        return new MoveEvaluationResult(
            proposal,
            isValid: false,
            isImproving: false,
            scoreBefore,
            scoreAfter: null,
            scoreDelta: 0,
            constraintErrors,
            resultingState: null,
            resultingAssignment: null,
            resultingHash: null,
            wasAlreadyVisited: false);
    }

    /// <summary>
    /// יוצר תוצאה למהלך חוקי — כולל ציון, מצב ו-hash.
    /// </summary>
    public static MoveEvaluationResult Valid(
        CandidateProposal proposal,
        Score scoreBefore,
        Score scoreAfter,
        AssignmentState resultingState,
        Assignment resultingAssignment,
        bool wasAlreadyVisited)
    {
        // Score הוא record struct — .Value הוא double הפנימי.
        var delta = scoreAfter.Value - scoreBefore.Value;
        // שיפור = הפרש חיובי (ציון גבוה יותר טוב במערכת שלנו).
        var isImproving = delta > 0;

        return new MoveEvaluationResult(
            proposal,
            isValid: true,
            isImproving,
            scoreBefore,
            scoreAfter,
            scoreDelta: delta,
            constraintErrors: Array.Empty<string>(),
            resultingState,
            resultingAssignment,
            // hash כבר עודכן incrementally ב-ApplyToClone על resultingState.
            resultingHash: resultingState.CurrentHash,
            wasAlreadyVisited);
    }
}
