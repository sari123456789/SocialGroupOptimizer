using MyProject.Core.Domain.Services;
using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.BL.Algorithm.LocalSearch.State;
using MyProject.BL.Algorithm.SolutionState;
using MyProject.BL.Logic.Scoring;

namespace MyProject.BL.Algorithm.LocalSearch.Evaluation;

/// <summary>
/// מעריך מהלך — clone, אימות, ניקוד; לא מבצע move על המצב הראשי.
/// </summary>
/// <remarks>
/// <para>תפקיד: הערכת CandidateProposal בודד; לא בוחר move ולא מעדכן RuntimeStateManager.</para>
/// <para>נקרא מ-: MoveEvaluationBatch (לולאה), מתזמר עתידי.</para>
/// <para>אסור: constraint.IsSatisfied ישיר, חישוב ציון ידני, ApplyMoveInPlace על currentState.</para>
/// </remarks>
public sealed class MoveEvaluator : IMoveEvaluator
{
    // תלויות דרך ממשקים — לא תלויים ב-ConstraintEngine/ScoringManager ישירות (למען בדיקות ו-DI).
    private readonly IAssignmentValidator _validator;
    private readonly IAssignmentScorer _scorer;

    /// <summary>
    /// יוצר מעריך עם תלויות אימות וניקוד.
    /// </summary>
    /// <param name="validator">מנוע אילוצים — בדרך כלל ConstraintEngine.</param>
    /// <param name="scorer">מנוע ניקוד — בדרך כלל ScoringManager.</param>
    public MoveEvaluator(IAssignmentValidator validator, IAssignmentScorer scorer)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _scorer = scorer ?? throw new ArgumentNullException(nameof(scorer));
    }

    /// <inheritdoc />
    public MoveEvaluationResult Evaluate(
        AssignmentState currentState,
        CandidateProposal proposal,
        MoveEvaluationContext context)
    {
        if (currentState is null)
        {
            throw new ArgumentNullException(nameof(currentState));
        }

        if (proposal is null)
        {
            throw new ArgumentNullException(nameof(proposal));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // הציון "לפני" נלקח מהמצב הפעיל — AssignmentStateUpdater לא מעדכן CurrentScore אחרי move.
        var scoreBefore = currentState.CurrentScore;

        // ApplyToClone: שכפול עמוק + יישום move על העותק בלבד.
        // proposal.Move — ה-MoveCandidate הפנימי (Swap/Transfer); המטא-דאטה (Source, Priority) נשאר ב-proposal.
        var resultingState = AssignmentStateUpdater.ApplyToClone(currentState, proposal.Move);

        // המרה ל-Assignment ב-Core — גם ה-validator וגם ה-scorer עובדים על ישות Assignment.
        var resultingAssignment = AssignmentStateConverter.ToAssignment(resultingState);

        // out var errors — אם IsValid מחזיר false, errors מכילה רשימת הודעות בעברית/אנגלית מהמאמתים.
        if (!_validator.IsValid(resultingAssignment, context.Constraints, out var errors))
        {
            // נתיב קצר: אין חישוב ציון למהלך לא חוקי (חוסך זמן ומונע שימוש בציון שגוי).
            return MoveEvaluationResult.Invalid(proposal, scoreBefore, errors);
        }

        // רק כאן קוראים ל-scorer — אחרי שאימות עבר בהצלחה.
        var scoreAfter = _scorer.CalculateScore(
            resultingAssignment,
            context.Participants,
            context.ScoringWeights);

        // ?. — null-conditional: אם VisitedTracker הוא null, הביטוי כולו מחזיר null ואז ?? false.
        // HasSeen בודק hash של המצב אחרי המהלך — מידע ל-SearchStrategy, לא פוסל כאן.
        var wasAlreadyVisited = context.VisitedTracker?.HasSeen(resultingState.CurrentHash) ?? false;

        // Factory Valid מחשב ScoreDelta ו-IsImproving בתוך MoveEvaluationResult.
        return MoveEvaluationResult.Valid(
            proposal,
            scoreBefore,
            scoreAfter,
            resultingState,
            resultingAssignment,
            wasAlreadyVisited);
    }
}
