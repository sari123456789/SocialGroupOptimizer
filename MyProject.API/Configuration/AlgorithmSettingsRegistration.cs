using MyProject.BL.Logic.Configuration;

namespace MyProject.API.Configuration;

internal static class AlgorithmSettingsRegistration
{
    public static AlgorithmSettings BindFromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("Algorithm");

        return new AlgorithmSettings(
            section.GetValue(nameof(AlgorithmSettings.MaxIterations), AlgorithmSettings.DefaultMaxIterations),
            section.GetValue(nameof(AlgorithmSettings.CandidateCount), AlgorithmSettings.DefaultCandidateCount),
            section.GetValue(nameof(AlgorithmSettings.RepairAttempts), AlgorithmSettings.DefaultRepairAttempts),
            section.GetValue(nameof(AlgorithmSettings.SolverDifficultyThreshold), AlgorithmSettings.DefaultSolverDifficultyThreshold),
            section.GetValue(nameof(AlgorithmSettings.SolverTimeoutMs), AlgorithmSettings.DefaultSolverTimeoutMs),
            section.GetValue(nameof(AlgorithmSettings.SolverBaseUrl), AlgorithmSettings.DefaultSolverBaseUrl)!,
            section.GetValue(nameof(AlgorithmSettings.SolverPollIntervalMs), AlgorithmSettings.DefaultSolverPollIntervalMs),
            section.GetValue(
                nameof(AlgorithmSettings.ClassificationBalanceTolerancePercent),
                AlgorithmSettings.DefaultClassificationBalanceTolerancePercent),
            section.GetValue(
                nameof(AlgorithmSettings.HighIsolationRatioThreshold),
                AlgorithmSettings.DefaultHighIsolationRatioThreshold),
            section.GetValue(
                nameof(AlgorithmSettings.HighWeakGroupRatioThreshold),
                AlgorithmSettings.DefaultHighWeakGroupRatioThreshold),
            section.GetValue(
                nameof(AlgorithmSettings.LightStagnationThreshold),
                AlgorithmSettings.DefaultLightStagnationThreshold),
            section.GetValue(
                nameof(AlgorithmSettings.HeavyStagnationThreshold),
                AlgorithmSettings.DefaultHeavyStagnationThreshold),
            section.GetValue(
                nameof(AlgorithmSettings.EnableLocalSearch),
                AlgorithmSettings.DefaultEnableLocalSearch));
    }
}
