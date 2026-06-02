namespace MyProject.BL.Algorithm.InitialPlacement.Difficulty;

/// <summary>
/// תפקיד: פרופיל קושי של בעיית החלוקה — מדדים משוקללים להחלטת Greedy מול Solver.
/// </summary>
/// <remarks>
/// נוצר ע"י <see cref="ProblemDifficultyAnalyzer.Analyze"/>; נצרך ע"י <see cref="InitialPlacementStrategyDecider"/>.
/// CapacitySlack קרוב לאפס = קשיחות גבוהה; HasLargeIsland = מורכבות קומבינטורית.
/// </remarks>
public sealed record ProblemDifficultyProfile
{
    /// <summary>
    /// תפקיד: מאתחל פרופיל קושי עם ולידציה של ציון הקושי.
    /// </summary>
    /// <param name="constraintDensity">צפיפות אילוצים (חובה+אסור)/משתתפים.</param>
    /// <param name="capacitySlack">מרחב תמרון בקיבולת.</param>
    /// <param name="isRigid">האם החלוקה קשיחה (slack נמוך).</param>
    /// <param name="largestIslandSize">גודל הרכיב הקשיר הגדול בגרף.</param>
    /// <param name="hasLargeIsland">האם יש "אי" גדול (&gt; מחצית הצמתים).</param>
    /// <param name="difficultyScore">ציון משוקלל 0–100.</param>
    /// <remarks>נקרא מ- <see cref="ProblemDifficultyAnalyzer.Analyze"/> בלבד.</remarks>
    public ProblemDifficultyProfile(
        double constraintDensity,
        double capacitySlack,
        bool isRigid,
        int largestIslandSize,
        bool hasLargeIsland,
        double difficultyScore)
    {
        ConstraintDensity = constraintDensity;
        CapacitySlack = capacitySlack;
        IsRigid = isRigid;
        LargestIslandSize = largestIslandSize;
        HasLargeIsland = hasLargeIsland;
        // ולידציה: ציון הקושי חייב להיות בטווח 0–100.
        if (difficultyScore < 0.0 || difficultyScore > 100.0)
        {
            throw new ArgumentOutOfRangeException(nameof(difficultyScore), "Difficulty score must be between 0 and 100.");
        }

        DifficultyScore = difficultyScore;
    }

    /// <summary>
    /// תפקיד: צפיפות אילוצים — (זוגות חובה + אסור) חלקי מספר משתתפים.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="InitialPlacementStrategyDecider"/> דרך DifficultyScore.</remarks>
    public double ConstraintDensity { get; }

    /// <summary>
    /// תפקיד: מרחב תמרון — (קיבולת מקס − משתתפים) / קיבולת מקס; קרוב ל-0 = קשיח.
    /// </summary>
    /// <remarks>נכלל בחישוב DifficultyScore; אין קריאה ישירה מחוץ ל-analyzer.</remarks>
    public double CapacitySlack { get; }

    /// <summary>
    /// תפקיד: האם החלוקה קשיחה — CapacitySlack מתחת לסף 0.05.
    /// </summary>
    /// <remarks>אין קריאות בייצור — מידע אנליטי בפרופיל.</remarks>
    public bool IsRigid { get; }

    /// <summary>
    /// תפקיד: גודל הרכיב הקשיר הגדול ביותר בגרף הקונפליקטים.
    /// </summary>
    /// <remarks>נכלל בחישוב DifficultyScore.</remarks>
    public int LargestIslandSize { get; }

    /// <summary>
    /// תפקיד: האם קיים רכיב קשיר &gt; מחצית הצמתים — סימן למורכבות קומבינטורית.
    /// </summary>
    /// <remarks>אין קריאות בייצור — מידע אנליטי בפרופיל.</remarks>
    public bool HasLargeIsland { get; }

    /// <summary>
    /// תפקיד: ציון קושי משוקלל בטווח 0–100 — קובע GreedyFirst מול SolverFirst.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="InitialPlacementStrategyDecider.Decide"/>.</remarks>
    public double DifficultyScore { get; }
}
