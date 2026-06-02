namespace MyProject.API.Placement.Excel;

public sealed class ParticipantsExcelParseResult
{
    public ParticipantsExcelParseResult(
        ParticipantsSheetSchema? schema,
        IReadOnlyList<ParsedParticipantRow> rows,
        IReadOnlyList<string> errors)
    {
        Schema = schema;
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    public bool Success => Errors.Count == 0 && Schema is not null;

    public ParticipantsSheetSchema? Schema { get; }

    public IReadOnlyList<ParsedParticipantRow> Rows { get; }

    public IReadOnlyList<string> Errors { get; }

    public static ParticipantsExcelParseResult Failed(IReadOnlyList<string> errors) =>
        new(null, Array.Empty<ParsedParticipantRow>(), errors);

    public static ParticipantsExcelParseResult FromParsed(
        ParticipantsSheetSchema schema,
        IReadOnlyList<ParsedParticipantRow> rows) =>
        new(schema, rows, Array.Empty<string>());
}
