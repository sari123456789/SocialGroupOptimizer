using System.Collections.Generic;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;

namespace MyProject.Core.Domain.Services;

/// <summary>
/// חוזה לחישוב ניקוד עבור הקצאה.
/// </summary>
public interface IScoreCalculator
{
    /// <summary>
    /// מחשב ניקוד עבור ההקצאה הנתונה.
    /// </summary>
    /// <param name="assignment">ההקצאה לחישוב ניקוד.</param>
    /// <param name="constraints">אילוצים רלוונטיים לחישוב הניקוד.</param>
    /// <returns>הניקוד המחושב.</returns>
    decimal CalculateScore(Assignment assignment, IReadOnlyList<IConstraint> constraints);
}
