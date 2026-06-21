namespace MyProject.API.Placement.Excel;

// סוגי העמודות הקבועות בגיליון
public enum ParticipantsSheetColumnKind
{
    ParticipantId,
    FullName,
    Preferences,
    MandatoryWith,
    ForbiddenWith,
    Classification,
}

// עמודת סיווג — שם המימד + מיקום בעמודה
public sealed record ClassificationColumnDefinition(
    string DimensionCode,
    int ColumnIndex);

// המבנה שזוהה משורת הכותרות
public sealed class ParticipantsSheetSchema
{
    public ParticipantsSheetSchema(
        IReadOnlyDictionary<ParticipantsSheetColumnKind, int> fixedColumns,
        IReadOnlyList<ClassificationColumnDefinition> classificationColumns)
    {
        FixedColumns = fixedColumns ?? throw new ArgumentNullException(nameof(fixedColumns));
        ClassificationColumns = classificationColumns ?? throw new ArgumentNullException(nameof(classificationColumns));
    }

    // איפה כל עמודה קבועה נמצאת
    public IReadOnlyDictionary<ParticipantsSheetColumnKind, int> FixedColumns { get; }

    // עמודות סיווג נוספות (Class, Level וכו')
    public IReadOnlyList<ClassificationColumnDefinition> ClassificationColumns { get; }

    // מחזיר מספר עמודה — זורק אם חסרה
    public int GetRequiredColumn(ParticipantsSheetColumnKind kind)
    {
        if (!FixedColumns.TryGetValue(kind, out var columnIndex))
        {
            throw new InvalidOperationException($"Required column '{kind}' is missing from schema.");
        }

        return columnIndex;
    }
}
