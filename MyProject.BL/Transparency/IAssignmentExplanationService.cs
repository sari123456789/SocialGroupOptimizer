using System.Collections.Generic;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Transparency;

/// <summary>
/// חוזה לשכבת שקיפות (Explainable Assignment) — מסביר שיבוץ קיים של משתתף.
/// </summary>
/// <remarks>
/// השכבה היא Read Only בלבד: היא אינה מריצה חלוקה מחדש, אינה יוצרת מועמדים,
/// אינה משתמשת ב-LocalSearch או ב-Solver, ואינה משנה או שומרת מצב כלשהו.
/// </remarks>
public interface IAssignmentExplanationService
{
    /// <summary>
    /// מסביר מדוע משתתף שובץ לקבוצתו ומעריך חלופות אפשריות.
    /// </summary>
    /// <param name="assignment">החלוקה הקיימת (מצב שמור) — אינה משתנה.</param>
    /// <param name="participantId">מזהה המשתתף להסבר.</param>
    /// <param name="participants">כל המשתתפים והעדפותיהם החברתיות.</param>
    /// <param name="constraints">אילוצי הדומיין של החלוקה.</param>
    /// <returns>הסבר שיבוץ מלא עבור המשתתף.</returns>
    ParticipantPlacementExplanation ExplainParticipantPlacement(
        Assignment assignment,
        ParticipantId participantId,
        IReadOnlyList<Participant> participants,
        IReadOnlyList<IConstraint> constraints);
}
