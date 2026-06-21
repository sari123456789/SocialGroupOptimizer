namespace MyProject.API.Placement.Excel;

// שמות העמודות הקבועות בגיליון Participants
public static class ParticipantsSheetColumnDefinitions
{
    // שם הגיליון בקובץ
    public const string SheetName = "Participants";

    public const string ParticipantId = "ParticipantId";
    public const string FullName = "FullName";
    public const string Preferences = "Preferences";
    public const string MandatoryWith = "MandatoryWith";
    public const string ForbiddenWith = "ForbiddenWith";

    // העמודות שחייבות להופיע (בנוסף לעמודות סיווג דינמיות)
    public static readonly IReadOnlySet<string> FixedColumnNames = new HashSet<string>(StringComparer.Ordinal)
    {
        ParticipantId,
        FullName,
        Preferences,
        MandatoryWith,
        ForbiddenWith,
    };

    // בודק אם כותרת היא אחת מהעמודות הקבועות
    public static bool IsFixedColumn(string header) =>
        FixedColumnNames.Contains(header);
}
