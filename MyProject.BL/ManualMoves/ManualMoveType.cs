namespace MyProject.BL.ManualMoves;

/// <summary>
/// סוג מהלך ידני של מנהל לאחר חלוקה קיימת.
/// </summary>
public enum ManualMoveType
{
    /// <summary>
    /// העברת משתתף יחיד מקבוצתו לקבוצת יעד.
    /// </summary>
    Transfer = 1,

    /// <summary>
    /// החלפה הדדית בין שני משתתפים בקבוצותיהם.
    /// </summary>
    Swap = 2
}
