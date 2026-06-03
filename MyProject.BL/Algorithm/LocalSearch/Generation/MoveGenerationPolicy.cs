using MyProject.BL.Algorithm.LocalSearch.Moves;
using MyProject.BL.Algorithm.LocalSearch.RuntimeData;
using MyProject.BL.Logic.Configuration;

namespace MyProject.BL.Algorithm.LocalSearch.Generation;

/// <summary>
/// מדיניות יצירת מועמדים — בוחרת מקורות אסטרטגיה לפי יחסים וסטגנציה.
/// </summary>
/// <remarks>
/// <para>תפקיד: בחירת מקורות בלבד — לא ייצור, אימות, ניקוד או ביצוע move.</para>
/// <para>ספים נלקחים מ-<see cref="AlgorithmSettings"/>; אין מספרים קבועים במחלקה.</para>
/// </remarks>
public sealed class MoveGenerationPolicy
{
    private readonly IReadOnlyList<IMoveCandidateStrategy> _availableStrategies;
    private readonly AlgorithmSettings _settings;

    /// <summary>
    /// יוצר מדיניות עם מאגר אסטרטגיות והגדרות ספים.
    /// </summary>
    public MoveGenerationPolicy(
        IEnumerable<IMoveCandidateStrategy> availableStrategies,
        AlgorithmSettings settings)
    {
        if (availableStrategies is null)
        {
            throw new ArgumentNullException(nameof(availableStrategies));
        }

        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _availableStrategies = availableStrategies.ToList();
    }

    /// <summary>
    /// בוחר את האסטרטגיות הפעילות למצב הנוכחי.
    /// </summary>
    public IReadOnlyList<IMoveCandidateStrategy> SelectStrategies(
        RuntimeDataSnapshot snapshot,
        MoveGenerationContext context)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var desiredSources = BuildDesiredSources(context);

        var selected = _availableStrategies
            .Where(strategy => desiredSources.Contains(strategy.Source))
            .Where(strategy => strategy.IsApplicable(snapshot, context))
            .ToList();

        if (selected.Count > 0)
        {
            return selected;
        }

        var lowContributionFallback = _availableStrategies
            .Where(strategy => strategy.Source == CandidateSource.LowContribution)
            .Where(strategy => strategy.IsApplicable(snapshot, context))
            .ToList();

        if (lowContributionFallback.Count > 0)
        {
            return lowContributionFallback;
        }

        return _availableStrategies
            .Where(strategy => strategy.IsApplicable(snapshot, context))
            .ToList();
    }

    /// <summary>
    /// בונה את קבוצת מקורות המועמדים לפי יחסים וסטגנציה.
    /// </summary>
    private HashSet<CandidateSource> BuildDesiredSources(MoveGenerationContext context)
    {
        var sources = new HashSet<CandidateSource>();

        if (context.IsolatedParticipantRatio >= _settings.HighIsolationRatioThreshold)
        {
            sources.Add(CandidateSource.IsolatedParticipant);
        }

        if (context.WeakGroupRatio >= _settings.HighWeakGroupRatioThreshold)
        {
            sources.Add(CandidateSource.LowScoreGroup);
        }

        if (context.IterationsSinceImprovement >= _settings.LightStagnationThreshold)
        {
            sources.Add(CandidateSource.NearMiss);
        }

        if (context.IterationsSinceImprovement >= _settings.HeavyStagnationThreshold)
        {
            sources.Add(CandidateSource.ControlledRandom);
        }

        if (sources.Count == 0)
        {
            sources.Add(CandidateSource.LowContribution);
        }

        return sources;
    }
}
