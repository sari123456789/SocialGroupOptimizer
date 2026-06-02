using ClosedXML.Excel;
using MyProject.Data.Import;

namespace MyProject.API.Placement.Excel;

/// <summary>
/// יוצר קובץ Excel לניסוי עומס: 20 משתתפים, 5 קבוצות × 4, מקרי קצה רבים.
/// </summary>
public static class StressTestParticipantsWorkbookGenerator
{
    public const string FileName = "stress-test-20-participants.xlsx";

    /// <summary>
    /// 5 קבוצות × 4 משתתפים. סיווגים מותאמים לאיזון יחסי (כל מימד: 5+10+5 או 10+10).
    /// </summary>
    private static readonly StressParticipant[] Participants =
    {
        // שרשרת חובה (001→002 בלבד)
        new("300000001", "יעל כהן", "י1", "חזק", "מדעי", "300000002", "300000002", ""),
        new("300000002", "דנה לוי", "י1", "בינוני", "הומניטרי", "300000003", "", ""),
        new("300000003", "אבי מזרחי", "י2", "חזק", "הומניטרי", "300000002", "", ""),

        // זוג חובה — שניהם י1
        new("300000004", "נועה שמי", "י1", "בינוני", "מקצועי", "300000005", "300000005", ""),
        new("300000005", "רוני כהן", "י1", "חלש", "מדעי", "300000004", "", ""),

        // זוג איסור
        new("300000006", "מיכל אברהם", "י2", "בינוני", "הומניטרי", "300000008", "", "300000007"),
        new("300000007", "אורי דוד", "י2", "חזק", "מדעי", "300000009", "", ""),

        // יחידת חובה של 4 (שרשרת 008→009→010→011)
        new("300000008", "שירה גולן", "י1", "בינוני", "הומניטרי", "300000009", "300000009", ""),
        new("300000009", "תומר וייס", "י1", "חלש", "מקצועי", "300000010", "300000010", ""),
        new("300000010", "הילה רוזן", "י2", "חזק", "מדעי", "300000011", "300000011", ""),
        new("300000011", "גיא שפירא", "י2", "בינוני", "הומניטרי", "300000010", "", ""),

        // חובה + איסור צולבים (012+013, 012≠014, 014+015)
        new("300000012", "ליאור ממן", "י1", "חזק", "מדעי", "300000013", "300000013", "300000014"),
        new("300000013", "מאיה בר", "י2", "בינוני", "הומניטרי", "300000012", "", ""),
        new("300000014", "איתי קפלן", "י1", "בינוני", "מקצועי", "300000015", "300000015", ""),
        new("300000015", "רוני לוי", "י2", "חזק", "מדעי", "300000014", "", ""),

        // משתתפים בודדים + העדפות + איסור נוסף
        new("300000016", "שרה נח", "י2", "בינוני", "הומניטרי", "300000017,300000018", "", ""),
        new("300000017", "יוני פרץ", "י1", "בינוני", "הומניטרי", "300000016", "", "300000018"),
        new("300000018", "נעמה שלו", "י2", "חלש", "הומניטרי", "300000016", "", ""),
        new("300000019", "אלון צור", "י1", "בינוני", "הומניטרי", "300000020", "300000020", ""),
        new("300000020", "טלי אש", "י2", "חלש", "מקצועי", "300000019", "", ""),
    };

    public static byte[] CreateWorkbookBytes() => CreateWorkbookBytes(out _);

    public static byte[] CreateWorkbookBytes(out StressTestScenarioInfo info)
    {
        info = new StressTestScenarioInfo(
            ParticipantCount: Participants.Length,
            GroupCount: 5,
            MinGroupSize: 4,
            MaxGroupSize: 4,
            EdgeCases: new[]
            {
                "שרשרת MandatoryWith (001→002)",
                "זוג חובה עם אותה כיתה (004+005, שניהם י1)",
                "זוג איסור (006≠007)",
                "יחידת חובה של 4 — ממלאת קבוצה שלמה (008–011)",
                "חובה + איסור צולבים (012+013, 012≠014, 014+015)",
                "העדפות מדורגות (Preferences) ברשימות שונות",
                "20 משתתפים, 5 קבוצות × 4, סיווגים מותאמים לאיזון יחסי",
            },
            RecommendedClassificationRules: new[]
            {
                ("Class", "Balance"),
                ("Level", "Balance"),
                ("Track", "Balance"),
            });

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(ParticipantsSheetColumnDefinitions.SheetName);

        sheet.Cell(1, 1).Value = ParticipantsSheetColumnDefinitions.ParticipantId;
        sheet.Cell(1, 2).Value = ParticipantsSheetColumnDefinitions.FullName;
        sheet.Cell(1, 3).Value = "Class";
        sheet.Cell(1, 4).Value = "Level";
        sheet.Cell(1, 5).Value = "Track";
        sheet.Cell(1, 6).Value = ParticipantsSheetColumnDefinitions.Preferences;
        sheet.Cell(1, 7).Value = ParticipantsSheetColumnDefinitions.MandatoryWith;
        sheet.Cell(1, 8).Value = ParticipantsSheetColumnDefinitions.ForbiddenWith;

        for (var i = 0; i < Participants.Length; i++)
        {
            var row = i + 2;
            var p = Participants[i];
            sheet.Cell(row, 1).SetValue(p.Id);
            sheet.Cell(row, 2).Value = p.Name;
            sheet.Cell(row, 3).Value = p.Class;
            sheet.Cell(row, 4).Value = p.Level;
            sheet.Cell(row, 5).Value = p.Track;
            sheet.Cell(row, 6).SetValue(p.Preferences);
            sheet.Cell(row, 7).SetValue(p.Mandatory);
            sheet.Cell(row, 8).SetValue(p.Forbidden);
        }

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

    public static void SaveToFile(string path)
    {
        var bytes = CreateWorkbookBytes(out _);
        File.WriteAllBytes(path, bytes);
    }

    private sealed record StressParticipant(
        string Id,
        string Name,
        string Class,
        string Level,
        string Track,
        string Preferences,
        string Mandatory,
        string Forbidden);

    public sealed record StressTestScenarioInfo(
        int ParticipantCount,
        int GroupCount,
        int MinGroupSize,
        int MaxGroupSize,
        IReadOnlyList<string> EdgeCases,
        IReadOnlyList<(string Dimension, string RuleType)> RecommendedClassificationRules);
}
