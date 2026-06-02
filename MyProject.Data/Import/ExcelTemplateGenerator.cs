using ClosedXML.Excel;

namespace MyProject.Data.Import;

public static class ExcelTemplateGenerator
{
    public static byte[] CreateTemplateBytes()
    {
        using var workbook = new XLWorkbook();

        var participants = workbook.Worksheets.Add(ExcelTemplateDefinitions.ParticipantsSheet);
        participants.Cell(1, 1).Value = ExcelTemplateDefinitions.IdentityColumn;
        participants.Cell(1, 2).Value = ExcelTemplateDefinitions.NameColumn;
        participants.Cell(1, 3).Value = "level";
        participants.Cell(2, 1).SetValue("200000001");
        participants.Cell(2, 2).Value = "א";
        participants.Cell(2, 3).Value = "default";
        participants.Cell(3, 1).SetValue("200000002");
        participants.Cell(3, 2).Value = "ב";
        participants.Cell(3, 3).Value = "default";
        participants.Cell(4, 1).SetValue("200000003");
        participants.Cell(4, 2).Value = "ג";
        participants.Cell(4, 3).Value = "default";
        participants.Cell(5, 1).SetValue("200000004");
        participants.Cell(5, 2).Value = "ד";
        participants.Cell(5, 3).Value = "default";
        participants.Column(1).Style.NumberFormat.Format = "@";
        ExcelSheetStyling.ApplyHeaderRow(participants, 3);
        participants.Columns().AdjustToContents();

        var settings = workbook.Worksheets.Add(ExcelTemplateDefinitions.SettingsSheet);
        settings.Cell(1, 1).Value = "הגדרה";
        settings.Cell(1, 2).Value = "ערך";
        settings.Cell(2, 1).Value = ExcelTemplateDefinitions.SettingAssignmentName;
        settings.Cell(2, 2).Value = "שיבוץ דמו";
        settings.Cell(3, 1).Value = ExcelTemplateDefinitions.SettingMinGroups;
        settings.Cell(3, 2).Value = 2;
        settings.Cell(4, 1).Value = ExcelTemplateDefinitions.SettingMaxGroups;
        settings.Cell(4, 2).Value = 2;
        settings.Cell(5, 1).Value = ExcelTemplateDefinitions.SettingMinGroupSize;
        settings.Cell(5, 2).Value = 2;
        settings.Cell(6, 1).Value = ExcelTemplateDefinitions.SettingMaxGroupSize;
        settings.Cell(6, 2).Value = 2;
        ExcelSheetStyling.ApplyHeaderRow(settings, 2);
        settings.Columns().AdjustToContents();

        var pairs = workbook.Worksheets.Add(ExcelTemplateDefinitions.PairsSheet);
        pairs.Cell(1, 1).Value = ExcelTemplateDefinitions.PairTypeColumn;
        pairs.Cell(1, 2).Value = ExcelTemplateDefinitions.PairParticipantAColumn;
        pairs.Cell(1, 3).Value = ExcelTemplateDefinitions.PairParticipantBColumn;
        pairs.Cell(2, 1).Value = ExcelTemplateDefinitions.PairTypeMandatory;
        pairs.Cell(2, 2).SetValue("200000001");
        pairs.Cell(2, 3).SetValue("200000002");
        pairs.Cell(3, 1).Value = ExcelTemplateDefinitions.PairTypeForbidden;
        pairs.Cell(3, 2).SetValue("200000001");
        pairs.Cell(3, 3).SetValue("200000003");
        pairs.Column(2).Style.NumberFormat.Format = "@";
        pairs.Column(3).Style.NumberFormat.Format = "@";
        ExcelSheetStyling.ApplyHeaderRow(pairs, 3);
        pairs.Columns().AdjustToContents();

        var classificationRules = workbook.Worksheets.Add(ExcelTemplateDefinitions.ClassificationRulesSheet);
        classificationRules.Cell(1, 1).Value = ExcelTemplateDefinitions.ClassificationDimensionColumn;
        classificationRules.Cell(1, 2).Value = ExcelTemplateDefinitions.ClassificationRuleTypeColumn;
        classificationRules.Cell(2, 1).Value = "level";
        classificationRules.Cell(2, 2).Value = ExcelTemplateDefinitions.RuleTypeBalance;
        ExcelSheetStyling.ApplyHeaderRow(classificationRules, 2);
        classificationRules.Columns().AdjustToContents();

        var readme = workbook.Worksheets.Add(ExcelTemplateDefinitions.ReadmeSheet);
        readme.Cell(1, 1).Value = "תבנית ייבוא שיבוץ — מלאי את הגיליונות משתתפים, הגדרות_שיבוץ, זוגות, אילוצי_סיווג.";
        readme.Cell(2, 1).Value = "תעודת זהות: 9 ספרות, טקסט, ייחודית.";
        readme.Cell(3, 1).Value = "עמודות סיווג: שם עמודה = מימד, ערך = רמה.";
        readme.Hide();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
