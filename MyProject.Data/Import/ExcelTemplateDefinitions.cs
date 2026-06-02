namespace MyProject.Data.Import;

public static class ExcelTemplateDefinitions
{
    public const string ParticipantsSheet = "משתתפים";
    public const string SettingsSheet = "הגדרות_שיבוץ";
    public const string PairsSheet = "זוגות";
    public const string ClassificationRulesSheet = "אילוצי_סיווג";
    public const string ReadmeSheet = "README";

    public const string IdentityColumn = "תעודת_זהות";
    public const string NameColumn = "שם";

    public const string SettingAssignmentName = "שם_שיבוץ";
    public const string SettingMinGroups = "מספר_קבוצות_מינימום";
    public const string SettingMaxGroups = "מספר_קבוצות_מаксимום";
    public const string SettingMinGroupSize = "גודל_קבוצה_מינימום";
    public const string SettingMaxGroupSize = "גודל_קבוצה_מаксимום";

    public const string PairTypeColumn = "סוג";
    public const string PairParticipantAColumn = "משתתף_א";
    public const string PairParticipantBColumn = "משתתף_ב";

    public const string ClassificationDimensionColumn = "מימד";
    public const string ClassificationRuleTypeColumn = "סוג_אילוץ";

    public const string PairTypeMandatory = "חובה";
    public const string PairTypeForbidden = "איסור";

    public const string RuleTypeBalance = "איזון";
    public const string RuleTypeSeparation = "הפרדה";

    public const string TemplateFileName = "assignment-import-template.xlsx";
}
