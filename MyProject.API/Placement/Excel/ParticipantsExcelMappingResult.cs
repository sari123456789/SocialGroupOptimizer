using MyProject.BL.Algorithm.InitialPlacement;

namespace MyProject.API.Placement.Excel;

// תוצאה אחרי המרה לקלט אלגוריתם
public sealed class ParticipantsExcelMappingResult
{
    public ParticipantsExcelMappingResult(
        InitialPlacementInput? input,
        IReadOnlyDictionary<string, string> participantFullNamesById,
        IReadOnlyList<string> errors)
    {
        Input = input;
        ParticipantFullNamesById = participantFullNamesById
            ?? throw new ArgumentNullException(nameof(participantFullNamesById));
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    // הצלחה רק עם קלט תקין וללא שגיאות
    public bool Success => Errors.Count == 0 && Input is not null;

    public InitialPlacementInput? Input { get; }

    /// <summary>ParticipantId מנורמל → FullName לתצוגה עתידית.</summary>
    public IReadOnlyDictionary<string, string> ParticipantFullNamesById { get; }

    public IReadOnlyList<string> Errors { get; }

    public static ParticipantsExcelMappingResult Failed(IReadOnlyList<string> errors) =>
        new(null, new Dictionary<string, string>(StringComparer.Ordinal), errors);
}
