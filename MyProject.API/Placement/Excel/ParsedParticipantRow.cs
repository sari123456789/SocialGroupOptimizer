namespace MyProject.API.Placement.Excel;

// שורה אחת מהאקסל אחרי הקריאה — לפני ולידציה מלאה
public sealed class ParsedParticipantRow
{
    public ParsedParticipantRow(
        int rowNumber,
        string participantId,
        string fullName,
        IReadOnlyDictionary<string, string> classifications,
        IReadOnlyList<string> preferences,
        IReadOnlyList<string> mandatoryWith,
        IReadOnlyList<string> forbiddenWith)
    {
        // מספר שורה חייב להיות חיובי
        if (rowNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowNumber));
        }

        RowNumber = rowNumber;
        ParticipantId = participantId ?? throw new ArgumentNullException(nameof(participantId));
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        Classifications = classifications ?? throw new ArgumentNullException(nameof(classifications));
        Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        MandatoryWith = mandatoryWith ?? throw new ArgumentNullException(nameof(mandatoryWith));
        ForbiddenWith = forbiddenWith ?? throw new ArgumentNullException(nameof(forbiddenWith));
    }

    // מספר השורה בגיליון — לשגיאות
    public int RowNumber { get; }

    /// <summary>מספר זהות כפי שנקרא מהאקסל (לפני ולידציה).</summary>
    public string ParticipantId { get; }

    /// <summary>שם מלא — metadata לתצוגה עתידית, לא נכנס לאלגוריתם בשלב זה.</summary>
    public string FullName { get; }

    /// <summary>מימד סיווג → רמה. מילון ריק אם אין ערכי סיווג.</summary>
    public IReadOnlyDictionary<string, string> Classifications { get; }

    // רשימת ת.ז. מועדפים לפי סדר
    public IReadOnlyList<string> Preferences { get; }

    // עם מי חייבים להיות באותה קבוצה
    public IReadOnlyList<string> MandatoryWith { get; }

    // עם מי אסור באותה קבוצה
    public IReadOnlyList<string> ForbiddenWith { get; }
}
