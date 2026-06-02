namespace MyProject.BL.Algorithm.InitialPlacement.Results;

/// <summary>
/// תפקיד: ממיר תוצאות InitialPlacement להודעות מותאמות למשתמש — מסנן רעש תשתיתי ומוסיף סיכום.
/// </summary>
/// <remarks>נקרא מ- AssignmentInitialPlacementValidator, <see cref="InitialPlacementOrchestrator"/>.</remarks>
public static class InitialPlacementUserMessages
{
    /// <summary>
    /// תפקיד: הודעת סיכום קבועה כשלא נמצאה חלוקה.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="MergeInfeasibleSummary"/>.</remarks>
    public const string InfeasibleAssignment = "לא נמצאה חלוקה שעומדת בכל האילוצים.";

    /// <summary>
    /// תפקיד: מחזיר הודעות להצגה למשתמש — ריק בהצלחה; מסונן ומתורגם בכישלון.
    /// </summary>
    /// <param name="result">תוצאת ההצבה הראשונית.</param>
    /// <returns>הודעות להצגה; ריקה בהצלחה.</returns>
    /// <remarks>נקרא מ- AssignmentInitialPlacementValidator.</remarks>
    public static IReadOnlyList<string> ForDisplay(InitialPlacementResult result)
    {
        // הצלחה — אין הודעות להצגה.
        if (result.Status is InitialPlacementStatus.Success or InitialPlacementStatus.SuccessViaSolver)
        {
            return Array.Empty<string>();
        }

        // כשל infeasible/תשתית — מוסיפים סיכום + פרטים מתורגמים.
        if (ShouldShowInfeasibleSummary(result))
        {
            return MergeInfeasibleSummary(result.Errors);
        }

        return result.Errors;
    }

    /// <summary>
    /// תפקיד: בודק האם השגיאות מצביעות על כשל תשתית (תקשורת/timeout) ולא על infeasibility.
    /// </summary>
    /// <param name="errors">רשימת שגיאות.</param>
    /// <returns>true אם יש שגיאת תשתית.</returns>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator"/> — להחלטה על הצגת סיכום infeasible.</remarks>
    public static bool IsSolverInfrastructureFailure(IReadOnlyList<string> errors) =>
        errors.Any(IsInfrastructureError);

    /// <summary>
    /// תפקיד: בונה רשימת הודעות עם סיכום infeasible ופרטים מתורגמים.
    /// </summary>
    /// <param name="detailErrors">שגיאות מפורטות; null מומר לריק.</param>
    /// <returns>סיכום + פרטים + רמז אם אין פרטים.</returns>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator.TrySolverOrReportInfeasible"/>.</remarks>
    public static IReadOnlyList<string> InfeasibleWithDetails(IReadOnlyList<string>? detailErrors) =>
        MergeInfeasibleSummary(detailErrors ?? Array.Empty<string>());

    private static bool ShouldShowInfeasibleSummary(InitialPlacementResult result) =>
        // סטטוסים שמצביעים על כשל חלוקה (לא הצלחה).
        result.Status is InitialPlacementStatus.InfeasiblePreCheck
            or InitialPlacementStatus.BuildFailed
            or InitialPlacementStatus.RepairFailed
            or InitialPlacementStatus.SolverFailed
            or InitialPlacementStatus.UnknownTimeout
        && (result.Status is InitialPlacementStatus.InfeasiblePreCheck
            or InitialPlacementStatus.BuildFailed
            or InitialPlacementStatus.RepairFailed
            // או כשל פותר שמזוהה כ-infeasible/תשתית ולא שגיאת API גולמית.
            || IsSolverInfrastructureFailure(result.Errors)
            || IsSolverInfeasible(result.Errors));

    private static bool IsSolverInfeasible(IReadOnlyList<string> errors) =>
        errors.Any(error =>
            error.Contains("infeasible", StringComparison.OrdinalIgnoreCase)
            || error.Contains("לא ניתן", StringComparison.Ordinal));

    private static IReadOnlyList<string> MergeInfeasibleSummary(IReadOnlyList<string> detailErrors)
    {
        var messages = new List<string> { InfeasibleAssignment };

        foreach (var error in detailErrors)
        {
            var translated = InfeasibilityExplanationBuilder.TranslatePublic(error);
            // מסננים רעש תשתיתי ושגיאות פנימיות שלא רלוונטיות למשתמש.
            if (ShouldIncludeDetail(translated))
            {
                messages.Add(translated);
            }
        }

        // אם נשאר רק הסיכום — מוסיפים רמז כללי.
        if (messages.Count == 1)
        {
            messages.Add(
                "לא אותרה סיבה מפורטת נוספת. נסו להסיר אילוצים, לבדוק זוגות חובה/איסור, או לשנות את אילוצי האיזון.");
        }

        return messages;
    }

    private static bool ShouldIncludeDetail(string error) =>
        !string.IsNullOrWhiteSpace(error)
        && !string.Equals(error, InfeasibleAssignment, StringComparison.Ordinal)
        && !IsInfrastructureError(error)
        && !error.Contains("No legal unit-level move", StringComparison.OrdinalIgnoreCase)
        && !error.Contains("External solver", StringComparison.OrdinalIgnoreCase);

    private static bool IsInfrastructureError(string error) =>
        error.Contains("communication failed", StringComparison.OrdinalIgnoreCase)
        || error.Contains("actively refused", StringComparison.OrdinalIgnoreCase)
        || error.Contains("No connection could be made", StringComparison.OrdinalIgnoreCase)
        || error.Contains("timed out before job submission", StringComparison.OrdinalIgnoreCase)
        || error.Contains("polling timed out", StringComparison.OrdinalIgnoreCase)
        || error.Contains("polling deadline exceeded", StringComparison.OrdinalIgnoreCase);
}
