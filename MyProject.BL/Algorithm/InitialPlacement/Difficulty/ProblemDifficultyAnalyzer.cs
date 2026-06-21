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
    private const double RigiditySlackThreshold = 0.05;//מרווח קיבולת קטן מ5%

    private const double LargeIslandFraction = 0.5;//רכיב קשיר בגרף הקונפליקטים גדול מחצי מגודל הגרף
    private const double MaxDensityForNormalization = 2.0;//משמעות הערך: צפיפות של 2 אילוצים למשתתף נחשבת גבוהה מאוד, ומעליה לא מגדילה את הציון.
    private const double DensityWeight = 0.4;//צפיפות אילוצים. 0.4 = 40% מהציון הסופי.
    private const double RigidityWeight = 0.3;//קשיחות קיבולת. 0.3 = 30% מהציון הסופי.
    private const double LargestIslandWeight = 0.3;//גודל רכיב קונפליט גדול 0.3 = 30% מהציון הסופי.

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
        var mandatoryCount = input.Constraints.OfType<MandatoryPairConstraint>().Count(); //כמה זוגות חובה
        var forbiddenCount = input.Constraints.OfType<ForbiddenPairConstraint>().Count();//כמה זוגות אסורים

        // אחוז אילוצי חובה ואסור - צפיפות אילוצים.
        var constraintDensity = n > 0
            ? (double)(mandatoryCount + forbiddenCount) / n //5+8/100=0.13
            : 0.0;

        var capacitySlack = ComputeCapacitySlack(input, n);//חישוב מרווח קיבולת
        var isRigid = capacitySlack < RigiditySlackThreshold;//אם מרווח הקיבולת קטן מ-5% => קשיח

        var largestIslandSize = ComputeLargestIsland(conflictGraph);//גודל הרכיב הקשיר הגדול ביותר בגרף הקונפליקטים
        var totalNodes = conflictGraph.Adjacency.Count;//מספר הצמתים בגרף הקונפליקטים
        // "אי גדול" מוגדר כרכיב שגדול מחצי מהגרף.
        var hasLargeIsland = totalNodes > 0 && largestIslandSize > totalNodes * LargeIslandFraction;//TRUE/FALRE
        var difficultyScore = ComputeDifficultyScore(constraintDensity, capacitySlack, largestIslandSize, totalNodes);

        return new ProblemDifficultyProfile(
            constraintDensity,
            capacitySlack,
            isRigid,
            largestIslandSize,
            hasLargeIsland,
            difficultyScore);
    }

    /// <summary>
    /// תפקיד הפונקציה: מחשבת מרווח קיבולת — Capacity Slack — יחס מקום פנוי לקיבולת מקסימלית.
    /// קלט עיקרי: קלט שיבוץ, מספר משתתפים.
    /// פלט עיקרי: ערך 0..1 — נמוך = בעיה קשיחה יותר.
    /// </summary>
    private static double ComputeCapacitySlack(InitialPlacementInput input, int participantCount)
    {
        var groupSizeConstraints = input.Constraints.OfType<GroupSizeConstraint>().ToList();
        if (groupSizeConstraints.Count == 0)
        {
            return 1.0;//אין אילוצי גודל קבוצה — מרווח מלא.
        }

        var totalMaxCapacity = groupSizeConstraints.Sum(c => c.MaxCapacity.Value);//סכימת כל הקיבולות המקסימליות של כל קבוצות הגודל.
        if (totalMaxCapacity == 0)
        {
            return 0.0;//אין מקום בכלל — מרווח אפס.
        }

        var slack = (double)(totalMaxCapacity - participantCount) / totalMaxCapacity;//קיבולת מקסימום - כמות משתתפים / לקיבולת מקסימום = אחוז מקום פנוי.
        // אם נוצר ערך שלילי, מהדקים ל-0 כדי לשמור על טווח סביר.
        return Math.Max(0.0, slack);
    }

    /// <summary>
    /// תפקיד הפונקציה: מחשבת גודל הרכיב הקשיר הגדול ביותר בגרף הקונפליקטים.
    /// קלט עיקרי: ConflictGraph — גרף קונפליקטים.
    /// פלט עיקרי: מספר יחידות ברכיב הגדול ביותר.
    /// </summary>
    private static int ComputeLargestIsland(ConflictGraph conflictGraph)
    {
        var adjacency = conflictGraph.Adjacency;//מפה של כל צומת לשכנים שלו בגרף הקונפליקטים.
        if (adjacency.Count == 0)//אין צמתים בגרף — אין רכיבים קשירים.
        {
            return 0;
        }

        var visited = new HashSet<int>();
        var largestComponent = 0;

        foreach (var startNode in adjacency.Keys)
        {
            if (visited.Contains(startNode))//אם הצומת כבר בוקר, דילוג על רכיב זה
            {
                continue;
            }

            var componentSize = BfsComponentSize(adjacency, startNode, visited);//ביצוע BFS כדי לספור את גודל הרכיב הקשיר שמתחיל בצומת זה.
            if (componentSize > largestComponent)
            {
                largestComponent = componentSize;
            }
        }

        return largestComponent;
    }
    /// <summary>
    /// תפקיד הפונקציה: מבצעת חיפוש רוחב (BFS) כדי לספור גודל רכיב קשיר בגרף.
    /// </summary>
    /// <param name="adjacency"></param>
    /// <param name="start"></param>
    /// <param name="visited"></param>
    /// <returns></returns>
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

    /// <summary>
    /// תפקיד הפונקציה: משקללת מדדי קושי לציון יחיד DifficultyScore (0–100).
    /// קלט עיקרי: צפיפות אילוצים, slack, גודל רכיב מרכזי.
    /// פלט עיקרי: ציון קושי מעוגל.
    /// </summary>
    private static double ComputeDifficultyScore(
        double constraintDensity,//צפיפות אילוצי חובה ואסור למשתתף
        double capacitySlack,//מרווח קיבולת
        int largestIslandSize,//גודל הרכיב הקשיר הגדול ביותר בגרף הקונפליקטים
        int totalNodes)//מספר הצמתים בגרף הקונפליקטים
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

        // 
        return value > max ? max : value;
    }
}
