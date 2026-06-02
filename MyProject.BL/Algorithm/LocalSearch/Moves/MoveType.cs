namespace MyProject.BL.Algorithm.LocalSearch.Moves;

/// <summary>
/// סוג הצעת שינוי במנוע ההחלפה.
/// </summary>
/// <remarks>
/// <para>תפקיד: סיווג move לצורך dispatch ב-<see cref="State.AssignmentStateUpdater"/>.</para>
/// <para>נקרא מ-: <see cref="MoveCandidate"/>, <see cref="State.AssignmentStateUpdater.ApplyMoveInPlace"/>.</para>
/// </remarks>
public enum MoveType
{
    /// <summary>
    /// החלפה — שני משתתפים מקבוצות שונות מחליפים קבוצות.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="MoveCandidate.Swap"/>, <see cref="State.AssignmentStateUpdater.ApplySwapInPlace"/>.</para>
    /// </remarks>
    Swap,

    /// <summary>
    /// העברה — משתתף בודד עובר מקבוצת מקור לקבוצת יעד.
    /// </summary>
    /// <remarks>
    /// <para>נקרא מ-: <see cref="MoveCandidate.Transfer"/>, <see cref="State.AssignmentStateUpdater.ApplyTransferInPlace"/>.</para>
    /// </remarks>
    Transfer,
}
