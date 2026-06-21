using System.Collections.Generic;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;

namespace MyProject.BL.ManualMoves;

/// <summary>
/// חוזה להערכת מהלך ידני (Swap/Transfer) על חלוקה קיימת.
/// </summary>
/// <remarks>
/// Read Only: אינו מריץ חלוקה מחדש, אינו מפעיל LocalSearch/Solver, ואינו שומר דבר.
/// מבחין בין שגיאות תקינות מערכת (חוסמות) לבין אילוצים עסקיים (ניתנים לחריגה).
/// </remarks>
public interface IManualMoveEvaluator
{
    /// <summary>
    /// מעריך מהלך ידני ומחזיר חוקיות, אילוצים שנשברו וציונים.
    /// </summary>
    /// <param name="current">החלוקה הנוכחית — אינה משתנה.</param>
    /// <param name="move">המהלך הידני להערכה.</param>
    /// <param name="participants">כל המשתתפים והעדפותיהם.</param>
    /// <param name="constraints">אילוצי הדומיין של החלוקה.</param>
    ManualMoveEvaluation Evaluate(
        Assignment current,
        ManualMove move,
        IReadOnlyList<Participant> participants,
        IReadOnlyList<IConstraint> constraints);
}
