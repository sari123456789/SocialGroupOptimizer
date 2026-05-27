using System.Collections.Generic;
using MyProject.BL.Logic.Configuration;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Logic.Scoring;

/// <summary>
/// חוזה לחישוב ניקוד איכות עבור הקצאה תקינה.
/// </summary>
/// <remarks>
/// שירות זה מניח שהאילוצים הקשיחים כבר אומתו על ידי ConstraintEngine.
/// תפקידו הוא להעריך את איכות ההקצאה בלבד.
/// </remarks>
public interface IAssignmentScorer
{
    /// <summary>
    /// מחשב ציון כולל להקצאה.
    /// </summary>
    /// <param name="assignment">ההקצאה לניקוד.</param>
    /// <param name="participants">כלל המשתתפים כולל העדפותיהם.</param>
    /// <param name="weights">משקלות לרכיבי הניקוד.</param>
    /// <returns>הציון הכולל של ההקצאה.</returns>
    Score CalculateScore(
        Assignment assignment,
        IReadOnlyList<Participant> participants,
        ScoringWeights weights);
}
