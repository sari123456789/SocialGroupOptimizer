namespace MyProject.API.Placement.Excel;

// תוצאת ולידציה על הנתונים שעברו parse
public sealed class ParticipantsExcelValidationResult
{
    public ParticipantsExcelValidationResult(IReadOnlyList<string> errors)
    {
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    // תקין כשאין שגיאות
    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<string> Errors { get; }

    public static ParticipantsExcelValidationResult Valid() =>
        new(Array.Empty<string>());

    public static ParticipantsExcelValidationResult Invalid(IReadOnlyList<string> errors) =>
        new(errors);
}
