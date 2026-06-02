using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Services;

namespace MyProject.BL.Algorithm.InitialPlacement.Placement;

/// <summary>
/// תפקיד: עטיפה דקה לבדיקת חוקיות חלוקה ראשונית דרך <see cref="IAssignmentValidator"/>.
/// </summary>
/// <remarks>
/// כל לוגיקת הבדיקה נמצאת ב-<see cref="IAssignmentValidator"/>.
/// נוצר ע"י <see cref="InitialPlacementOrchestrator"/>.
/// </remarks>
public sealed class InitialFeasibilityDecider
{
    private readonly IAssignmentValidator _validator;

    /// <summary>
    /// תפקיד: מאתחל עם מאמת אילוצים.
    /// </summary>
    /// <param name="validator">מאמת האילוצים; לא null.</param>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator"/>.</remarks>
    public InitialFeasibilityDecider(IAssignmentValidator validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <summary>
    /// תפקיד: בודק האם הקבוצות מהוות חלוקה חוקית.
    /// </summary>
    /// <param name="groups">קבוצות שנבנו — רשימה לא ריקה.</param>
    /// <param name="constraints">אילוצי הדומיין.</param>
    /// <param name="errors">שגיאות אימות; ריקה אם חוקי.</param>
    /// <returns>true אם החלוקה חוקית.</returns>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator.Run"/> — אחרי Greedy ו-Repair.</remarks>
    public bool IsValid(
        IReadOnlyList<Group> groups,
        IReadOnlyList<IConstraint> constraints,
        out IReadOnlyList<string> errors)
    {
        if (groups is null)
        {
            throw new ArgumentNullException(nameof(groups));
        }

        // רשימה ריקה = אין חלוקה — מחזירים false בלי לזרוק חריגה (מצב עסקי).
        if (groups.Count == 0)
        {
            errors = new[] { "No groups provided." };
            return false;
        }

        if (constraints is null)
        {
            throw new ArgumentNullException(nameof(constraints));
        }

        // עטיפה ל-Assignment — IAssignmentValidator עובד על ישות Core, לא על List<Group> גולמי.
        var assignment = new Assignment(groups);

        // out errors — המאמת ממלא רשימת הפרות; true = כל האילוצים מתקיימים.
        return _validator.IsValid(assignment, constraints, out errors);
    }
}
