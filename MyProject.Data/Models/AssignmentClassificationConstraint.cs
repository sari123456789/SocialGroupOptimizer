namespace MyProject.Data.Models;

/// <summary>
/// הגדרת אילוץ סיווג על מימד מסוים בריצת שיבוץ.
/// </summary>
/// <remarks>
/// <para>מצביע על <see cref="ClassificationDimensionId"/> — לא על רמה בודדת.</para>
/// <para><see cref="IsBalanceOrSeparation"/> == true → אילוץ איזון יחסי בליבה; false → הפרדה הומוגנית בקבוצות.</para>
/// <para>רשימת הרמות לאילוץ נגזרת מכל ה־<c>ClassificationLevels</c> של אותו מימד.</para>
/// </remarks>
public class AssignmentClassificationConstraint
{
    public int AssignmentId { get; set; }

    public int ClassificationDimensionId { get; set; }

    public bool IsBalanceOrSeparation { get; set; }
}
