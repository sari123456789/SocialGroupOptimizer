using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;
using MyProject.BL.Algorithm.LocalSearch.Engine;

namespace MyProject.BL.Algorithm.Improvement;

/// <summary>
/// תוצאת שיפור חלוקה — חלוקה סופית ומטא-נתונים מ-Local Search.
/// </summary>
public sealed class AssignmentImprovementResult
{
    private AssignmentImprovementResult(
        Assignment assignment,
        bool localSearchRan,
        bool hadImprovement,
        LocalSearchResult? searchResult,
        Score? initialScore,
        Score? finalScore,
        GroupRebalanceDiagnostics? groupRebalanceDiagnostics = null)
    {
        Assignment = assignment ?? throw new ArgumentNullException(nameof(assignment));
        LocalSearchRan = localSearchRan;
        HadImprovement = hadImprovement;
        SearchResult = searchResult;
        InitialScore = initialScore;
        FinalScore = finalScore;
        GroupRebalanceDiagnostics = groupRebalanceDiagnostics;
    }

    /// <summary>חלוקה סופית — משופרת או מקורית.</summary>
    public Assignment Assignment { get; }

    /// <summary>האם בוצע חיפוש מקומי (לא כובה בהגדרות).</summary>
    public bool LocalSearchRan { get; }

    /// <summary>האם הציון הסופי גבוה מההתחלתי.</summary>
    public bool HadImprovement { get; }

    /// <summary>תוצאת Local Search מלאה — null אם דולג.</summary>
    public LocalSearchResult? SearchResult { get; }

    /// <summary>ציון לפני Local Search.</summary>
    public Score? InitialScore { get; }

    /// <summary>ציון אחרי Local Search — null אם דולג.</summary>
    public Score? FinalScore { get; }

    /// <summary>אבחון GroupRebalance fallback — null אם Local Search כובה.</summary>
    public GroupRebalanceDiagnostics? GroupRebalanceDiagnostics { get; }

    /// <summary>חיפוש מקומי לא רץ — מחזיר חלוקה מקורית.</summary>
    public static AssignmentImprovementResult Skipped(Assignment assignment, Score initialScore)
    {
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        return new AssignmentImprovementResult(
            assignment,
            localSearchRan: false,
            hadImprovement: false,
            searchResult: null,
            initialScore,
            finalScore: initialScore);
    }

    /// <summary>חיפוש מקומי הסתיים — חלוקה מומרת מ-FinalState.</summary>
    public static AssignmentImprovementResult Improved(Assignment assignment, LocalSearchResult searchResult)
    {
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        if (searchResult is null)
        {
            throw new ArgumentNullException(nameof(searchResult));
        }

        return Improved(assignment, searchResult, searchResult.InitialScore);
    }

    /// <summary>
    /// חיפוש מקומי הסתיים — כולל fallback של GroupRebalance שקוף בחלוקה המוחזרת.
    /// <paramref name="overallInitialScore"/> הוא הציון לפני כל שלבי השיפור.
    /// </summary>
    public static AssignmentImprovementResult Improved(
        Assignment assignment,
        LocalSearchResult searchResult,
        Score overallInitialScore,
        GroupRebalanceDiagnostics? groupRebalanceDiagnostics = null)
    {
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        if (searchResult is null)
        {
            throw new ArgumentNullException(nameof(searchResult));
        }

        return new AssignmentImprovementResult(
            assignment,
            localSearchRan: true,
            hadImprovement: searchResult.FinalScore.Value > overallInitialScore.Value,
            searchResult,
            overallInitialScore,
            searchResult.FinalScore,
            groupRebalanceDiagnostics);
    }
}
