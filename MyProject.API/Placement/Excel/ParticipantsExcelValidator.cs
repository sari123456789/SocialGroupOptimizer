// מגדירים את המרחב של אימות אקסל משתתפים
namespace MyProject.API.Placement.Excel;

// בודק את תוצאת הפרסור ומוודא שהנתונים תקינים
public sealed class ParticipantsExcelValidator
{
    // מאמתים את הנתונים מול הגדרות הקבוצות
    public ParticipantsExcelValidationResult Validate(
        ParticipantsExcelParseResult parseResult,
        int groupCount,
        int minGroupSize,
        int maxGroupSize)
    {
        // בודקים שקיבלנו תוצאת פרסור
        if (parseResult is null)
        {
            // זורקים שגיאה אם התוצאה ריקה
            throw new ArgumentNullException(nameof(parseResult));
        }

        // יוצרים רשימה לאיסוף שגיאות
        var errors = new List<string>();

        // מעתיקים שגיאות שכבר היו בפרסור
        if (parseResult.Errors.Count > 0)
        {
            // מוסיפים את כל השגיאות הקיימות
            errors.AddRange(parseResult.Errors);
        }

        // אם הפרסור נכשל מחזירים כישלון מיד
        if (!parseResult.Success)
        {
            // מחזירים תוצאה לא תקינה עם שגיאות ממוינות
            return ParticipantsExcelValidationResult.Invalid(SortErrors(errors));
        }

        // בודקים שהגדרות הקבוצות תקינות
        ValidateGroupSettings(groupCount, minGroupSize, maxGroupSize, errors);

        // לוקחים את שורות המשתתפים מהפרסור
        var rows = parseResult.Rows;
        // יוצרים קבוצה של מזהים תקינים
        var validParticipantIds = new HashSet<string>(StringComparer.Ordinal);
        // יוצרים מילון לזיהוי מזהים כפולים
        var seenParticipantIds = new Dictionary<string, int>(StringComparer.Ordinal);

        // עוברים על כל השורות לפי מספר שורה
        foreach (var row in rows.OrderBy(entry => entry.RowNumber))
        {
            // בודקים את מזהה המשתתף
            ValidateParticipantId(row, seenParticipantIds, validParticipantIds, errors);
            // בודקים את השם המלא
            ValidateFullName(row, errors);
            // בודקים שאין כפילויות בהעדפות
            ValidatePreferenceDuplicates(row, errors);
            // בודקים שהמשתתף לא מפנה לעצמו
            ValidateSelfReferences(row, errors);
        }

        // עוברים שוב על השורות לבדיקת הפניות
        foreach (var row in rows.OrderBy(entry => entry.RowNumber))
        {
            // בודקים שכל המזהים ברשימות קיימים
            ValidateReferenceLists(row, validParticipantIds, errors);
        }

        // בודקים זוגות כפולים וסתירות גלובליות
        ValidateGlobalPairs(rows, errors);

        // בודקים קיבולת רק אם הגדרות הקבוצות תקינות
        if (groupCount >= 1 && minGroupSize >= 1 && maxGroupSize >= minGroupSize)
        {
            // בודקים שמספר המשתתפים מתאים לקיבולת
            ValidateCapacity(rows.Count, groupCount, minGroupSize, maxGroupSize, errors);
        }

        // מחזירים תוצאה תקינה או לא תקינה לפי השגיאות
        return errors.Count == 0
            ? ParticipantsExcelValidationResult.Valid()
            : ParticipantsExcelValidationResult.Invalid(SortErrors(errors));
    }

    // בודק שמספר הקבוצות וגדלי הקבוצה תקינים
    private static void ValidateGroupSettings(
        int groupCount,
        int minGroupSize,
        int maxGroupSize,
        List<string> errors)
    {
        // בודקים שיש לפחות קבוצה אחת
        if (groupCount < 1)
        {
            // מוסיפים שגיאה על מספר קבוצות לא תקין
            errors.Add("groupCount חייב להיות לפחות 1.");
        }

        // בודקים שהגודל המינימלי לפחות אחד
        if (minGroupSize < 1)
        {
            // מוסיפים שגיאה על גודל מינימלי לא תקין
            errors.Add("minGroupSize חייב להיות לפחות 1.");
        }

        // בודקים שהמקסימום לא קטן מהמינימום
        if (maxGroupSize < minGroupSize)
        {
            // מוסיפים שגיאה על טווח גדלים לא תקין
            errors.Add("maxGroupSize לא יכול להיות קטן מ-minGroupSize.");
        }
    }

    // בודק שמזהה המשתתף תקין ולא כפול
    private static void ValidateParticipantId(
        ParsedParticipantRow row,
        Dictionary<string, int> seenParticipantIds,
        HashSet<string> validParticipantIds,
        List<string> errors)
    {
        // בודקים שיש מזהה משתתף
        if (string.IsNullOrWhiteSpace(row.ParticipantId))
        {
            // מוסיפים שגיאה על מזהה חסר
            errors.Add($"שורה {row.RowNumber}: ParticipantId חסר.");
            // יוצאים מהפונקציה
            return;
        }

        // מנסים לנרמל את המזהה לתבנית תקינה
        if (!TryNormalizeParticipantId(row.ParticipantId, out var normalizedId))
        {
            // מוסיפים שגיאה על מזהה לא תקין
            errors.Add($"שורה {row.RowNumber}: ParticipantId '{row.ParticipantId}' לא תקין — חייב להיות 9 ספרות.");
            // יוצאים מהפונקציה
            return;
        }

        // בודקים שהמזהה לא הופיע כבר בשורה אחרת
        if (!seenParticipantIds.TryAdd(normalizedId, row.RowNumber))
        {
            // מוסיפים שגיאה על מזהה כפול
            errors.Add($"שורה {row.RowNumber}: ParticipantId '{normalizedId}' כפול.");
            // יוצאים מהפונקציה
            return;
        }

        // מוסיפים את המזהה לרשימת המזהים התקינים
        validParticipantIds.Add(normalizedId);
    }

    // בודק שיש שם מלא למשתתף
    private static void ValidateFullName(ParsedParticipantRow row, List<string> errors)
    {
        // בודקים שהשם לא ריק
        if (string.IsNullOrWhiteSpace(row.FullName))
        {
            // מוסיפים שגיאה על שם חסר
            errors.Add($"שורה {row.RowNumber}: FullName חסר.");
        }
    }

    // בודק שאין כפילויות ברשימת ההעדפות
    private static void ValidatePreferenceDuplicates(ParsedParticipantRow row, List<string> errors)
    {
        // יוצרים קבוצה לזיהוי מזהים שכבר ראינו
        var seen = new HashSet<string>(StringComparer.Ordinal);

        // עוברים על כל ההעדפות בשורה
        foreach (var preferredParticipantId in row.Preferences)
        {
            // בודקים שהמזהה לא מופיע פעמיים
            if (!seen.Add(preferredParticipantId))
            {
                // מוסיפים שגיאה על כפילות בהעדפות
                errors.Add(
                    $"שורה {row.RowNumber}: כפילות ב-Preferences עבור '{preferredParticipantId}'.");
            }
        }
    }

    // בודק שהמשתתף לא מפנה לעצמו ברשימות
    private static void ValidateSelfReferences(ParsedParticipantRow row, List<string> errors)
    {
        // מנסים לנרמל את מזהה המשתתף הנוכחי
        if (!TryNormalizeParticipantId(row.ParticipantId, out var normalizedId))
        {
            // יוצאים אם המזהה לא תקין
            return;
        }

        // בודקים שהמשתתף לא מופיע בהעדפות של עצמו
        if (row.Preferences.Any(
            preferredParticipantId => string.Equals(
                preferredParticipantId,
                normalizedId,
                StringComparison.Ordinal)
                || string.Equals(preferredParticipantId, row.ParticipantId, StringComparison.Ordinal)))
        {
            // מוסיפים שגיאה על העדפה עצמית
            errors.Add($"שורה {row.RowNumber}: משתתף לא יכול להעדיף את עצמו ב-Preferences.");
        }

        // בודקים שהמשתתף לא מופיע בחובה יחד עם עצמו
        if (row.MandatoryWith.Any(
            otherParticipantId => IsSameParticipant(otherParticipantId, normalizedId, row.ParticipantId)))
        {
            // מוסיפים שגיאה על זוג חובה עם עצמו
            errors.Add($"שורה {row.RowNumber}: משתתף לא יכול להופיע עם עצמו ב-MandatoryWith.");
        }

        // בודקים שהמשתתף לא מופיע באסור יחד עם עצמו
        if (row.ForbiddenWith.Any(
            otherParticipantId => IsSameParticipant(otherParticipantId, normalizedId, row.ParticipantId)))
        {
            // מוסיפים שגיאה על זוג אסור עם עצמו
            errors.Add($"שורה {row.RowNumber}: משתתף לא יכול להופיע עם עצמו ב-ForbiddenWith.");
        }
    }

    // בודק שכל המזהים ברשימות קיימים בקובץ
    private static void ValidateReferenceLists(
        ParsedParticipantRow row,
        IReadOnlySet<string> validParticipantIds,
        List<string> errors)
    {
        // עוברים על כל ההעדפות
        foreach (var preferredParticipantId in row.Preferences)
        {
            // בודקים שהמזהה קיים ברשימת המשתתפים
            if (!IsKnownParticipant(preferredParticipantId, validParticipantIds))
            {
                // מוסיפים שגיאה על מזהה לא מוכר בהעדפות
                errors.Add(
                    $"שורה {row.RowNumber}: Preferences מכיל מזהה '{preferredParticipantId}' שלא קיים ברשימת המשתתפים.");
            }
        }

        // עוברים על כל זוגות החובה
        foreach (var otherParticipantId in row.MandatoryWith)
        {
            // בודקים שהמזהה קיים ברשימת המשתתפים
            if (!IsKnownParticipant(otherParticipantId, validParticipantIds))
            {
                // מוסיפים שגיאה על מזהה לא מוכר בחובה יחד
                errors.Add(
                    $"שורה {row.RowNumber}: MandatoryWith מכיל מזהה '{otherParticipantId}' שלא קיים ברשימת המשתתפים.");
            }
        }

        // עוברים על כל זוגות האסור
        foreach (var otherParticipantId in row.ForbiddenWith)
        {
            // בודקים שהמזהה קיים ברשימת המשתתפים
            if (!IsKnownParticipant(otherParticipantId, validParticipantIds))
            {
                // מוסיפים שגיאה על מזהה לא מוכר באסור יחד
                errors.Add(
                    $"שורה {row.RowNumber}: ForbiddenWith מכיל מזהה '{otherParticipantId}' שלא קיים ברשימת המשתתפים.");
            }
        }
    }

    // בודק זוגות כפולים וסתירות בין חובה לאסור
    private static void ValidateGlobalPairs(IReadOnlyList<ParsedParticipantRow> rows, List<string> errors)
    {
        // יוצרים מילון לזוגות חובה שכבר ראינו
        var mandatoryPairs = new Dictionary<string, int>(StringComparer.Ordinal);
        // יוצרים מילון לזוגות אסור שכבר ראינו
        var forbiddenPairs = new Dictionary<string, int>(StringComparer.Ordinal);

        // עוברים על כל השורות לפי מספר שורה
        foreach (var row in rows.OrderBy(entry => entry.RowNumber))
        {
            // מנסים לנרמל את מזהה המשתתף בשורה
            if (!TryNormalizeParticipantId(row.ParticipantId, out var normalizedId))
            {
                // מדלגים על שורה עם מזהה לא תקין
                continue;
            }

            // עוברים על כל זוגות החובה בשורה
            foreach (var otherParticipantId in row.MandatoryWith)
            {
                // מנסים לנרמל את מזהה הצד השני
                if (!TryNormalizeParticipantId(otherParticipantId, out var normalizedOtherId))
                {
                    // מדלגים על מזהה לא תקין
                    continue;
                }

                // יוצרים מפתח ייחודי לזוג
                var pairKey = CreatePairKey(normalizedId, normalizedOtherId);
                // בודקים אם הזוג כבר הוגדר בשורה אחרת
                if (mandatoryPairs.TryGetValue(pairKey, out var existingRowNumber))
                {
                    // מוסיפים שגיאה על זוג חובה כפול
                    errors.Add(
                        $"שורה {row.RowNumber}: זוג MandatoryWith כפול (כבר הוגדר בשורה {existingRowNumber}).");
                }
                else
                {
                    // שומרים את הזוג עם מספר השורה
                    mandatoryPairs[pairKey] = row.RowNumber;
                }

                // בודקים אם אותו זוג מופיע גם באסור
                if (forbiddenPairs.ContainsKey(pairKey))
                {
                    // מוסיפים שגיאה על סתירה בין חובה לאסור
                    errors.Add(
                        $"שורה {row.RowNumber}: אותו זוג מופיע גם ב-MandatoryWith וגם ב-ForbiddenWith.");
                }
            }

            // עוברים על כל זוגות האסור בשורה
            foreach (var otherParticipantId in row.ForbiddenWith)
            {
                // מנסים לנרמל את מזהה הצד השני
                if (!TryNormalizeParticipantId(otherParticipantId, out var normalizedOtherId))
                {
                    // מדלגים על מזהה לא תקין
                    continue;
                }

                // יוצרים מפתח ייחודי לזוג
                var pairKey = CreatePairKey(normalizedId, normalizedOtherId);
                // בודקים אם הזוג כבר הוגדר בשורה אחרת
                if (forbiddenPairs.TryGetValue(pairKey, out var existingRowNumber))
                {
                    // מוסיפים שגיאה על זוג אסור כפול
                    errors.Add(
                        $"שורה {row.RowNumber}: זוג ForbiddenWith כפול (כבר הוגדר בשורה {existingRowNumber}).");
                }
                else
                {
                    // שומרים את הזוג עם מספר השורה
                    forbiddenPairs[pairKey] = row.RowNumber;
                }

                // בודקים אם אותו זוג מופיע גם בחובה
                if (mandatoryPairs.ContainsKey(pairKey))
                {
                    // מוסיפים שגיאה על סתירה בין חובה לאסור
                    errors.Add(
                        $"שורה {row.RowNumber}: אותו זוג מופיע גם ב-MandatoryWith וגם ב-ForbiddenWith.");
                }
            }
        }
    }

    // בודק שמספר המשתתפים מתאים לקיבולת הקבוצות
    private static void ValidateCapacity(
        int participantCount,
        int groupCount,
        int minGroupSize,
        int maxGroupSize,
        List<string> errors)
    {
        // מחשבים את הקיבולת המינימלית הכוללת
        var minCapacity = groupCount * minGroupSize;
        // מחשבים את הקיבולת המקסימלית הכוללת
        var maxCapacity = groupCount * maxGroupSize;

        // בודקים שיש יותר מדי משתתפים לקיבולת
        if (participantCount > maxCapacity)
        {
            // מוסיפים שגיאה על חוסר מקומות
            errors.Add(
                $"אין מספיק קיבולת לכל המשתתפים: {participantCount} משתתפים, מקסימום {maxCapacity} מקומות ({groupCount} קבוצות × {maxGroupSize}).");
        }

        // בודקים שיש פחות מדי משתתפים לקיבולת המינימלית
        if (participantCount < minCapacity)
        {
            // מוסיפים שגיאה על יותר מדי מקומות מינימום
            errors.Add(
                $"יותר מדי מקומות מינימום ביחס למספר המשתתפים: {participantCount} משתתפים, נדרשים לפחות {minCapacity} מקומות ({groupCount} קבוצות × {minGroupSize}).");
        }
    }

    // בודק אם מזהה משתתף מוכר ברשימה התקינה
    private static bool IsKnownParticipant(string participantId, IReadOnlySet<string> validParticipantIds)
    {
        // בודקים התאמה ישירה למזהה
        if (validParticipantIds.Contains(participantId))
        {
            // המזהה מוכר
            return true;
        }

        // מנסים לנרמל ולבדוק שוב
        return TryNormalizeParticipantId(participantId, out var normalizedId)
            && validParticipantIds.Contains(normalizedId);
    }

    // בודק אם שני מזהים מייצגים את אותו משתתף
    private static bool IsSameParticipant(
        string otherParticipantId,
        string normalizedCurrentId,
        string rawCurrentId) =>
        // משווים למזהה מנורמל
        string.Equals(otherParticipantId, normalizedCurrentId, StringComparison.Ordinal)
        // או למזהה הגולמי
        || string.Equals(otherParticipantId, rawCurrentId, StringComparison.Ordinal);

    // מנסה לנרמל מזהה משתתף לתבנית של תשע ספרות
    private static bool TryNormalizeParticipantId(string raw, out string normalized)
    {
        // מתחילים עם מחרוזת ריקה
        normalized = string.Empty;

        // בודקים שהקלט לא ריק
        if (string.IsNullOrWhiteSpace(raw))
        {
            // נכשלים אם אין תוכן
            return false;
        }

        // מסירים רווחים מיותרים
        var trimmed = raw.Trim();
        // מחפשים נקודה עשרונית אם יש
        var decimalSeparatorIndex = trimmed.IndexOf('.');
        // אם יש נקודה לוקחים רק את החלק שלפנייה
        if (decimalSeparatorIndex >= 0)
        {
            // חותכים את המחרוזת לפני הנקודה
            trimmed = trimmed[..decimalSeparatorIndex];
        }

        // בודקים שיש בדיוק תשע ספרות
        if (trimmed.Length != 9 || !trimmed.All(char.IsDigit))
        {
            // נכשלים אם האורך או התווים לא תקינים
            return false;
        }

        // שומרים את המזהה המנורמל
        normalized = trimmed;
        // מחזירים הצלחה
        return true;
    }

    // יוצר מפתח ייחודי לזוג משתתפים ללא תלות בסדר
    private static string CreatePairKey(string firstParticipantId, string secondParticipantId) =>
        // מסדרים לפי סדר אלפביתי ומחברים
        string.CompareOrdinal(firstParticipantId, secondParticipantId) <= 0
            ? $"{firstParticipantId}|{secondParticipantId}"
            : $"{secondParticipantId}|{firstParticipantId}";

    // ממיין את רשימת השגיאות לפי מספר שורה
    private static IReadOnlyList<string> SortErrors(IReadOnlyList<string> errors) =>
        // ממיינים קודם לפי מספר שורה ואז לפי טקסט
        errors
            .OrderBy(error => ExtractRowNumber(error))
            .ThenBy(error => error, StringComparer.Ordinal)
            .ToList();

    // מחלץ מספר שורה מהודעת שגיאה
    private static int ExtractRowNumber(string error)
    {
        // הקידומת שמציינת מספר שורה
        const string prefix = "שורה ";
        // בודקים שההודעה מתחילה בקידומת הנכונה
        if (!error.StartsWith(prefix, StringComparison.Ordinal))
        {
            // מחזירים ערך גבוה כדי לדחוף לסוף
            return int.MaxValue;
        }

        // מחפשים את הנקודתיים אחרי מספר השורה
        var spaceIndex = error.IndexOf(':', prefix.Length);
        // בודקים שמצאנו מיקום תקין
        if (spaceIndex <= prefix.Length)
        {
            // מחזירים ערך גבוה אם לא הצלחנו לחלץ
            return int.MaxValue;
        }

        // מנסים לפרסר את מספר השורה מהטקסט
        return int.TryParse(error.AsSpan(prefix.Length, spaceIndex - prefix.Length), out var rowNumber)
            ? rowNumber
            : int.MaxValue;
    }
}
