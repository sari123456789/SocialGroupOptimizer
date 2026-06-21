namespace MyProject.BL.Transparency;

/// <summary>
/// קודי סיבה אחידים להסבר שיבוץ — מאפשרים תרגום עקבי ל-UI.
/// </summary>
/// <remarks>
/// כל קוד מתאר גורם אחד שתרם לשיבוץ הנוכחי או חוסם/משפיע על מעבר לקבוצה אחרת.
/// השכבה היא Read Only בלבד ואינה משנה מצב חלוקה.
/// </remarks>
public enum ExplanationReasonCode
{
    /// <summary>
    /// העדפה חברתית של המשתתף מומשה בקבוצה (מישהו שהוא ביקש נמצא איתו).
    /// </summary>
    SOCIAL_MATCH = 1,

    /// <summary>
    /// משתתפים אחרים בקבוצה ביקשו דווקא אותו.
    /// </summary>
    SOCIAL_REQUESTED = 2,

    /// <summary>
    /// השיבוץ מנע בידוד חברתי — קיים לפחות קשר חברתי אחד בקבוצה.
    /// </summary>
    ISOLATION_PREVENTED = 3,

    /// <summary>
    /// מעבר לקבוצה זו מפר זוג אסור.
    /// </summary>
    FORBIDDEN_CONSTRAINT = 4,

    /// <summary>
    /// מעבר לקבוצה זו מפריד זוג חובה.
    /// </summary>
    MANDATORY_PAIR = 5,

    /// <summary>
    /// מעבר לקבוצה זו חורג ממגבלת קיבולת/גודל.
    /// </summary>
    CAPACITY_LIMIT = 6,

    /// <summary>
    /// מעבר לקבוצה זו פוגע באיזון/הפרדת הסיווגים.
    /// </summary>
    CLASSIFICATION_BALANCE = 7,

    /// <summary>
    /// מעבר חוקי אך מוריד את ציון השיבוץ של המשתתף.
    /// </summary>
    SCORE_DECREASE = 8,

    /// <summary>
    /// הקבוצה הנוכחית היא הטובה ביותר עבור המשתתף — אין מעבר משפר.
    /// </summary>
    BETTER_CURRENT_GROUP = 9
}
