using MyProject.BL.Logic.Configuration;

// סינטקס: namespace מגדיר מרחב שמות לקובץ; internal = נגיש רק בתוך MyProject.API.
namespace MyProject.API.Configuration;

// סינטקס: static class — אין מופעים; רק מתודות סטטיות. לוגיקה: עוזר רישום DI.
internal static class AlgorithmSettingsRegistration
{
    // סינטקס: public static מחזיר AlgorithmSettings; פרמטר IConfiguration = הגדרות ASP.NET.
    // לוגיקה: קורא appsettings ומייצר אובייקט הגדרות לאלגוריתם.
    public static AlgorithmSettings BindFromConfiguration(IConfiguration configuration)
    {
        // סינטקס: GetSection("Algorithm") — משנה JSON תחת המפתח Algorithm.
        var section = configuration.GetSection("Algorithm");

        // סינטקס: return new AlgorithmSettings(...) — קריאה לבנאי עם ערכים מקונפיגורציה או ברירת מחדל.
        // לוגיקה: כל GetValue קורא מפתח; אם חסר — משתמש ב-Default* מהמחלקה.
        return new AlgorithmSettings(
            // סינטקס: nameof(AlgorithmSettings.MaxIterations) = מחרוזת "MaxIterations" בטוחה לריפקטור.
            section.GetValue(nameof(AlgorithmSettings.MaxIterations), AlgorithmSettings.DefaultMaxIterations),
            section.GetValue(nameof(AlgorithmSettings.CandidateCount), AlgorithmSettings.DefaultCandidateCount),
            section.GetValue(nameof(AlgorithmSettings.RepairAttempts), AlgorithmSettings.DefaultRepairAttempts),
            section.GetValue(nameof(AlgorithmSettings.SolverDifficultyThreshold), AlgorithmSettings.DefaultSolverDifficultyThreshold),
            section.GetValue(nameof(AlgorithmSettings.SolverTimeoutMs), AlgorithmSettings.DefaultSolverTimeoutMs),
            // סינטקס: ! אחרי GetValue — null-forgiving; אומר לקומפיילר שהערך לא null (URL חובה).
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
                AlgorithmSettings.DefaultEnableLocalSearch),
            section.GetValue(
                nameof(AlgorithmSettings.MaxGroupRebalancePasses),
                AlgorithmSettings.DefaultMaxGroupRebalancePasses));
    }
}