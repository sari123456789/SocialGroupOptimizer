using ClosedXML.Excel;
using MyProject.API.Placement.Excel;
using MyProject.Data.Import;

namespace MyProject.API.Placement;

public static class AssignmentWorkbookFormatDetector
{
    public static bool IsSingleSheetParticipantsFormat(Stream stream)
    {
        if (!stream.CanSeek)
        {
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            buffer.Position = 0;
            return IsSingleSheetParticipantsFormat(buffer);
        }

        var position = stream.Position;
        using var workbook = new XLWorkbook(stream);
        stream.Position = position;

        var hasParticipants = workbook.Worksheets.Any(worksheet =>
            string.Equals(
                worksheet.Name,
                ParticipantsSheetColumnDefinitions.SheetName,
                StringComparison.Ordinal));

        var hasHebrewParticipants = workbook.Worksheets.Any(worksheet =>
            string.Equals(
                worksheet.Name,
                ExcelTemplateDefinitions.ParticipantsSheet,
                StringComparison.Ordinal));

        return hasParticipants && !hasHebrewParticipants;
    }
}
