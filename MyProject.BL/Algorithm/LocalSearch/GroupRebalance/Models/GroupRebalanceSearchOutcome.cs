namespace MyProject.BL.Algorithm.LocalSearch.GroupRebalance.Models;

/// <summary>
/// תוצאת מעבר חיפוש אחד של איזון קבוצות — המועמד הטוב ביותר וסטטיסטיקות הערכה.
/// </summary>
public sealed class GroupRebalanceSearchOutcome
{
    public GroupRebalanceSearchOutcome(
        GroupRebalanceResult? bestResult,
        int splitClusterCount,
        int candidatesEvaluated)
    {
        BestResult = bestResult;
        SplitClusterCount = splitClusterCount;
        CandidatesEvaluated = candidatesEvaluated;
    }

    public GroupRebalanceResult? BestResult { get; }

    public int SplitClusterCount { get; }

    public int CandidatesEvaluated { get; }
}
