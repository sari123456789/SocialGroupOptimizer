using ClosedXML.Excel;

namespace MyProject.Data.Import;

public static class ExcelSheetStyling
{
    public static void ApplyHeaderRow(IXLWorksheet sheet, int columnCount)
    {
        if (columnCount <= 0)
        {
            return;
        }

        var headerRange = sheet.Range(1, 1, 1, columnCount);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EAF6");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Medium;

        sheet.SheetView.FreezeRows(1);
    }
}
