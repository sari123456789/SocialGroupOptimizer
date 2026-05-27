namespace MyProject.Data.Models;

/// <summary>
/// רמת ערך אפשרית בתוך מימד — מתאים לתוכן תא באקסל (למשל "מתחיל" תחת מימד "רמה").
/// </summary>
/// <remarks>
/// שייך תמיד למימד אחד דרך <see cref="ClassificationDimensionId"/>.
/// ייחודיות: אותו <see cref="LevelCode"/> לא יכול להופיע פעמיים באותו מימד.
/// </remarks>
public class ClassificationLevel
{
    /// <summary>מפתח מסד פנימי.</summary>
    public int ClassificationLevelId { get; set; }

    /// <summary>מימד האב.</summary>
    public int ClassificationDimensionId { get; set; }

    /// <summary>קוד טקסטואלי שממופה ל־<c>ClassificationLevelCode</c> בליבה.</summary>
    public string LevelCode { get; set; } = string.Empty;
}
