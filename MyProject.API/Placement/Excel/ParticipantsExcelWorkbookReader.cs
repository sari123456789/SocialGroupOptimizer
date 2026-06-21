using ClosedXML.Excel;

// מגדירים את המרחב של קריאת אקסל משתתפים
namespace MyProject.API.Placement.Excel;

// קורא את קובץ האקסל ומחזיר שורות משתתפים מפורסות
public sealed class ParticipantsExcelWorkbookReader
{
    // קוראים את זרם האקסל ומחזירים תוצאת פרסור
    public ParticipantsExcelParseResult Read(Stream excelStream)
    {
        // בודקים שהזרם לא ריק
        if (excelStream is null)
        {
            // זורקים שגיאה אם לא קיבלנו זרם
            throw new ArgumentNullException(nameof(excelStream));
        }

        // יוצרים רשימה לאיסוף שגיאות
        var errors = new List<string>();

        // פותחים את חוברת העבודה מהזרם
        using var workbook = new XLWorkbook(excelStream);
        // מחפשים את הגיליון עם השם הנכון
        var worksheet = workbook.Worksheets.FirstOrDefault(
            sheet => string.Equals(
                sheet.Name,
                ParticipantsSheetColumnDefinitions.SheetName,
                StringComparison.Ordinal));

        // בודקים אם מצאנו את הגיליון
        if (worksheet is null)
        {
            // מחזירים כישלון עם הודעה על גיליון חסר
            return ParticipantsExcelParseResult.Failed(new[]
            {
                $"חסר גיליון '{ParticipantsSheetColumnDefinitions.SheetName}'.",
            });
        }

        // בונים את סכמת העמודות מהשורה הראשונה
        var schema = TryBuildSchema(worksheet, errors);
        // בודקים אם היו שגיאות בבניית הסכמה
        if (errors.Count > 0 || schema is null)
        {
            // מחזירים כישלון עם רשימת השגיאות
            return ParticipantsExcelParseResult.Failed(errors);
        }

        // קוראים את שורות המשתתפים מהגיליון
        var rows = ParseRows(worksheet, schema, errors);
        // בודקים אם היו שגיאות בקריאת השורות
        if (errors.Count > 0)
        {
            // מחזירים כישלון עם רשימת השגיאות
            return ParticipantsExcelParseResult.Failed(errors);
        }

        // בודקים שלא נמצאו שורות משתתפים בכלל
        if (rows.Count == 0)
        {
            // מחזירים כישלון עם הודעה על חוסר שורות
            return ParticipantsExcelParseResult.Failed(new[]
            {
                $"גיליון '{ParticipantsSheetColumnDefinitions.SheetName}': לא נמצאו שורות משתתפים.",
            });
        }

        // מחזירים תוצאה מוצלחת עם הסכמה והשורות
        return ParticipantsExcelParseResult.FromParsed(schema, rows);
    }

    // בונה את מבנה העמודות לפי שורת הכותרות
    private static ParticipantsSheetSchema? TryBuildSchema(IXLWorksheet worksheet, List<string> errors)
    {
        // לוקחים את שורת הכותרות הראשונה
        var headerRow = worksheet.Row(1);
        // מוצאים את מספר העמודה האחרונה בשימוש
        var lastColumn = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

        // בודקים שיש בכלל עמודות בגיליון
        if (lastColumn == 0)
        {
            // מוסיפים שגיאה על חוסר שורת כותרות
            errors.Add($"גיליון '{ParticipantsSheetColumnDefinitions.SheetName}': חסרה שורת כותרות.");
            // מחזירים ריק כי אין סכמה תקינה
            return null;
        }

        // יוצרים מילון לעמודות הקבועות
        var fixedColumns = new Dictionary<ParticipantsSheetColumnKind, int>();
        // יוצרים רשימה לעמודות סיווג
        var classificationColumns = new List<ClassificationColumnDefinition>();
        // יוצרים קבוצה לזיהוי כותרות כפולות
        var seenHeaders = new HashSet<string>(StringComparer.Ordinal);

        // עוברים על כל עמודה בשורת הכותרות
        for (var columnIndex = 1; columnIndex <= lastColumn; columnIndex++)
        {
            // קוראים את טקסט הכותרת של העמודה
            var header = ReadCellText(headerRow.Cell(columnIndex));
            // בודקים שהכותרת לא ריקה
            if (string.IsNullOrWhiteSpace(header))
            {
                // מוסיפים שגיאה על כותרת ריקה
                errors.Add(
                    $"גיליון '{ParticipantsSheetColumnDefinitions.SheetName}', עמודה {columnIndex}: כותרת ריקה.");
                // ממשיכים לעמודה הבאה
                continue;
            }

            // בודקים שאין כפילות בכותרת
            if (!seenHeaders.Add(header))
            {
                // מוסיפים שגיאה על כותרת שמופיעה פעמיים
                errors.Add(
                    $"גיליון '{ParticipantsSheetColumnDefinitions.SheetName}': כותרת '{header}' מופיעה פעמיים.");
                // ממשיכים לעמודה הבאה
                continue;
            }

            // בודקים אם זו עמודת מזהה משתתף
            if (string.Equals(header, ParticipantsSheetColumnDefinitions.ParticipantId, StringComparison.Ordinal))
            {
                // מוסיפים את העמודה הקבועה למילון
                AddFixedColumn(fixedColumns, ParticipantsSheetColumnKind.ParticipantId, columnIndex, errors);
                // ממשיכים לעמודה הבאה
                continue;
            }

            // בודקים אם זו עמודת שם מלא
            if (string.Equals(header, ParticipantsSheetColumnDefinitions.FullName, StringComparison.Ordinal))
            {
                // מוסיפים את העמודה הקבועה למילון
                AddFixedColumn(fixedColumns, ParticipantsSheetColumnKind.FullName, columnIndex, errors);
                // ממשיכים לעמודה הבאה
                continue;
            }

            // בודקים אם זו עמודת העדפות
            if (string.Equals(header, ParticipantsSheetColumnDefinitions.Preferences, StringComparison.Ordinal))
            {
                // מוסיפים את העמודה הקבועה למילון
                AddFixedColumn(fixedColumns, ParticipantsSheetColumnKind.Preferences, columnIndex, errors);
                // ממשיכים לעמודה הבאה
                continue;
            }

            // בודקים אם זו עמודת חובה יחד
            if (string.Equals(header, ParticipantsSheetColumnDefinitions.MandatoryWith, StringComparison.Ordinal))
            {
                // מוסיפים את העמודה הקבועה למילון
                AddFixedColumn(fixedColumns, ParticipantsSheetColumnKind.MandatoryWith, columnIndex, errors);
                // ממשיכים לעמודה הבאה
                continue;
            }

            // בודקים אם זו עמודת אסור יחד
            if (string.Equals(header, ParticipantsSheetColumnDefinitions.ForbiddenWith, StringComparison.Ordinal))
            {
                // מוסיפים את העמודה הקבועה למילון
                AddFixedColumn(fixedColumns, ParticipantsSheetColumnKind.ForbiddenWith, columnIndex, errors);
                // ממשיכים לעמודה הבאה
                continue;
            }

            // כל כותרת אחרת נחשבת לעמודת סיווג
            classificationColumns.Add(new ClassificationColumnDefinition(header, columnIndex));
        }

        // בודקים שיש עמודת מזהה משתתף
        if (!fixedColumns.ContainsKey(ParticipantsSheetColumnKind.ParticipantId))
        {
            // מוסיפים שגיאה על עמודה חסרה
            errors.Add(
                $"גיליון '{ParticipantsSheetColumnDefinitions.SheetName}': חסרה עמודת '{ParticipantsSheetColumnDefinitions.ParticipantId}'.");
        }

        // בודקים שיש עמודת שם מלא
        if (!fixedColumns.ContainsKey(ParticipantsSheetColumnKind.FullName))
        {
            // מוסיפים שגיאה על עמודה חסרה
            errors.Add(
                $"גיליון '{ParticipantsSheetColumnDefinitions.SheetName}': חסרה עמודת '{ParticipantsSheetColumnDefinitions.FullName}'.");
        }

        // מחזירים סכמה רק אם אין שגיאות
        return errors.Count > 0
            ? null
            : new ParticipantsSheetSchema(fixedColumns, classificationColumns);
    }

    // מוסיף עמודה קבועה למילון אם היא לא כפולה
    private static void AddFixedColumn(
        Dictionary<ParticipantsSheetColumnKind, int> fixedColumns,
        ParticipantsSheetColumnKind kind,
        int columnIndex,
        List<string> errors)
    {
        // בודקים אם העמודה כבר הוגדרה קודם
        if (fixedColumns.ContainsKey(kind))
        {
            // מוסיפים שגיאה על הגדרה כפולה של אותה עמודה
            errors.Add(
                $"גיליון '{ParticipantsSheetColumnDefinitions.SheetName}': עמודת '{kind}' מוגדרת יותר מפעם אחת.");
            // יוצאים מהפונקציה
            return;
        }

        // שומרים את מספר העמודה במילון
        fixedColumns[kind] = columnIndex;
    }

    // קורא את שורות הנתונים מהגיליון
    private static List<ParsedParticipantRow> ParseRows(
        IXLWorksheet worksheet,
        ParticipantsSheetSchema schema,
        List<string> errors)
    {
        // יוצרים רשימה לשורות המפורסות
        var rows = new List<ParsedParticipantRow>();
        // מוצאים את מספר השורה האחרונה בשימוש
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        // מוצאים את מספר העמודה האחרונה בשימוש
        var lastColumn = worksheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;

        // עוברים על כל שורת נתונים החל מהשורה השנייה
        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            // לוקחים את השורה הנוכחית
            var row = worksheet.Row(rowNumber);
            // מדלגים על שורות ריקות לגמרי
            if (IsRowEmpty(row, lastColumn))
            {
                // ממשיכים לשורה הבאה
                continue;
            }

            // קוראים את מזהה המשתתף מהשורה
            var participantId = ReadCellText(row.Cell(schema.GetRequiredColumn(ParticipantsSheetColumnKind.ParticipantId)));
            // קוראים את השם המלא מהשורה
            var fullName = ReadCellText(row.Cell(schema.GetRequiredColumn(ParticipantsSheetColumnKind.FullName)));

            // יוצרים מילון לסיווגים של המשתתף
            var classifications = new Dictionary<string, string>(StringComparer.Ordinal);
            // עוברים על כל עמודת סיווג בסכמה
            foreach (var classificationColumn in schema.ClassificationColumns)
            {
                // קוראים את קוד הרמה מהתא
                var levelCode = ReadCellText(row.Cell(classificationColumn.ColumnIndex));
                // מדלגים על תאים ריקים
                if (string.IsNullOrWhiteSpace(levelCode))
                {
                    // ממשיכים לעמודה הבאה
                    continue;
                }

                // שומרים את קוד הרמה לפי מימד הסיווג
                classifications[classificationColumn.DimensionCode] = levelCode.Trim();
            }

            // קוראים את רשימת ההעדפות מופרדות בפסיקים
            var preferences = ReadCommaSeparatedIds(
                schema,
                row,
                ParticipantsSheetColumnKind.Preferences);
            // קוראים את רשימת חובה יחד מופרדת בפסיקים
            var mandatoryWith = ReadCommaSeparatedIds(
                schema,
                row,
                ParticipantsSheetColumnKind.MandatoryWith);
            // קוראים את רשימת אסור יחד מופרדת בפסיקים
            var forbiddenWith = ReadCommaSeparatedIds(
                schema,
                row,
                ParticipantsSheetColumnKind.ForbiddenWith);

            // מוסיפים את השורה המפורסת לרשימה
            rows.Add(new ParsedParticipantRow(
                rowNumber,
                participantId,
                fullName,
                classifications,
                preferences,
                mandatoryWith,
                forbiddenWith));
        }

        // מחזירים את כל השורות שקראנו
        return rows;
    }

    // קורא מזהים מופרדים בפסיקים מתא בעמודה מסוימת
    private static IReadOnlyList<string> ReadCommaSeparatedIds(
        ParticipantsSheetSchema schema,
        IXLRow row,
        ParticipantsSheetColumnKind columnKind)
    {
        // בודקים אם העמודה קיימת בסכמה
        if (!schema.FixedColumns.TryGetValue(columnKind, out var columnIndex))
        {
            // מחזירים רשימה ריקה אם אין עמודה כזו
            return Array.Empty<string>();
        }

        // קוראים את הטקסט הגולמי מהתא
        var raw = ReadCellText(row.Cell(columnIndex));
        // בודקים שהתא לא ריק
        if (string.IsNullOrWhiteSpace(raw))
        {
            // מחזירים רשימה ריקה אם אין תוכן
            return Array.Empty<string>();
        }

        // מפצלים לפי פסיק ומנקים רווחים מיותרים
        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToList();
    }

    // בודק אם כל התאים בשורה ריקים
    private static bool IsRowEmpty(IXLRow row, int lastColumn)
    {
        // עוברים על כל העמודות בשורה
        for (var columnIndex = 1; columnIndex <= lastColumn; columnIndex++)
        {
            // אם מצאנו תא עם תוכן השורה לא ריקה
            if (!string.IsNullOrWhiteSpace(ReadCellText(row.Cell(columnIndex))))
            {
                // מחזירים שהשורה לא ריקה
                return false;
            }
        }

        // אם הגענו לכאן כל התאים ריקים
        return true;
    }

    // קורא טקסט מתא באקסל בצורה אחידה
    private static string ReadCellText(IXLCell cell)
    {
        // בודקים אם התא ריק
        if (cell.IsEmpty())
        {
            // מחזירים מחרוזת ריקה
            return string.Empty;
        }

        // אם התא מכיל מספר מטפלים בו בנפרד
        if (cell.DataType == XLDataType.Number)
        {
            // ממירים למספר שלם ואז למחרוזת
            return ((long)cell.GetDouble()).ToString();
        }

        // לכל סוג אחר מחזירים את הטקסט המעוצב
        return cell.GetFormattedString().Trim();
    }
}