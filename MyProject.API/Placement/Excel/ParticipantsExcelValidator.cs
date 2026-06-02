namespace MyProject.API.Placement.Excel;

public sealed class ParticipantsExcelValidator
{
    public ParticipantsExcelValidationResult Validate(
        ParticipantsExcelParseResult parseResult,
        int groupCount,
        int minGroupSize,
        int maxGroupSize)
    {
        if (parseResult is null)
        {
            throw new ArgumentNullException(nameof(parseResult));
        }

        var errors = new List<string>();

        if (parseResult.Errors.Count > 0)
        {
            errors.AddRange(parseResult.Errors);
        }

        if (!parseResult.Success)
        {
            return ParticipantsExcelValidationResult.Invalid(SortErrors(errors));
        }

        ValidateGroupSettings(groupCount, minGroupSize, maxGroupSize, errors);

        var rows = parseResult.Rows;
        var validParticipantIds = new HashSet<string>(StringComparer.Ordinal);
        var seenParticipantIds = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var row in rows.OrderBy(entry => entry.RowNumber))
        {
            ValidateParticipantId(row, seenParticipantIds, validParticipantIds, errors);
            ValidateFullName(row, errors);
            ValidatePreferenceDuplicates(row, errors);
            ValidateSelfReferences(row, errors);
        }

        foreach (var row in rows.OrderBy(entry => entry.RowNumber))
        {
            ValidateReferenceLists(row, validParticipantIds, errors);
        }

        ValidateGlobalPairs(rows, errors);

        if (groupCount >= 1 && minGroupSize >= 1 && maxGroupSize >= minGroupSize)
        {
            ValidateCapacity(rows.Count, groupCount, minGroupSize, maxGroupSize, errors);
        }

        return errors.Count == 0
            ? ParticipantsExcelValidationResult.Valid()
            : ParticipantsExcelValidationResult.Invalid(SortErrors(errors));
    }

    private static void ValidateGroupSettings(
        int groupCount,
        int minGroupSize,
        int maxGroupSize,
        List<string> errors)
    {
        if (groupCount < 1)
        {
            errors.Add("groupCount חייב להיות לפחות 1.");
        }

        if (minGroupSize < 1)
        {
            errors.Add("minGroupSize חייב להיות לפחות 1.");
        }

        if (maxGroupSize < minGroupSize)
        {
            errors.Add("maxGroupSize לא יכול להיות קטן מ-minGroupSize.");
        }
    }

    private static void ValidateParticipantId(
        ParsedParticipantRow row,
        Dictionary<string, int> seenParticipantIds,
        HashSet<string> validParticipantIds,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(row.ParticipantId))
        {
            errors.Add($"שורה {row.RowNumber}: ParticipantId חסר.");
            return;
        }

        if (!TryNormalizeParticipantId(row.ParticipantId, out var normalizedId))
        {
            errors.Add($"שורה {row.RowNumber}: ParticipantId '{row.ParticipantId}' לא תקין — חייב להיות 9 ספרות.");
            return;
        }

        if (!seenParticipantIds.TryAdd(normalizedId, row.RowNumber))
        {
            errors.Add($"שורה {row.RowNumber}: ParticipantId '{normalizedId}' כפול.");
            return;
        }

        validParticipantIds.Add(normalizedId);
    }

    private static void ValidateFullName(ParsedParticipantRow row, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(row.FullName))
        {
            errors.Add($"שורה {row.RowNumber}: FullName חסר.");
        }
    }

    private static void ValidatePreferenceDuplicates(ParsedParticipantRow row, List<string> errors)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var preferredParticipantId in row.Preferences)
        {
            if (!seen.Add(preferredParticipantId))
            {
                errors.Add(
                    $"שורה {row.RowNumber}: כפילות ב-Preferences עבור '{preferredParticipantId}'.");
            }
        }
    }

    private static void ValidateSelfReferences(ParsedParticipantRow row, List<string> errors)
    {
        if (!TryNormalizeParticipantId(row.ParticipantId, out var normalizedId))
        {
            return;
        }

        if (row.Preferences.Any(
            preferredParticipantId => string.Equals(
                preferredParticipantId,
                normalizedId,
                StringComparison.Ordinal)
                || string.Equals(preferredParticipantId, row.ParticipantId, StringComparison.Ordinal)))
        {
            errors.Add($"שורה {row.RowNumber}: משתתף לא יכול להעדיף את עצמו ב-Preferences.");
        }

        if (row.MandatoryWith.Any(
            otherParticipantId => IsSameParticipant(otherParticipantId, normalizedId, row.ParticipantId)))
        {
            errors.Add($"שורה {row.RowNumber}: משתתף לא יכול להופיע עם עצמו ב-MandatoryWith.");
        }

        if (row.ForbiddenWith.Any(
            otherParticipantId => IsSameParticipant(otherParticipantId, normalizedId, row.ParticipantId)))
        {
            errors.Add($"שורה {row.RowNumber}: משתתף לא יכול להופיע עם עצמו ב-ForbiddenWith.");
        }
    }

    private static void ValidateReferenceLists(
        ParsedParticipantRow row,
        IReadOnlySet<string> validParticipantIds,
        List<string> errors)
    {
        foreach (var preferredParticipantId in row.Preferences)
        {
            if (!IsKnownParticipant(preferredParticipantId, validParticipantIds))
            {
                errors.Add(
                    $"שורה {row.RowNumber}: Preferences מכיל מזהה '{preferredParticipantId}' שלא קיים ברשימת המשתתפים.");
            }
        }

        foreach (var otherParticipantId in row.MandatoryWith)
        {
            if (!IsKnownParticipant(otherParticipantId, validParticipantIds))
            {
                errors.Add(
                    $"שורה {row.RowNumber}: MandatoryWith מכיל מזהה '{otherParticipantId}' שלא קיים ברשימת המשתתפים.");
            }
        }

        foreach (var otherParticipantId in row.ForbiddenWith)
        {
            if (!IsKnownParticipant(otherParticipantId, validParticipantIds))
            {
                errors.Add(
                    $"שורה {row.RowNumber}: ForbiddenWith מכיל מזהה '{otherParticipantId}' שלא קיים ברשימת המשתתפים.");
            }
        }
    }

    private static void ValidateGlobalPairs(IReadOnlyList<ParsedParticipantRow> rows, List<string> errors)
    {
        var mandatoryPairs = new Dictionary<string, int>(StringComparer.Ordinal);
        var forbiddenPairs = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var row in rows.OrderBy(entry => entry.RowNumber))
        {
            if (!TryNormalizeParticipantId(row.ParticipantId, out var normalizedId))
            {
                continue;
            }

            foreach (var otherParticipantId in row.MandatoryWith)
            {
                if (!TryNormalizeParticipantId(otherParticipantId, out var normalizedOtherId))
                {
                    continue;
                }

                var pairKey = CreatePairKey(normalizedId, normalizedOtherId);
                if (mandatoryPairs.TryGetValue(pairKey, out var existingRowNumber))
                {
                    errors.Add(
                        $"שורה {row.RowNumber}: זוג MandatoryWith כפול (כבר הוגדר בשורה {existingRowNumber}).");
                }
                else
                {
                    mandatoryPairs[pairKey] = row.RowNumber;
                }

                if (forbiddenPairs.ContainsKey(pairKey))
                {
                    errors.Add(
                        $"שורה {row.RowNumber}: אותו זוג מופיע גם ב-MandatoryWith וגם ב-ForbiddenWith.");
                }
            }

            foreach (var otherParticipantId in row.ForbiddenWith)
            {
                if (!TryNormalizeParticipantId(otherParticipantId, out var normalizedOtherId))
                {
                    continue;
                }

                var pairKey = CreatePairKey(normalizedId, normalizedOtherId);
                if (forbiddenPairs.TryGetValue(pairKey, out var existingRowNumber))
                {
                    errors.Add(
                        $"שורה {row.RowNumber}: זוג ForbiddenWith כפול (כבר הוגדר בשורה {existingRowNumber}).");
                }
                else
                {
                    forbiddenPairs[pairKey] = row.RowNumber;
                }

                if (mandatoryPairs.ContainsKey(pairKey))
                {
                    errors.Add(
                        $"שורה {row.RowNumber}: אותו זוג מופיע גם ב-MandatoryWith וגם ב-ForbiddenWith.");
                }
            }
        }
    }

    private static void ValidateCapacity(
        int participantCount,
        int groupCount,
        int minGroupSize,
        int maxGroupSize,
        List<string> errors)
    {
        var minCapacity = groupCount * minGroupSize;
        var maxCapacity = groupCount * maxGroupSize;

        if (participantCount > maxCapacity)
        {
            errors.Add(
                $"אין מספיק קיבולת לכל המשתתפים: {participantCount} משתתפים, מקסימום {maxCapacity} מקומות ({groupCount} קבוצות × {maxGroupSize}).");
        }

        if (participantCount < minCapacity)
        {
            errors.Add(
                $"יותר מדי מקומות מינימום ביחס למספר המשתתפים: {participantCount} משתתפים, נדרשים לפחות {minCapacity} מקומות ({groupCount} קבוצות × {minGroupSize}).");
        }
    }

    private static bool IsKnownParticipant(string participantId, IReadOnlySet<string> validParticipantIds)
    {
        if (validParticipantIds.Contains(participantId))
        {
            return true;
        }

        return TryNormalizeParticipantId(participantId, out var normalizedId)
            && validParticipantIds.Contains(normalizedId);
    }

    private static bool IsSameParticipant(
        string otherParticipantId,
        string normalizedCurrentId,
        string rawCurrentId) =>
        string.Equals(otherParticipantId, normalizedCurrentId, StringComparison.Ordinal)
        || string.Equals(otherParticipantId, rawCurrentId, StringComparison.Ordinal);

    private static bool TryNormalizeParticipantId(string raw, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var trimmed = raw.Trim();
        var decimalSeparatorIndex = trimmed.IndexOf('.');
        if (decimalSeparatorIndex >= 0)
        {
            trimmed = trimmed[..decimalSeparatorIndex];
        }

        if (trimmed.Length != 9 || !trimmed.All(char.IsDigit))
        {
            return false;
        }

        normalized = trimmed;
        return true;
    }

    private static string CreatePairKey(string firstParticipantId, string secondParticipantId) =>
        string.CompareOrdinal(firstParticipantId, secondParticipantId) <= 0
            ? $"{firstParticipantId}|{secondParticipantId}"
            : $"{secondParticipantId}|{firstParticipantId}";

    private static IReadOnlyList<string> SortErrors(IReadOnlyList<string> errors) =>
        errors
            .OrderBy(error => ExtractRowNumber(error))
            .ThenBy(error => error, StringComparer.Ordinal)
            .ToList();

    private static int ExtractRowNumber(string error)
    {
        const string prefix = "שורה ";
        if (!error.StartsWith(prefix, StringComparison.Ordinal))
        {
            return int.MaxValue;
        }

        var spaceIndex = error.IndexOf(':', prefix.Length);
        if (spaceIndex <= prefix.Length)
        {
            return int.MaxValue;
        }

        return int.TryParse(error.AsSpan(prefix.Length, spaceIndex - prefix.Length), out var rowNumber)
            ? rowNumber
            : int.MaxValue;
    }
}
