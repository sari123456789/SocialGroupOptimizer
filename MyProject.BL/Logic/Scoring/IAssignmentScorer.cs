using System.Collections.Generic;
using MyProject.BL.Logic.Configuration;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;
namespace MyProject.BL.Logic.Scoring;
/// <summary>
/// חוזה לחישוב ניקוד איכות עבור הקצאה שכבר עברה אימות אילוצים קשיחים.
/// </summary>
/// <remarks>
/// <para>תפקיד: הפרדה בין אימות חוקיות (<see cref="Core.Domain.Services.IAssignmentValidator"/>) לבין הערכת איכות (העדפות חברתיות, בידוד וכו').</para>
/// <para>נקרא מ-: עדיין לא בשימוש מחוץ ל-<see cref="ScoringManager"/> — תפקיד עתידי: חיפוש מקומי (LocalSearch), השוואת מועמדים, ורישום DI ב-API.</para>
/// </remarks>
public interface IAssignmentScorer
{
    /// <summary>
    /// מחשב ציון כולל להקצאה על בסיס משקלות ניתנים לכוונון.
    /// </summary>
    /// <param name="assignment">החלוקה לניקוד (מניחה שהאילוצים כבר אומתו).</param>
    /// <param name="participants">כל המשתתפים והעדפותיהם — מקור לניתוח חיבורים חברתיים.</param>
    /// <param name="weights">משקלות לרכיבי הניקוד (חברתי, בידוד, סיווג).</param>
    /// <returns>ציון מספרי עטוף ב-<see cref="Score"/>.</returns>
    /// <remarks>
    /// <para>נקרא מ-: מימוש ב-<see cref="ScoringManager.CalculateScore"/>; צרכנים עתידיים יזריקו <see cref="IAssignmentScorer"/>.</para>
    /// </remarks>
    Score CalculateScore(
        Assignment assignment,
        IReadOnlyList<Participant> participants,
        ScoringWeights weights);
}
