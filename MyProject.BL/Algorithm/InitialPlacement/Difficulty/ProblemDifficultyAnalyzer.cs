using MyProject.Core.Domain.Constraints;

namespace MyProject.BL.Algorithm.InitialPlacement.Difficulty;

/// <summary>
/// תפקיד: מנתח קושי בעיית החלוקה — מחשב צפיפות, slack, רכיבים קשירים וציון משוקלל.
/// </summary>
/// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator.Run"/> לפני בחירת אסטרטגיה.</remarks>
public static class ProblemDifficultyAnalyzer
{
    /// <summary>
    /// סף ל-Slack: מתחת לערך זה הפרופיל נחשב קשיח.
    /// </summary>
    private const double RigiditySlackThreshold = 0.05;

    /// <summary>
    /// סף ל"אי גדול": יותר ממחצית הצמתים בגרף.
    /// </summary>
    private const double LargeIslandFraction = 0.5;
    private const double MaxDensityForNormalization = 2.0;
    private const double DensityWeight = 0.4;
    private const double RigidityWeight = 0.3;
    private const double LargestIslandWeight = 0.3;

    /// <summary>
    /// תפקיד: מנתח קושי ומחזיר פרופיל מלא.
    /// </summary>
    /// <param name="input">קלט ההצבה הראשונית.</param>
    /// <param name="mandatoryUnits">יחידות חובה.</param>
    /// <param name="conflictGraph">גרף קונפליקטים.</param>
    /// <returns>פרופיל קושי עם DifficultyScore.</returns>
    /// <remarks>נקרא מ- <see cref="InitialPlacementOrchestrator.Run"/>, בדיקות _solver_validation.</remarks>
    public static ProblemDifficultyProfile Analyze(
        InitialPlacementInput input,
        MandatoryUnitMap mandatoryUnits,
        ConflictGraph conflictGraph)
    {
        // Analyze נקראת מתוך InitialPlacementOrchestrator.Run
        // בקובץ Orchestration/InitialPlacementOrchestrator.cs.
        // המטרה: לספק תמונת קושי מרוכזת לשימוש בהחלטות אלגוריתמיות.
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (mandatoryUnits is null)
        {
            throw new ArgumentNullException(nameof(mandatoryUnits));
        }

        if (conflictGraph is null)
        {
            throw new ArgumentNullException(nameof(conflictGraph));
        }

        var n = input.Participants.Count;
        var mandatoryCount = input.Constraints.OfType<MandatoryPairConstraint>().Count();
        var forbiddenCount = input.Constraints.OfType<ForbiddenPairConstraint>().Count();

        // תחביר תנאי מקוצר: מונעים חילוק באפס כשאין משתתפים.
        var constraintDensity = n > 0
            ? (double)(mandatoryCount + forbiddenCount) / n
            : 0.0;

        var capacitySlack = ComputeCapacitySlack(input, n);
        var isRigid = capacitySlack < RigiditySlackThreshold;

        var largestIslandSize = ComputeLargestIsland(conflictGraph);
        var totalNodes = conflictGraph.Adjacency.Count;
        // "אי גדול" מוגדר כרכיב שגדול מחצי מהגרף.
        var hasLargeIsland = totalNodes > 0 && largestIslandSize > totalNodes * LargeIslandFraction;
        var difficultyScore = ComputeDifficultyScore(constraintDensity, capacitySlack, largestIslandSize, totalNodes);

        return new ProblemDifficultyProfile(
            constraintDensity,
            capacitySlack,
            isRigid,
            largestIslandSize,
            hasLargeIsland,
            difficultyScore);
        // ProblemDifficultyProfile מוגדר בקובץ Difficulty/ProblemDifficultyProfile.cs
        // והוא אובייקט נתונים בלבד ללא לוגיקה.
    }

    private static double ComputeCapacitySlack(InitialPlacementInput input, int participantCount)
    {
        var groupSizeConstraints = input.Constraints.OfType<GroupSizeConstraint>().ToList();
        if (groupSizeConstraints.Count == 0)
        {
            return 1.0;
        }

        var totalMaxCapacity = groupSizeConstraints.Sum(c => c.MaxCapacity.Value);
        if (totalMaxCapacity == 0)
        {
            return 0.0;
        }

        var slack = (double)(totalMaxCapacity - participantCount) / totalMaxCapacity;
        // אם נוצר ערך שלילי, מהדקים ל-0 כדי לשמור על טווח סביר.
        return Math.Max(0.0, slack);
    }

    /// <summary>
    /// מחשב את גודל הרכיב הקשיר הגדול ביותר בגרף הקונפליקטים (BFS).
    /// </summary>
    private static int ComputeLargestIsland(ConflictGraph conflictGraph)
    {
        var adjacency = conflictGraph.Adjacency;
        if (adjacency.Count == 0)
        {
            return 0;
        }

        var visited = new HashSet<int>();
        var largestComponent = 0;

        foreach (var startNode in adjacency.Keys)
        {
            if (visited.Contains(startNode))
            {
                continue;
            }

            var componentSize = BfsComponentSize(adjacency, startNode, visited);
            if (componentSize > largestComponent)
            {
                largestComponent = componentSize;
            }
        }

        return largestComponent;
    }

    private static int BfsComponentSize(
        IReadOnlyDictionary<int, HashSet<int>> adjacency,
        int start,
        HashSet<int> visited)
    {
        var queue = new Queue<int>();
        queue.Enqueue(start);
        visited.Add(start);
        var count = 0;

        while (queue.Count > 0)
        {
            // BFS: מוציאים את הצומת הבא מהתור, ומרחיבים את כל השכנים שלו שטרם בוקרו.
            var node = queue.Dequeue();
            count++;

            if (!adjacency.TryGetValue(node, out var neighbors))
            {
                continue;
            }

            foreach (var neighbor in neighbors)
            {
                if (visited.Add(neighbor))
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        return count;
    }

    private static double ComputeDifficultyScore(
        double constraintDensity,
        double capacitySlack,
        int largestIslandSize,
        int totalNodes)
    {
        // נרמול כל מדד ל-0..100 ואז משקלול לפי המקדמים הקבועים.
        var densityScore = NormalizeToHundred(constraintDensity, MaxDensityForNormalization);
        var capacityRigidityScore = NormalizeToHundred(1.0 - Clamp01(capacitySlack), 1.0);

        var islandRatio = totalNodes > 0
            ? (double)largestIslandSize / totalNodes
            : 0.0;
        var largestIslandScore = NormalizeToHundred(islandRatio, 1.0);

        var weightedScore = densityScore * DensityWeight
            + capacityRigidityScore * RigidityWeight
            + largestIslandScore * LargestIslandWeight;

        return Math.Round(Clamp(weightedScore, 0.0, 100.0), 2);
    }

    private static double NormalizeToHundred(double value, double maxValue)
    {
        if (maxValue <= 0.0)
        {
            return 0.0;
        }

        // Clamp ל-[0,1] ואז כפל ב-100.
        return Clamp(value / maxValue, 0.0, 1.0) * 100.0;
    }

    private static double Clamp01(double value) => Clamp(value, 0.0, 1.0);

    private static double Clamp(double value, double min, double max)
    {
        if (value < min)
        {
            return min;
        }

        // ternary — הגבלה עליונה בלבד אם value > max.
        return value > max ? max : value;
    }
}
