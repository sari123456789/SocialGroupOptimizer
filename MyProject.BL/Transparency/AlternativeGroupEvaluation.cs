using System.Collections.Generic;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Transparency;

/// <summary>
/// הערכת חלופה — מה היה קורה אילו המשתתף היה עובר לקבוצה זו.
/// </summary>
/// <remarks>
/// </remarks>
public sealed class AlternativeGroupEvaluation
{
    /// <summary>
    /// מאתחל הערכת חלופה חדשה.
    /// </summary>
    /// <param name="groupId">מזהה הקבוצה החלופית.</param>
    /// <param name="isLegal">האם מעבר לקבוצה זו חוקי .</param>
    /// <param name="estimatedScoreDelta">שינוי ציון משוער של המשתתף לעומת הקבוצה הנוכחית.</param>
    /// <param name="reasons">סיבות המסבירות את החוקיות/השפעת הציון.</param>
    public AlternativeGroupEvaluation(
        GroupId groupId,
        bool isLegal,
        bool requiresOverride,
        double estimatedParticipantScoreDelta,
        double estimatedAssignmentScoreDelta,
        IReadOnlyList<ExplanationReason> reasons)
    {
        GroupId = groupId;
        IsLegal = isLegal;
        RequiresOverride = requiresOverride;
        EstimatedParticipantScoreDelta = estimatedParticipantScoreDelta;
        EstimatedAssignmentScoreDelta = estimatedAssignmentScoreDelta;
        Reasons = reasons ?? new List<ExplanationReason>();
    }

    /// <summary>
    /// מזהה הקבוצה החלופית.
    /// </summary>
    public GroupId GroupId { get; }

    /// <summary>
    /// האם מעבר לקבוצה זו חוקי.
    /// </summary>
    public bool IsLegal { get; }

    /// <summary>
    /// האם המעבר אפשרי רק עם אישור חריגה על אילוצים עסקיים.
    /// </summary>
    public bool RequiresOverride { get; }

    /// <summary>
    /// שינוי ציון משוער של המשתתף (חיובי = שיפור, שלילי = ירידה).
    /// </summary>
    public double EstimatedParticipantScoreDelta { get; }

    /// <summary>
    /// שינוי ציון משוער של כל החלוקה (חיובי = שיפור, שלילי = ירידה).
    /// </summary>
    public double EstimatedAssignmentScoreDelta { get; }

    /// <summary>
    /// סיבות להערכת החלופה.
    /// </summary>
    public IReadOnlyList<ExplanationReason> Reasons { get; }
}
