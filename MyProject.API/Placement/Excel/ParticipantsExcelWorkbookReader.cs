using ClosedXML.Excel;

namespace MyProject.API.Placement.Excel;

public sealed class ParticipantsExcelWorkbookReader
{
    public ParticipantsExcelParseResult Read(Stream excelStream)
    {
        if (excelStream is null)
        {
            throw new ArgumentNullException(nameof(excelStream));
        }

        var errors = new List<string>();

        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.FirstOrDefault(
            sheet => string.Equals(
                sheet.Name,
                ParticipantsSheetColumnDefinitions.SheetName,
                StringComparison.Ordinal));

        if (worksheet is null)
        {
            return ParticipantsExcelParseResult.Failed(new[]
            {
                $"חסר גיליון '{ParticipantsSheetColumnDefinitions.SheetName}'.",
            });
        }

        var schema = TryBuildSchema(worksheet, errors);
        if (errors.Count > 0 || schema is null)
        {
            return ParticipantsExcelParseResult.Failed(errors);
        }

        var rows = ParseRows(worksheet, schema, errors);
        if (errors.Count > 0)
        {
            return ParticipantsExcelParseResult.Failed(errors);
        }

        if (rows.Count == 0)
        {
            return ParticipantsExcelParseResult.Failed(new[]
            {
                $"גיליון '{ParticipantsSheetColumnDefinitions.SheetName}': לא נמצאו שורות משתתפים.",
            });
        }

        return ParticipantsExcelParseResult.FromParsed(schema, rows);
    }

    private static ParticipantsSheetSchema? TryBuildSchema(IXLWorksheet worksheet, List<string> errors)
    {
        var headerRow = worksheet.Row(1);
        var lastColumn = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

        if (lastColumn == 0)
        {
            errors.Add($"גיליון '{ParticipantsSheetColumnDefinitions.SheetName}': חסרה שורת כותרות.");
            return null;
        }

        var fixedColumns = new Dictionary<ParticipantsSheetColumnKind, int>();
        var classificationColumns = new List<ClassificationColumnDefinition>();
        var seenHeaders = new HashSet<string>(StringComparer.Ordinal);

        for (var columnIndex = 1; columnIndex <= lastColumn; columnIndex++)
        {
            var header = ReadCellText(headerRow.Cell(columnIndex));
            if (string.IsNullOrWhiteSpace(header))
            {
                errors.Add(
                    $"גיליון '{ParticipantsSheetColumnDefinitions.SheetName}', עמודה {columnIndex}: כותרת ריקה.");
                continue;
            }

            if (!seenHeaders.Add(header))
            {
                errors.Add(
                    $"גיליון '{ParticipantsSheetColumnDefinitions.SheetName}': כותרת '{header}' מופיעה פעמיים.");
                continue;
            }

            if (string.Equals(header, ParticipantsSheetColumnDefinitions.ParticipantId, StringComparison.Ordinal))
            {
                AddFixedColumn(fixedColumns, ParticipantsSheetColumnKind.ParticipantId, columnIndex, errors);
                continue;
            }

            if (string.Equals(header, ParticipantsSheetColumnDefinitions.FullName, StringComparison.Ordinal))
            {
                AddFixedColumn(fixedColumns, ParticipantsSheetColumnKind.FullName, columnIndex, errors);
                continue;
            }

            if (string.Equals(header, ParticipantsSheetColumnDefinitions.Preferences, StringComparison.Ordinal))
            {
                AddFixedColumn(fixedColumns, ParticipantsSheetColumnKind.Preferences, columnIndex, errors);
                continue;
            }

            if (string.Equals(header, ParticipantsSheetColumnDefinitions.MandatoryWith, StringComparison.Ordinal))
            {
                AddFixedColumn(fixedColumns, ParticipantsSheetColumnKind.MandatoryWith, columnIndex, errors);
                continue;
            }

            if (string.Equals(header, ParticipantsSheetColumnDefinitions.ForbiddenWith, StringComparison.Ordinal))
            {
                AddFixedColumn(fixedColumns, ParticipantsSheetColumnKind.ForbiddenWith, columnIndex, errors);
                continue;
            }

            classificationColumns.Add(new ClassificationColumnDefinition(header, columnIndex));
        }

        if (!fixedColumns.ContainsKey(ParticipantsSheetColumnKind.ParticipantId))
        {
            errors.Add(
                $"גיליון '{ParticipantsSheetColumnDefinitions.SheetName}': חסרה עמודת '{ParticipantsSheetColumnDefinitions.ParticipantId}'.");
        }

        if (!fixedColumns.ContainsKey(ParticipantsSheetColumnKind.FullName))
        {
            errors.Add(
                $"גיליון '{ParticipantsSheetColumnDefinitions.SheetName}': חסרה עמודת '{ParticipantsSheetColumnDefinitions.FullName}'.");
        }

        return errors.Count > 0
            ? null
            : new ParticipantsSheetSchema(fixedColumns, classificationColumns);
    }

    private static void AddFixedColumn(
        Dictionary<ParticipantsSheetColumnKind, int> fixedColumns,
        ParticipantsSheetColumnKind kind,
        int columnIndex,
        List<string> errors)
    {
        if (fixedColumns.ContainsKey(kind))
        {
            errors.Add(
                $"גיליון '{ParticipantsSheetColumnDefinitions.SheetName}': עמודת '{kind}' מוגדרת יותר מפעם אחת.");
            return;
        }

        fixedColumns[kind] = columnIndex;
    }

    private static List<ParsedParticipantRow> ParseRows(
        IXLWorksheet worksheet,
        ParticipantsSheetSchema schema,
        List<string> errors)
    {
        var rows = new List<ParsedParticipantRow>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        var lastColumn = worksheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            if (IsRowEmpty(row, lastColumn))
            {
                continue;
            }

            var participantId = ReadCellText(row.Cell(schema.GetRequiredColumn(ParticipantsSheetColumnKind.ParticipantId)));
            var fullName = ReadCellText(row.Cell(schema.GetRequiredColumn(ParticipantsSheetColumnKind.FullName)));

            var classifications = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var classificationColumn in schema.ClassificationColumns)
            {
                var levelCode = ReadCellText(row.Cell(classificationColumn.ColumnIndex));
                if (string.IsNullOrWhiteSpace(levelCode))
                {
                    continue;
                }

                classifications[classificationColumn.DimensionCode] = levelCode.Trim();
            }

            var preferences = ReadCommaSeparatedIds(
                schema,
                row,
                ParticipantsSheetColumnKind.Preferences);
            var mandatoryWith = ReadCommaSeparatedIds(
                schema,
                row,
                ParticipantsSheetColumnKind.MandatoryWith);
            var forbiddenWith = ReadCommaSeparatedIds(
                schema,
                row,
                ParticipantsSheetColumnKind.ForbiddenWith);

            rows.Add(new ParsedParticipantRow(
                rowNumber,
                participantId,
                fullName,
                classifications,
                preferences,
                mandatoryWith,
                forbiddenWith));
        }

        return rows;
    }

    private static IReadOnlyList<string> ReadCommaSeparatedIds(
        ParticipantsSheetSchema schema,
        IXLRow row,
        ParticipantsSheetColumnKind columnKind)
    {
        if (!schema.FixedColumns.TryGetValue(columnKind, out var columnIndex))
        {
            return Array.Empty<string>();
        }

        var raw = ReadCellText(row.Cell(columnIndex));
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToList();
    }

    private static bool IsRowEmpty(IXLRow row, int lastColumn)
    {
        for (var columnIndex = 1; columnIndex <= lastColumn; columnIndex++)
        {
            if (!string.IsNullOrWhiteSpace(ReadCellText(row.Cell(columnIndex))))
            {
                return false;
            }
        }

        return true;
    }

    private static string ReadCellText(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        if (cell.DataType == XLDataType.Number)
        {
            return ((long)cell.GetDouble()).ToString();
        }

        return cell.GetFormattedString().Trim();
    }
}
