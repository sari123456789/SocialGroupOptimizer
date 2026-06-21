namespace MyProject.API.Placement.Excel;

// תוצאה אחרי קריאת האקסל — סכימה, שורות ושגיאות
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

    // הצלחה רק אם אין שגיאות ויש סכימה
    public bool Success => Errors.Count == 0 && Schema is not null;

    // מבנה העמודות שזוהה מהכותרות
    public ParticipantsSheetSchema? Schema { get; }

    public IReadOnlyList<ParsedParticipantRow> Rows { get; }

    public IReadOnlyList<string> Errors { get; }

    // בונה תוצאת כשלון
    public static ParticipantsExcelParseResult Failed(IReadOnlyList<string> errors) =>
        new(null, Array.Empty<ParsedParticipantRow>(), errors);

    // בונה תוצאה מוצלחת
    public static ParticipantsExcelParseResult FromParsed(
        ParticipantsSheetSchema schema,
        IReadOnlyList<ParsedParticipantRow> rows) =>
        new(schema, rows, Array.Empty<string>());
}
