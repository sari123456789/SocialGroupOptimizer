using System.Collections.Generic;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;

namespace MyProject.BL.Logic.Constraints;

/// <summary>
/// מאמת שבכל קבוצה מספר המשתתפים מרמה נתונה במימד סיווג נמצא בטווח המותר.
/// </summary>
/// <remarks>נקרא מ-: <see cref="ConstraintEngine"/> בלבד.</remarks>
public sealed class ClassificationBalanceValidator
{
    /// <summary>
    /// בודק ClassificationBalanceConstraint ומחזיר הפרות.
    /// </summary>
    /// <param name="assignment">החלוקה לבדיקה.</param>
    /// <param name="constraints">אילוצי איזון סיווג (כבר מסוננים).</param>
    /// <returns>הודעות שגיאה; ריק אם כל הקבוצות עומדות בטווחי הספירה לרמה.</returns>
    public IReadOnlyList<string> Validate(
        Assignment assignment,
        IReadOnlyList<ClassificationBalanceConstraint> constraints)
    {
        var errors = new List<string>();

        foreach (var constraint in constraints)
        {
            // ! — שלילה: אם האילוץ לא מתקיים, מוסיפים הודעת שגיאה.
            // לעומת מאמתים אחרים — כאן אין continue; הלוגיקה הפוכה אך זהה בתוצאה.
            if (!constraint.IsSatisfied(assignment))
            {
                // TargetDimension / TargetLevel — מימד ורמה שנבדקים; MinCountPerGroup / MaxCountPerGroup — הטווח.
                errors.Add(
                    $"ClassificationBalance violated for dimension '{constraint.TargetDimension}' level '{constraint.TargetLevel}': " +
                    $"each group must contain between {constraint.MinCountPerGroup} and {constraint.MaxCountPerGroup} participant(s).");
            }
        }

        return errors;
    }
}
