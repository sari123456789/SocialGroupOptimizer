using System.Text.RegularExpressions;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Services;
using MyProject.Core.Domain.ValueObjects;
using DomainGroup = MyProject.Core.Domain.Entities.Group;

namespace MyProject.BL.Algorithm.InitialPlacement.Results;

/// <summary>
/// תפקיד: מעשיר הודעות כשלון בהסברים בעברית — מתרגם שגיאות טכניות ומוסיף רמזים.
/// </summary>
/// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator"/>, GreedyPlacementBuilder, <see cref="InitialPlacementUserMessages"/>.</remarks>
public static class InfeasibilityExplanationBuilder
{
    /// <summary>
    /// תפקיד: מתרגם שגיאות גולמיות, מאמת חלוקה חלקית, ומוסיף רמזים אם אין הסבר.
    /// </summary>
    /// <param name="status">סטטוס התוצאה — משפיע על רמזי fallback.</param>
    /// <param name="input">קלט ההצבה.</param>
    /// <param name="mandatoryUnits">יחידות חובה — לרמזים.</param>
    /// <param name="rawErrors">שגיאות גולמיות מהאלגוריתם.</param>
    /// <param name="partialGroups">חלוקה חלקית (אם קיימת) לבדיקת אילוצים נוספת.</param>
    /// <param name="validator">מאמת אילוצים.</param>
    /// <returns>רשימת הודעות בעברית ללא כפילויות.</returns>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator.TrySolverOrReportInfeasible"/>.</remarks>
    public static IReadOnlyList<string> Enrich(
        InitialPlacementStatus status,
        InitialPlacementInput input,
        MandatoryUnitMap mandatoryUnits,
        IReadOnlyList<string> rawErrors,
        IReadOnlyList<DomainGroup>? partialGroups,
        IAssignmentValidator validator)
    {
        var messages = new List<string>();

        // שלב 1: תרגום שגיאות גולמיות לעברית.
        foreach (var error in rawErrors)
        {
            var translated = Translate(error);
            if (!string.IsNullOrWhiteSpace(translated))
            {
                messages.Add(translated);
            }
        }

        // שלב 2: אם יש חלוקה חלקית — מאמתים ומוסיפים שגיאות אילוץ נוספות.
        if (partialGroups is { Count: > 0 })
        {
            var assignment = new Assignment(partialGroups);
            if (!validator.IsValid(assignment, input.Constraints, out var validationErrors))
            {
                foreach (var error in validationErrors)
                {
                    var translated = Translate(error);
                    if (!string.IsNullOrWhiteSpace(translated))
                    {
                        messages.Add(translated);
                    }
                }
            }
        }

        // Distinct — הסרת כפילויות; Where — סינון הודעות ריקות.
        messages = messages
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        // שלב 3: אם אין הסבר — מוסיפים רמזים כלליים לפי סטטוס.
        if (messages.Count == 0)
        {
            messages.AddRange(BuildFallbackHints(status, input, mandatoryUnits));
        }

        return messages;
    }

    /// <summary>
    /// תפקיד: מסביר למה יחידת שיבוץ לא נכנסה לאף קבוצה פנויה.
    /// </summary>
    /// <param name="members">חברי היחידה.</param>
    /// <param name="slots">משבצות קבוצות נוכחיות.</param>
    /// <param name="capacities">קיבולת מקסימלית לכל קבוצה.</param>
    /// <param name="forbiddenLookup">מפת זוגות איסור.</param>
    /// <returns>הודעה בעברית; ריקה אם נמצאה קבוצה מתאימה.</returns>
    /// <remarks>נקרא מ- <see cref="GreedyPlacementBuilder"/> בעת כשלון שיבוץ יחידה.</remarks>
    public static string DescribeUnitPlacementFailure(
        IReadOnlyList<ParticipantId> members,
        Dictionary<int, List<ParticipantId>> slots,
        Dictionary<int, int> capacities,
        Dictionary<ParticipantId, HashSet<ParticipantId>> forbiddenLookup)
    {
        var unitLabel = members.Count == 1
            ? $"משתתף {members[0]}"
            : $"יחידת חובה של {members.Count} משתתפים ({FormatParticipantIds(members)})";

        // capacityBlocked — true אם אף קבוצה לא מספיקה בגודל; forbiddenBlocked — אם יש מקום אך איסורים חוסמים.
        var capacityBlocked = true;
        var forbiddenBlocked = false;

        foreach (var groupIndex in slots.Keys.OrderBy(key => key))
        {
            if (slots[groupIndex].Count + members.Count <= capacities[groupIndex])
            {
                capacityBlocked = false;

                if (!HasForbiddenConflict(members, slots[groupIndex], forbiddenLookup))
                {
                    // נמצאה קבוצה מתאימה — אין הודעת כשלון.
                    return string.Empty;
                }

                forbiddenBlocked = true;
            }
        }

        // בחירת הודעה לפי סוג החסימה (קיבולת / איסור / שניהם).
        if (capacityBlocked && forbiddenBlocked)
        {
            return $"{unitLabel} — אין קבוצה עם מקום פנוי שעומדת בזוגות האיסור.";
        }

        if (capacityBlocked)
        {
            return $"{unitLabel} — אין קבוצה עם מספיק מקום פנוי.";
        }

        return $"{unitLabel} — זוגות האיסור מונעים שיבוץ לכל הקבוצות הפנויות.";
    }

    /// <summary>
    /// תפקיד: מתרגם שגיאה בודדת לעברית — API ציבורי לשימוש חיצוני.
    /// </summary>
    /// <param name="error">הודעת שגיאה (אנגלית או עברית).</param>
    /// <returns>הודעה מתורגמת או המקור אם כבר בעברית.</returns>
    /// <remarks>נקרא מ- <see cref="InitialPlacementUserMessages"/>.</remarks>
    public static string TranslatePublic(string error) => Translate(error);

    private static IEnumerable<string> BuildFallbackHints(
        InitialPlacementStatus status,
        InitialPlacementInput input,
        MandatoryUnitMap mandatoryUnits)
    {
        // yield return — רמזים לפי סטטוס ומאפייני הקלט, בלי לבנות רשימה מראש.
        if (status is InitialPlacementStatus.BuildFailed)
        {
            yield return "הבנייה החמדנית לא הצליחה לשבץ את כל המשתתפים. בדקו שילוב של זוגות חובה, זוגות איסור וגודל קבוצות.";
        }
        else if (status is InitialPlacementStatus.RepairFailed)
        {
            yield return "האלגוריתם המקומי לא מצא חלוקה חוקית. ייתכן שהשילוב הנוכחי של אילוצים קשה לפתרון, או שיש סתירה שלא אותרה.";
        }

        var balanceDimensions = input.Constraints
            .OfType<ClassificationProportionalBalanceConstraint>()
            .Select(constraint => constraint.TargetDimension.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (balanceDimensions.Count > 0)
        {
            yield return
                $"הוגדר איזון יחסי במימדים: {string.Join(", ", balanceDimensions)}. " +
                "יחידות חובה גדולות עם ריכוז של אותה רמה עלולות לחסום איזון.";
        }

        var largestUnit = mandatoryUnits.Units.Values.MaxBy(members => members.Count);
        if (largestUnit is { Count: >= 3 })
        {
            yield return
                $"קיימת יחידת חובה של {largestUnit.Count} משתתפים — היא ממלאת קבוצה שלמה ומגבילה את האפשרויות.";
        }
    }

    private static string Translate(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return string.Empty;
        }

        // אם כבר בעברית — מחזירים כמו שהוא.
        if (error.Contains("יחידת חובה", StringComparison.Ordinal)
            || error.Contains("לא ניתן", StringComparison.Ordinal)
            || Regex.IsMatch(error, @"[\u0590-\u05FF]"))
        {
            return error;
        }

        // Regex — תבניות שגיאה ידועות מהמאמת/האלגוריתם.
        var mandatoryPairMatch = Regex.Match(
            error,
            @"MandatoryPair violated: (?<a>\S+) is in group (?<ga>\S+) but (?<b>\S+) is in group (?<gb>\S+)\.");
        if (mandatoryPairMatch.Success)
        {
            return
                $"זוג חובה ({mandatoryPairMatch.Groups["a"].Value}, {mandatoryPairMatch.Groups["b"].Value}) " +
                $"מפוצל בין קבוצות {mandatoryPairMatch.Groups["ga"].Value} ו-{mandatoryPairMatch.Groups["gb"].Value}.";
        }

        var forbiddenPairMatch = Regex.Match(
            error,
            @"ForbiddenPair violated: (?<a>\S+) and (?<b>\S+) are both in group (?<g>\S+)\.");
        if (forbiddenPairMatch.Success)
        {
            return
                $"זוג איסור ({forbiddenPairMatch.Groups["a"].Value}, {forbiddenPairMatch.Groups["b"].Value}) " +
                $"נמצא יחד בקבוצה {forbiddenPairMatch.Groups["g"].Value}.";
        }

        var proportionalMatch = Regex.Match(
            error,
            @"ClassificationProportionalBalance violated for dimension '(?<dim>[^']+)' levels \[(?<levels>[^\]]*)\]");
        if (proportionalMatch.Success)
        {
            return
                $"אילוץ איזון יחסי במימד '{proportionalMatch.Groups["dim"].Value}' לא מתקיים בחלוקה שנבנתה " +
                $"(רמות: {proportionalMatch.Groups["levels"].Value}).";
        }

        var balanceMatch = Regex.Match(
            error,
            @"ClassificationBalance violated for dimension '(?<dim>[^']+)' level '(?<level>[^']+)':");
        if (balanceMatch.Success)
        {
            return
                $"אילוץ איזון במימד '{balanceMatch.Groups["dim"].Value}', רמה '{balanceMatch.Groups["level"].Value}', " +
                "לא מתקיים בחלוקה שנבנתה.";
        }

        var groupSizeMatch = Regex.Match(
            error,
            @"Group (?<id>\S+) has (?<count>\d+) participant\(s\) but requires between (?<min>\d+) and (?<max>\d+)\.");
        if (groupSizeMatch.Success)
        {
            return
                $"קבוצה {groupSizeMatch.Groups["id"].Value} מכילה {groupSizeMatch.Groups["count"].Value} משתתפים, " +
                $"אך נדרש בין {groupSizeMatch.Groups["min"].Value} ל-{groupSizeMatch.Groups["max"].Value}.";
        }

        if (error.Contains("Forbidden participants", StringComparison.Ordinal)
            && error.Contains("same mandatory unit", StringComparison.Ordinal))
        {
            var idsMatch = Regex.Match(error, @"Forbidden participants (?<a>\S+) and (?<b>\S+)");
            if (idsMatch.Success)
            {
                return
                    $"זוג איסור ({idsMatch.Groups["a"].Value}, {idsMatch.Groups["b"].Value}) " +
                    "נמצא בתוך אותה יחידת חובה — סתירה מובנית.";
            }
        }

        if (error.Contains("obvious clique", StringComparison.OrdinalIgnoreCase))
        {
            return "יש יותר מדי יחידות חובה שסותרות זו את זו מול מספר הקבוצות המותר.";
        }

        return error;
    }

    private static string FormatParticipantIds(IReadOnlyList<ParticipantId> members) =>
        string.Join(", ", members.Select(member => member.Value));

    private static bool HasForbiddenConflict(
        IEnumerable<ParticipantId> incomingMembers,
        IReadOnlyList<ParticipantId> existingMembers,
        Dictionary<ParticipantId, HashSet<ParticipantId>> forbiddenLookup)
    {
        foreach (var incoming in incomingMembers)
        {
            if (!forbiddenLookup.TryGetValue(incoming, out var forbidden))
            {
                continue;
            }

            // Any — בודק אם מישהו מהקבוצה הקיימת נמצא ברשימת האיסורים של incoming.
            if (existingMembers.Any(existing => forbidden.Contains(existing)))
            {
                return true;
            }
        }

        return false;
    }
}
