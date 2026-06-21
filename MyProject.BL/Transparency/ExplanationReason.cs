namespace MyProject.BL.Transparency;

/// <summary>
/// סיבה בודדת בהסבר שיבוץ — קוד אחיד, הודעה לקריאה ומשקל מספרי.
/// </summary>
/// <remarks>
/// אובייקט קריאה בלבד; נבנה על ידי <see cref="AssignmentExplanationService"/>.
/// </remarks>
public sealed class ExplanationReason
{
    /// <summary>
    /// מאתחל סיבה חדשה.
    /// </summary>
    /// <param name="code">קוד הסיבה האחיד.</param>
    /// <param name="message">הודעה מילולית (עברית) להצגה למנהל.</param>
    /// <param name="weight">משקל/עוצמה מספרית של הסיבה (למשל מספר התאמות או דלתת ציון).</param>
    public ExplanationReason(ExplanationReasonCode code, string message, double weight)
    {
        Code = code;
        Message = message ?? string.Empty;
        Weight = weight;
    }

    /// <summary>
    /// קוד הסיבה האחיד.
    /// </summary>
    public ExplanationReasonCode Code { get; }

    /// <summary>
    /// הודעה מילולית להצגה.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// משקל/עוצמה מספרית של הסיבה.
    /// </summary>
    public double Weight { get; }
}
