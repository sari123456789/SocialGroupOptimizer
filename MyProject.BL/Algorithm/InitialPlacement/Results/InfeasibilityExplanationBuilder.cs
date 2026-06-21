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
                    // שגיאות גודל/מספר קבוצות מהבנייה החלקית מתארות מצב פנימי כושל,
                    // לא בעיה בנתוני המנהל — מדלגים עליהן כדי לא לבלבל.
                    if (IsPartialBuildGroupArtifact(error))
                    {
                        continue;
                    }

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

    /// <summary>
    /// בונה אבחון אילוצים בעברית כשהפותר לא מצא פתרון — מסביר למנהל מה עלול לחסום.
    /// </summary>
    public static IReadOnlyList<string> BuildDiagnostics(
        InitialPlacementInput input,
        MandatoryUnitMap mandatoryUnits,
        ConflictGraph conflictGraph)
    {
        var messages = new List<string>();

        var mandatoryPairs = input.Constraints.OfType<MandatoryPairConstraint>().ToList();
        var forbiddenPairs = input.Constraints.OfType<ForbiddenPairConstraint>().ToList();
        var groupCountConstraint = input.Constraints.OfType<GroupCountConstraint>().FirstOrDefault();
        var groupSizeConstraints = input.Constraints.OfType<GroupSizeConstraint>().ToList();

        foreach (var mandatory in mandatoryPairs)
        {
            var key = ParticipantPairKey.Create(mandatory.ParticipantA, mandatory.ParticipantB);
            if (forbiddenPairs.Any(forbidden =>
                    ParticipantPairKey.Create(forbidden.ParticipantA, forbidden.ParticipantB) == key))
            {
                messages.Add(
                    $"סתירה: {mandatory.ParticipantA} ו-{mandatory.ParticipantB} מוגדרים גם כזוג חובה וגם כזוג איסור.");
            }
        }

        // סתירות "זוג איסור בתוך אותה יחידת חובה" — סתירה ישירה שיש לתקן.
        foreach (var forbidden in forbiddenPairs)
        {
            if (mandatoryUnits.TryGetUnitId(forbidden.ParticipantA, out var unitA)
                && mandatoryUnits.TryGetUnitId(forbidden.ParticipantB, out var unitB)
                && unitA == unitB)
            {
                messages.Add(
                    $"המשתתפים {forbidden.ParticipantA} ו-{forbidden.ParticipantB} מוגדרים גם חייבים יחד וגם אסורים יחד — סתירה שיש לתקן.");
            }
        }

        var largestUnit = mandatoryUnits.Units.Values.MaxBy(members => members.Count);
        var largestUnitSize = largestUnit?.Count ?? 0;

        // הסיבה הנפוצה: סכום הגדלים המינימליים של הקבוצות תופס כמעט את כל המשתתפים,
        // ולכן בפועל נשאר מעט מאוד מקום בכל קבוצה — וקבוצת-חובה גדולה לא נכנסת.
        if (largestUnitSize > 1)
        {
            var effectiveCapacity = EffectiveMaxGroupCapacity(
                input.Participants.Count,
                groupCountConstraint,
                groupSizeConstraints);

            if (effectiveCapacity > 0 && largestUnitSize > effectiveCapacity)
            {
                messages.Add(
                    $"כדי שכל הקבוצות יעמדו בגודל המינימלי שהוגדר, נשאר מקום ל-{effectiveCapacity} משתתפים בלבד בכל קבוצה, " +
                    $"אך זוגות החובה יוצרים קבוצת-משתתפים של {largestUnitSize} שחייבים להיות יחד. " +
                    "פתרונות: הקטינו את מספר הקבוצות, הקטינו את הגודל המינימלי של הקבוצה, או פצלו חלק מזוגות החובה.");
            }
        }

        if (groupCountConstraint is not null && mandatoryUnits.Units.Count > 0)
        {
            var colorsNeeded = EstimateMinimumColors(conflictGraph.Adjacency);
            if (colorsNeeded > groupCountConstraint.MaxGroups)
            {
                messages.Add(
                    $"בגלל שילוב של דרישות \"חייבים יחד\" ו\"אסורים יחד\", צריך לפצל את המשתתפים ל-{colorsNeeded} קבוצות לפחות, " +
                    $"אך הוגדרו רק {groupCountConstraint.MaxGroups} קבוצות. הוסיפו קבוצות או הקטינו את מספר זוגות החובה/האיסור.");
            }
        }

        if (groupSizeConstraints.Count > 0 && largestUnitSize > 0)
        {
            var maxGroupSize = groupSizeConstraints.Max(constraint => constraint.MaxCapacity.Value);
            if (largestUnitSize > maxGroupSize)
            {
                messages.Add(
                    $"יש קבוצת משתתפים שחייבים להיות יחד בגודל {largestUnitSize}, " +
                    $"אך הקבוצה המקסימלית מכילה רק {maxGroupSize} משתתפים. הקטינו את זוגות החובה או הגדילו את גודל הקבוצה.");
            }
        }

        return messages
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

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

        var mandatoryUnitSizeMatch = Regex.Match(
            error,
            @"Mandatory unit with (?<count>\d+) participants is larger than maximum group capacity (?<max>\d+)\.");
        if (mandatoryUnitSizeMatch.Success)
        {
            return
                $"יחידת חובה של {mandatoryUnitSizeMatch.Groups["count"].Value} משתתפים גדולה מגודל הקבוצה המקסימלי " +
                $"({mandatoryUnitSizeMatch.Groups["max"].Value}).";
        }

        if (error.Contains("obvious clique", StringComparison.OrdinalIgnoreCase))
        {
            return "יש יותר מדי יחידות חובה שסותרות זו את זו מול מספר הקבוצות המותר.";
        }

        var mandatoryForbiddenMatch = Regex.Match(
            error,
            @"Participants (?<a>\S+) and (?<b>\S+) are both mandatory together and forbidden together\.");
        if (mandatoryForbiddenMatch.Success)
        {
            return
                $"סתירה: {mandatoryForbiddenMatch.Groups["a"].Value} ו-{mandatoryForbiddenMatch.Groups["b"].Value} " +
                "מוגדרים גם כזוג חובה וגם כזוג איסור.";
        }

        var capacityTooSmallMatch = Regex.Match(
            error,
            @"Participant count (?<count>\d+) is smaller than total minimum required capacity (?<min>\d+)\.");
        if (capacityTooSmallMatch.Success)
        {
            return
                $"מספר המשתתפים ({capacityTooSmallMatch.Groups["count"].Value}) קטן מהקיבולת המינימלית הנדרשת " +
                $"({capacityTooSmallMatch.Groups["min"].Value}).";
        }

        var capacityTooLargeMatch = Regex.Match(
            error,
            @"Participant count (?<count>\d+) is greater than total maximum capacity (?<max>\d+)\.");
        if (capacityTooLargeMatch.Success)
        {
            return
                $"מספר המשתתפים ({capacityTooLargeMatch.Groups["count"].Value}) גדול מהקיבולת המקסימלית המותרת " +
                $"({capacityTooLargeMatch.Groups["max"].Value}).";
        }

        var cliqueMatch = Regex.Match(
            error,
            @"Conflict graph contains an obvious clique of (?<units>\d+) mandatory units, but only (?<groups>\d+) groups are allowed\.");
        if (cliqueMatch.Success)
        {
            return
                $"יש {cliqueMatch.Groups["units"].Value} יחידות חובה שכולן סותרות זו את זו, " +
                $"אך מותרות רק {cliqueMatch.Groups["groups"].Value} קבוצות.";
        }

        if (error.Contains("Solver reported infeasible problem", StringComparison.OrdinalIgnoreCase)
            || error.Contains("External solver failed", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        // שגיאות תוצר של בנייה חלקית כושלת — לא רלוונטיות למנהל.
        if (IsPartialBuildGroupArtifact(error))
        {
            return string.Empty;
        }

        return error;
    }

    /// <summary>
    /// מחשב כמה משתתפים קבוצה אחת יכולה להכיל בפועל, בהנחה שכל שאר הקבוצות
    /// חייבות לעמוד בגודל המינימלי שהוגדר להן. מחזיר 0 אם אי אפשר לקבוע.
    /// </summary>
    private static int EffectiveMaxGroupCapacity(
        int participantCount,
        GroupCountConstraint? groupCountConstraint,
        IReadOnlyList<GroupSizeConstraint> groupSizeConstraints)
    {
        var sizeByGroupId = new Dictionary<int, GroupSizeConstraint>();
        foreach (var constraint in groupSizeConstraints)
        {
            sizeByGroupId[constraint.GroupId.Value] = constraint;
        }

        var groupCount = groupCountConstraint?.MaxGroups ?? 0;
        if (groupCount <= 0 && sizeByGroupId.Count > 0)
        {
            groupCount = sizeByGroupId.Keys.Max();
        }

        if (groupCount <= 0 || participantCount <= 0)
        {
            return 0;
        }

        var minByGroup = new int[groupCount + 1];
        var maxByGroup = new int[groupCount + 1];
        var totalMin = 0;
        for (var groupId = 1; groupId <= groupCount; groupId++)
        {
            if (sizeByGroupId.TryGetValue(groupId, out var size))
            {
                minByGroup[groupId] = size.MinSize;
                maxByGroup[groupId] = size.MaxCapacity.Value;
            }
            else
            {
                minByGroup[groupId] = 0;
                maxByGroup[groupId] = participantCount;
            }

            totalMin += minByGroup[groupId];
        }

        var best = 0;
        for (var groupId = 1; groupId <= groupCount; groupId++)
        {
            // המקום שנשאר לקבוצה זו אחרי שכל השאר תופסות את המינימום שלהן.
            var roomLeft = participantCount - (totalMin - minByGroup[groupId]);
            var capacity = Math.Min(maxByGroup[groupId], roomLeft);
            best = Math.Max(best, capacity);
        }

        return best;
    }

    private static bool IsPartialBuildGroupArtifact(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return false;
        }

        return error.Contains("required by GroupSizeConstraint was not found", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(error, @"Group \S+ has \d+ participant\(s\) but requires between \d+ and \d+\.");
    }

    private static int EstimateMinimumColors(IReadOnlyDictionary<int, HashSet<int>> adjacency)
    {
        if (adjacency.Count == 0)
        {
            return 0;
        }

        var order = adjacency.Keys
            .OrderByDescending(unitId => adjacency[unitId].Count)
            .ToList();

        var colorByUnit = new Dictionary<int, int>();
        var maxColor = 0;

        foreach (var unitId in order)
        {
            var usedColors = new HashSet<int>();
            foreach (var neighbor in adjacency[unitId])
            {
                if (colorByUnit.TryGetValue(neighbor, out var neighborColor))
                {
                    usedColors.Add(neighborColor);
                }
            }

            var color = 1;
            while (usedColors.Contains(color))
            {
                color++;
            }

            colorByUnit[unitId] = color;
            maxColor = Math.Max(maxColor, color);
        }

        return maxColor;
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
