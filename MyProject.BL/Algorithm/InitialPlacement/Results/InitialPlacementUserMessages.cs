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
            if (result.Errors.Any(error => string.Equals(error, InfeasibleAssignment, StringComparison.Ordinal)))
            {
                return NormalizeDisplayMessages(result.Errors);
            }

            return MergeInfeasibleSummary(result.Errors);
        }

        return NormalizeDisplayMessages(result.Errors);
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

    /// <summary>
    /// תפקיד: בודק האם יש להציג סיכום Infeasible למשתמש — סטטוס כשל חלוקה או שגיאות פותר שמצביעות על infeasible.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
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

    /// <summary>
    /// תפקיד: מזהה אם השגיאות מצביעות על כשל פותר infeasible ולא על שגיאת תשתית.
    /// </summary>
    /// <param name="errors"></param>
    /// <returns></returns>
    private static bool IsSolverInfeasible(IReadOnlyList<string> errors) =>
        errors.Any(error =>
            error.Contains("infeasible", StringComparison.OrdinalIgnoreCase)
            || error.Contains("לא ניתן", StringComparison.Ordinal));

    /// <summary>
    /// תפקיד: בונה רשימת הודעות להצגה למשתמש — מוסיף סיכום Infeasible + פרטים מתורגמים + רמז אם אין פרטים.
    /// </summary>
    /// <param name="detailErrors"></param>
    /// <returns></returns>
    private static IReadOnlyList<string> MergeInfeasibleSummary(IReadOnlyList<string> detailErrors)
    {
        var messages = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        TryAddCanonical(messages, seen, InfeasibleAssignment, includeSummary: true);

        foreach (var error in detailErrors)
        {
            TryAddCanonical(messages, seen, error);
        }

        // אם נשאר רק הסיכום — מוסיפים רמז כללי.
        if (messages.Count == 1)
        {
            messages.Add(
                "לא אותרה סיבה מפורטת נוספת. נסו להסיר אילוצים, לבדוק זוגות חובה/איסור, או לשנות את אילוצי האיזון.");
        }

        return RemoveEnglishWhenHebrewEquivalentExists(messages);
    }

    /// <summary>
    /// תפקיד: מסנן כפילויות, מסנן שגיאות תשתית ושגיאות גולמיות, מתרגם הודעות לאנגלית/עברית ומסיר הודעות באנגלית אם קיימת מקבילה בעברית.
    /// </summary>
    /// <param name="messages"></param>
    /// <returns></returns>
    private static IReadOnlyList<string> NormalizeDisplayMessages(IReadOnlyList<string> messages)
    {
        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var message in messages)
        {
            TryAddCanonical(normalized, seen, message, includeSummary: true);
        }

        return RemoveEnglishWhenHebrewEquivalentExists(normalized);
    }

    /// <summary>
    /// תפקיד: מסיר הודעות באנגלית אם קיים מקבילה בעברית — מניח שהמשתמש יעדיף לראות רק את ההודעה בעברית אם שניהם קיימים.
    /// </summary>
    /// <param name="messages"></param>
    /// <returns></returns>
    private static IReadOnlyList<string> RemoveEnglishWhenHebrewEquivalentExists(
        IReadOnlyList<string> messages)
    {
        var hebrewMessages = new HashSet<string>(
            messages.Where(ContainsHebrew),
            StringComparer.Ordinal);

        return messages
            .Where(message =>
                ContainsHebrew(message)
                || !hebrewMessages.Contains(InfeasibilityExplanationBuilder.TranslatePublic(message)))
            .ToList();
    }

    /// <summary>
    /// תפקיד: מזהה אם מחרוזת מכילה תווים בעברית (Unicode U+0590 עד U+05FF).
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private static bool ContainsHebrew(string message) =>
        message.Any(static ch => ch is >= '\u0590' and <= '\u05FF');

    /// <summary>
    /// תפקיד: מוסיף הודעה לרשימה אם היא ראויה להצגה למשתמש — מסנן כפילויות, שגיאות תשתית ושגיאות גולמיות.
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="seen"></param>
    /// <param name="message"></param>
    /// <param name="includeSummary"></param>
    private static void TryAddCanonical(
        ICollection<string> messages,
        ISet<string> seen,
        string? message,
        bool includeSummary = false)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var canonical = InfeasibilityExplanationBuilder.TranslatePublic(message);
        if (!ContainsHebrew(message)
            && !string.Equals(message, canonical, StringComparison.Ordinal))
        {
            message = canonical;
        }

        var isSummary = string.Equals(canonical, InfeasibleAssignment, StringComparison.Ordinal);

        if (isSummary)
        {
            if (!includeSummary || !seen.Add(canonical))
            {
                return;
            }

            messages.Add(canonical);
            return;
        }

        if (!ShouldIncludeDetail(canonical) || !seen.Add(canonical))
        {
            return;
        }

        messages.Add(canonical);
    }

    /// <summary>
    /// תפקיד: בודק האם שגיאה מפורטת ראויה להצגה למשתמש — מסנן שגיאות תשתית/שגיאות פותר גולמיות.
    /// </summary>
    /// <param name="error"></param>
    /// <returns></returns>
    private static bool ShouldIncludeDetail(string error)
    {
        if (string.IsNullOrWhiteSpace(error)
            || string.Equals(error, InfeasibleAssignment, StringComparison.Ordinal)
            || IsInfrastructureError(error)
            || error.Contains("No legal unit-level move", StringComparison.OrdinalIgnoreCase)
            || error.Contains("External solver", StringComparison.OrdinalIgnoreCase)
            || error.Contains("Solver reported infeasible problem", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!ContainsHebrew(error))
        {
            var translated = InfeasibilityExplanationBuilder.TranslatePublic(error);
            if (!string.Equals(error, translated, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// תפקיד: מזהה שגיאות תשתית (תקשורת/timeout) שמופיעות בשגיאות הפותר.
    /// </summary>
    /// <param name="error"></param>
    /// <returns></returns>
    private static bool IsInfrastructureError(string error) =>
        error.Contains("communication failed", StringComparison.OrdinalIgnoreCase)
        || error.Contains("actively refused", StringComparison.OrdinalIgnoreCase)
        || error.Contains("No connection could be made", StringComparison.OrdinalIgnoreCase)
        || error.Contains("timed out before job submission", StringComparison.OrdinalIgnoreCase)
        || error.Contains("polling timed out", StringComparison.OrdinalIgnoreCase)
        || error.Contains("polling deadline exceeded", StringComparison.OrdinalIgnoreCase);
}