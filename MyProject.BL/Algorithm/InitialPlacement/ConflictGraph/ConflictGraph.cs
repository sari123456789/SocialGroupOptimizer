namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// תפקיד: גרף קונפליקטים לא-מכוון בין יחידות חובה — קשת = זוג איסור בין שתי יחידות.
/// </summary>
/// <remarks>
/// נבנה ע"י <see cref="ConflictGraphBuilder.Build"/>.
/// נצרך ע"י ProblemDifficultyAnalyzer, SolverInputBuilder, ISolverFallback.
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
