using ClosedXML.Excel;
using MyProject.Data.Import;

namespace MyProject.API.Placement.Excel;

// יוצר קובץ דוגמה להורדה — עם משתתפים לדוגמה
public static class ParticipantsExcelTemplateGenerator
{
    public const string TemplateFileName = "participants-template.xlsx";

    public static byte[] CreateTemplateBytes()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(ParticipantsSheetColumnDefinitions.SheetName);

        // שורת כותרות
        sheet.Cell(1, 1).Value = ParticipantsSheetColumnDefinitions.ParticipantId;
        sheet.Cell(1, 2).Value = ParticipantsSheetColumnDefinitions.FullName;
        sheet.Cell(1, 3).Value = "Class";
        sheet.Cell(1, 4).Value = "Level";
        sheet.Cell(1, 5).Value = "Track";
        sheet.Cell(1, 6).Value = ParticipantsSheetColumnDefinitions.Preferences;
        sheet.Cell(1, 7).Value = ParticipantsSheetColumnDefinitions.MandatoryWith;
        sheet.Cell(1, 8).Value = ParticipantsSheetColumnDefinitions.ForbiddenWith;

        // שורות דוגמה — 4 משתתפים
        sheet.Cell(2, 1).SetValue("200000001");
        sheet.Cell(2, 2).Value = "יעל כהן";
        sheet.Cell(2, 3).Value = "י1";
        sheet.Cell(2, 4).Value = "חזק";
        sheet.Cell(2, 5).Value = "מדעי";
        sheet.Cell(2, 6).SetValue("200000002,200000003");
        sheet.Cell(2, 7).SetValue("200000002");
        sheet.Cell(2, 8).SetValue("200000003");

        sheet.Cell(3, 1).SetValue("200000002");
        sheet.Cell(3, 2).Value = "דנה לוי";
        sheet.Cell(3, 3).Value = "י1";
        sheet.Cell(3, 4).Value = "בינוני";
        sheet.Cell(3, 5).Value = "מדעי";
        sheet.Cell(3, 6).SetValue("200000001");
        sheet.Cell(3, 7).SetValue("200000001");

        sheet.Cell(4, 1).SetValue("200000003");
        sheet.Cell(4, 2).Value = "אבי מזרחי";
        sheet.Cell(4, 3).Value = "י2";
        sheet.Cell(4, 4).Value = "חזק";
        sheet.Cell(4, 5).Value = "הומניטרי";
        sheet.Cell(4, 8).SetValue("200000001");

        sheet.Cell(5, 1).SetValue("200000004");
        sheet.Cell(5, 2).Value = "נועה שמי";
        sheet.Cell(5, 3).Value = "י2";
        sheet.Cell(5, 4).Value = "בינוני";
        sheet.Cell(5, 5).Value = "הומניטרי";

        // ת.ז. כטקסט — שלא יהפוך למספר באקסל
        sheet.Column(1).Style.NumberFormat.Format = "@";
        sheet.Column(6).Style.NumberFormat.Format = "@";
        sheet.Column(7).Style.NumberFormat.Format = "@";
        sheet.Column(8).Style.NumberFormat.Format = "@";
        ExcelSheetStyling.ApplyHeaderRow(sheet, 8);
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
