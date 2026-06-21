namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// תפקיד המחלקה: גרף קונפליקטים לא-מכוון בין יחידות חובה.
/// המחלקה משתתפת בשלב ניתוח קושי, שיבוץ ופותר אילוצים.
/// </summary>
/// <remarks>
/// מימוש Adjacency List — רשימת שכנויות: לכל יחידה HashSet של יחידות אסורות.
/// </remarks>
public sealed class ConflictGraph
{
    /// <summary>
    /// תפקיד: מאתחל גרף ממילון שכנויות.
    /// </summary>
    /// <param name="adjacency">מיפוי מזהה יחידה לקבוצת יחידות שסותרות אותה.</param>
    /// <remarks>נקרא מ- <see cref="ConflictGraphBuilder.Build"/> בלבד.</remarks>
    public ConflictGraph(IReadOnlyDictionary<int, HashSet<int>> adjacency)
    {
        // Adjacency — מילון read-only; HashSet לכל צומת = שכנים (יחידות אסורות).
        Adjacency = adjacency;
    }

    /// <summary>
    /// תפקיד: מיפוי מזהה יחידה ליחידות שסותרות אותה (קשתות דו-כיווניות).
    /// </summary>
    /// <remarks>נקרא מ- ProblemDifficultyAnalyzer, SolverInputBuilder.</remarks>
    public IReadOnlyDictionary<int, HashSet<int>> Adjacency { get; }
}
