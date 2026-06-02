namespace MyProject.API.Placement.Excel;

public static class ParticipantsSheetColumnDefinitions
{
    public const string SheetName = "Participants";

    public const string ParticipantId = "ParticipantId";
    public const string FullName = "FullName";
    public const string Preferences = "Preferences";
    public const string MandatoryWith = "MandatoryWith";
    public const string ForbiddenWith = "ForbiddenWith";

    public static readonly IReadOnlySet<string> FixedColumnNames = new HashSet<string>(StringComparer.Ordinal)
    {
        ParticipantId,
        FullName,
        Preferences,
        MandatoryWith,
        ForbiddenWith,
    };

    public static bool IsFixedColumn(string header) =>
        FixedColumnNames.Contains(header);
}
