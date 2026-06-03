namespace MyProject.BL.Algorithm.LocalSearch.Generation;

/// <summary>
/// מצב החיפוש הנוכחי — קלט להחלטות יצירת מועמדים.
/// </summary>
/// <remarks>
/// <para>תפקיד: נושא מדדי מצב מהלולאת החיפוש אל <see cref="MoveGenerationPolicy"/> ואל האסטרטגיות.</para>
/// <para>יחסים מחושבים בבנאי בטווח 0.0–1.0 לפי מונים וסה״כ.</para>
/// </remarks>
public sealed class MoveGenerationContext
{
    /// <summary>
    /// יוצר הקשר חיפוש read-only עם יחסים מחושבים.
    /// </summary>
    /// <param name="iterationIndex">אינדקס האיטרציה הנוכחית (0-based).</param>
    /// <param name="iterationsSinceImprovement">כמה איטרציות עברו ללא שיפור בציון.</param>
    /// <param name="isolatedParticipantCount">מספר משתתפים מבודדים.</param>
    /// <param name="weakGroupCount">מספר קבוצות חלשות (מדד-עזר).</param>
    /// <param name="totalParticipants">סה״כ משתתפים — חייב להיות &gt; 0.</param>
    /// <param name="totalGroups">סה״כ קבוצות — חייב להיות &gt; 0.</param>
    public MoveGenerationContext(
        int iterationIndex,
        int iterationsSinceImprovement,
        int isolatedParticipantCount,
        int weakGroupCount,
        int totalParticipants,
        int totalGroups)
    {
        if (iterationIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(iterationIndex), "Iteration index must not be negative.");
        }

        if (iterationsSinceImprovement < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(iterationsSinceImprovement), "Iterations since improvement must not be negative.");
        }

        if (isolatedParticipantCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(isolatedParticipantCount), "Isolated participant count must not be negative.");
        }

        if (weakGroupCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weakGroupCount), "Weak group count must not be negative.");
        }

        if (totalParticipants <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalParticipants), "Total participants must be greater than zero.");
        }

        if (totalGroups <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalGroups), "Total groups must be greater than zero.");
        }

        if (isolatedParticipantCount > totalParticipants)
        {
            throw new ArgumentOutOfRangeException(
                nameof(isolatedParticipantCount),
                "Isolated participant count cannot exceed total participants.");
        }

        if (weakGroupCount > totalGroups)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weakGroupCount),
                "Weak group count cannot exceed total groups.");
        }

        IterationIndex = iterationIndex;
        IterationsSinceImprovement = iterationsSinceImprovement;
        IsolatedParticipantCount = isolatedParticipantCount;
        WeakGroupCount = weakGroupCount;
        TotalParticipants = totalParticipants;
        TotalGroups = totalGroups;
        IsolatedParticipantRatio = isolatedParticipantCount / (double)totalParticipants;
        WeakGroupRatio = weakGroupCount / (double)totalGroups;
    }

    /// <summary>אינדקס האיטרציה הנוכחית (0-based).</summary>
    public int IterationIndex { get; }

    /// <summary>מספר איטרציות רצופות ללא שיפור בציון.</summary>
    public int IterationsSinceImprovement { get; }

    /// <summary>מספר המשתתפים המבודדים בחלוקה הנוכחית.</summary>
    public int IsolatedParticipantCount { get; }

    /// <summary>מספר הקבוצות החלשות בחלוקה הנוכחית (לפי מדד-עזר).</summary>
    public int WeakGroupCount { get; }

    /// <summary>סה״כ משתתפים בחלוקה.</summary>
    public int TotalParticipants { get; }

    /// <summary>סה״כ קבוצות בחלוקה.</summary>
    public int TotalGroups { get; }

    /// <summary>יחס מבודדים: IsolatedParticipantCount / TotalParticipants (0.0–1.0).</summary>
    public double IsolatedParticipantRatio { get; }

    /// <summary>יחס קבוצות חלשות: WeakGroupCount / TotalGroups (0.0–1.0).</summary>
    public double WeakGroupRatio { get; }
}
