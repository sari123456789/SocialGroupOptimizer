using System.Collections.Generic;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Transparency;

/// <summary>
/// הערכת חלופת החלפה — מה היה קורה אילו המשתתף הוחלף עם משתתף מקבוצה אחרת.
/// </summary>
/// <remarks>
/// הערכה תיאורטית בלבד (Read Only). החוקיות נבדקת דרך <see cref="ManualMoves.IManualMoveEvaluator"/>.
/// </remarks>
public sealed class AlternativeSwapEvaluation
{
    public AlternativeSwapEvaluation(
        GroupId targetGroupId,
        ParticipantId swapWithParticipantId,
        bool isLegal,
        bool requiresOverride,
        double estimatedParticipantScoreDelta,
        double estimatedAssignmentScoreDelta,
        IReadOnlyList<ExplanationReason> reasons)
    {
        TargetGroupId = targetGroupId;
        SwapWithParticipantId = swapWithParticipantId;
        IsLegal = isLegal;
        RequiresOverride = requiresOverride;
        EstimatedParticipantScoreDelta = estimatedParticipantScoreDelta;
        EstimatedAssignmentScoreDelta = estimatedAssignmentScoreDelta;
        Reasons = reasons ?? new List<ExplanationReason>();
    }

    /// <summary>קבוצת היעד של המשתתף לאחר ההחלפה.</summary>
    public GroupId TargetGroupId { get; }

    /// <summary>המשתתף שאיתו מתבצעת ההחלפה.</summary>
    public ParticipantId SwapWithParticipantId { get; }

    /// <summary>האם ההחלפה חוקית לחלוטין (ללא הפרות).</summary>
    public bool IsLegal { get; }

    /// <summary>האם ההחלפה אפשרית רק עם אישור חריגה על אילוצים עסקיים.</summary>
    public bool RequiresOverride { get; }

    /// <summary>שינוי ציון מקומי משוער של המשתתף (חיובי = שיפור).</summary>
    public double EstimatedParticipantScoreDelta { get; }

    /// <summary>שינוי ציון משוער של כל החלוקה (חיובי = שיפור).</summary>
    public double EstimatedAssignmentScoreDelta { get; }

    /// <summary>סיבות להערכת ההחלפה.</summary>
    public IReadOnlyList<ExplanationReason> Reasons { get; }
}
