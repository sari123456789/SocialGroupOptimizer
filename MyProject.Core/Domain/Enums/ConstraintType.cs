namespace MyProject.Core.Domain.Enums;

/// <summary>
/// קטגוריות אילוצים קשים נתמכות בדומיין.
/// </summary>
public enum ConstraintType
{
    /// <summary>
    /// אילוץ על גודל קבוצה (מינימום/מקסימום משתתפים).
    /// </summary>
    GroupSize = 1,

    /// <summary>
    /// דורש שמשתתפים יוקצו לאותה קבוצה.
    /// </summary>
    MustLink = 2,

    /// <summary>
    /// אוסר שמשתתפים יוקצו לאותה קבוצה.
    /// </summary>
    CannotLink = 3,

    /// <summary>
    /// אילוץ על איזון/חלוקת סיווגים בין קבוצות.
    /// </summary>
    ClassificationBalance = 4,

    /// <summary>
    /// אילוץ על מספר הקבוצות הנדרש בהקצאה.
    /// </summary>
    GroupCount = 5,

    /// <summary>
    /// אילוץ על שמירת יחסי רמות מימד (מאפיינים בלעדיים) בתוך כל קבוצה בהתאם לחלוקה הגלובלית.
    /// </summary>
    ClassificationProportionalBalance = 6,

    /// <summary>
    /// אילוץ הפרדה: בכל קבוצה מותרת לכל היותר רמת מימד אחת מתוך קבוצת הרמות.
    /// </summary>
    ClassificationHomogeneousGroup = 7
}
