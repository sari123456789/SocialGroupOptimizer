namespace MyProject.API.Placement.Excel;

public enum ParticipantsSheetColumnKind
{
    ParticipantId,
    FullName,
    Preferences,
    MandatoryWith,
    ForbiddenWith,
    Classification,
}

public sealed record ClassificationColumnDefinition(
    string DimensionCode,
    int ColumnIndex);

public sealed class ParticipantsSheetSchema
{
    public ParticipantsSheetSchema(
        IReadOnlyDictionary<ParticipantsSheetColumnKind, int> fixedColumns,
        IReadOnlyList<ClassificationColumnDefinition> classificationColumns)
    {
        FixedColumns = fixedColumns ?? throw new ArgumentNullException(nameof(fixedColumns));
        ClassificationColumns = classificationColumns ?? throw new ArgumentNullException(nameof(classificationColumns));
    }

    public IReadOnlyDictionary<ParticipantsSheetColumnKind, int> FixedColumns { get; }

    public IReadOnlyList<ClassificationColumnDefinition> ClassificationColumns { get; }

    public int GetRequiredColumn(ParticipantsSheetColumnKind kind)
    {
        if (!FixedColumns.TryGetValue(kind, out var columnIndex))
        {
            throw new InvalidOperationException($"Required column '{kind}' is missing from schema.");
        }

        return columnIndex;
    }
}
