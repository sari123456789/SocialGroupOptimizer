namespace MyProject.Data.Models;

/// <summary>
/// סיווג בפועל של משתתף בריצת שיבוץ אחת: ערך אחד לכל מימד.
/// </summary>
/// <remarks>
/// <para>לא מקושר ישירות ל־<c>ParticipantId</c> — אלא ל־<see cref="ParticipantAssignmentId"/> (משתתף בתוך שיבוץ).</para>
/// <para>מפתח ראשי: (<see cref="ParticipantAssignmentId"/>, <see cref="ClassificationDimensionId"/>) — מונע שתי רמות שונות באותו מימד.</para>
/// <para><see cref="ClassificationLevelId"/> חייב להשתייך לאותו מימד כמו <see cref="ClassificationDimensionId"/>.</para>
/// </remarks>
public class ParticipantClassification
{
    /// <summary>שורת "משתתף בשיבוץ" — מקשרת לאדם ולריצת שיבוץ.</summary>
    public int ParticipantAssignmentId { get; set; }

    /// <summary>איזה מימד (עמודה) מסווגים.</summary>
    public int ClassificationDimensionId { get; set; }

    /// <summary>איזו רמה (ערך תא) נבחרה במימד הזה.</summary>
    public int ClassificationLevelId { get; set; }
}
