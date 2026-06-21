using MyProject.Core.Domain.Enums;

namespace MyProject.BL.ManualMoves;

/// <summary>
/// אילוץ עסקי שנשבר כתוצאה ממהלך ידני.
/// </summary>
/// <remarks>
/// אילוצים עסקיים ניתנים לאישור חריגה (override). אילוצי תקינות מערכת אינם נכללים
/// כאן אלא ב-<c>BlockingErrors</c> של <see cref="ManualMoveEvaluation"/>.
/// </remarks>
public sealed class BrokenConstraint
{
    /// <summary>
    /// חומרת ברירת מחדל לאילוץ עסקי שנשבר.
    /// </summary>
    public const string BusinessSeverity = "Business";

    public BrokenConstraint(ConstraintType constraintType, string message)
    {
        ConstraintType = constraintType;
        Message = message ?? string.Empty;
    }

    /// <summary>
    /// סוג האילוץ שנשבר.
    /// </summary>
    public ConstraintType ConstraintType { get; }

    /// <summary>
    /// הודעה מילולית (עברית) המתארת את ההפרה.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// חומרת ההפרה — אילוצים עסקיים בלבד.
    /// </summary>
    public string Severity => BusinessSeverity;

    /// <summary>
    /// האם ניתן לאשר חריגה על אילוץ זה — תמיד אמת עבור אילוצים עסקיים.
    /// </summary>
    public bool CanOverride => true;
}
