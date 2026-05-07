using System.Collections.Generic;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;

namespace MyProject.Core.Domain.Services;

/// <summary>
/// חוזה לאימות פתרונות הקצאה.
/// </summary>
public interface IAssignmentValidator
{
    /// <summary>
    /// מאמת את ההקצאה מול אילוצים נתונים.
    /// </summary>
    /// <param name="assignment">ההקצאה לאימות.</param>
    /// <param name="constraints">אילוצים שחייבים להתקיים.</param>
    /// <param name="errors">הודעות שגיאה כאשר האימות נכשל.</param>
    /// <returns><see langword="true"/> אם ההקצאה חוקית; אחרת <see langword="false"/>.</returns>
    bool IsValid(Assignment assignment, IReadOnlyList<IConstraint> constraints, out IReadOnlyList<string> errors);
}
