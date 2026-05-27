namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// גרף קונפליקטים פשוט בין יחידות חובה.
/// </summary>
public sealed class ConflictGraph
{
    public ConflictGraph(IReadOnlyDictionary<int, HashSet<int>> adjacency)
    {
        Adjacency = adjacency;
    }

    /// <summary>
    /// מיפוי מזהה יחידה לרשימת יחידות שסותרות אותה.
    /// </summary>
    public IReadOnlyDictionary<int, HashSet<int>> Adjacency { get; }
}
