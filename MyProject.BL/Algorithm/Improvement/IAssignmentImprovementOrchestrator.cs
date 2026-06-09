namespace MyProject.BL.Algorithm.Improvement;

/// <summary>
/// חוזה לשיפור חלוקה ראשונית חוקית באמצעות Local Search.
/// </summary>
public interface IAssignmentImprovementOrchestrator
{
    /// <summary>
    /// משפר חלוקה ראשונית — או מחזיר אותה אם Local Search כבוי.
    /// </summary>
    /// <param name="input">קלט — חלוקה, משתתפים, אילוצים ומשקלות.</param>
    /// <returns>תוצאת שיפור עם חלוקה סופית.</returns>
    AssignmentImprovementResult Improve(AssignmentImprovementInput input);
}
