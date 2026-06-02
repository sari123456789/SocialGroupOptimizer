namespace MyProject.BL.Algorithm.InitialPlacement.Solver.Models;

/// <summary>
/// תפקיד: תשובה פנימית מהפותר החיצוני — סטטוס, שיוכים, שגיאות ודגל timeout.
/// </summary>
/// <remarks>נוצר ע"י <see cref="ExternalSolverClient"/>; נצרך ע"י <see cref="ExternalSolverFallback"/>.</remarks>
public sealed class SolverResponse
{
    /// <summary>
    /// תפקיד: בנאי תאימות — שיוך לפי משתתפים (פורמט היסטורי).
    /// </summary>
    /// <param name="Status">סטטוס התשובה.</param>
    /// <param name="ParticipantGroupAssignments">מיפוי משתתף → קבוצה.</param>
    /// <param name="Errors">שגיאות מהפותר.</param>
    /// <param name="IsTimeout">האם הקריאה נכשלה עקב timeout.</param>
    /// <remarks>נקרא מבדיקות _solver_validation (StubExternalSolverClient).</remarks>
    public SolverResponse(
        SolverResponseStatus Status,
        IReadOnlyDictionary<string, int> ParticipantGroupAssignments,
        IReadOnlyList<string> Errors,
        bool IsTimeout)
    {
        this.Status = Status;
        this.ParticipantGroupAssignments = ParticipantGroupAssignments ?? throw new ArgumentNullException(nameof(ParticipantGroupAssignments));
        this.Errors = Errors ?? throw new ArgumentNullException(nameof(Errors));
        this.IsTimeout = IsTimeout;
        this.UnitGroupAssignments = new Dictionary<int, int>();
    }

    /// <summary>
    /// תפקיד: בנאי ראשי — שיוך לפי יחידות שיבוץ (פורמט נוכחי).
    /// </summary>
    /// <param name="status">סטטוס התשובה.</param>
    /// <param name="UnitGroupAssignments">מיפוי יחידה → קבוצה.</param>
    /// <param name="errors">שגיאות מהפותר.</param>
    /// <param name="isTimeout">האם הקריאה נכשלה עקב timeout.</param>
    /// <remarks>נקרא מ- <see cref="ExternalSolverClient.MapWireStatus"/>.</remarks>
    public SolverResponse(
        SolverResponseStatus status,
        IReadOnlyDictionary<int, int> UnitGroupAssignments,
        IReadOnlyList<string> errors,
        bool isTimeout)
    {
        Status = status;
        this.UnitGroupAssignments = UnitGroupAssignments ?? throw new ArgumentNullException(nameof(UnitGroupAssignments));
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        IsTimeout = isTimeout;
        ParticipantGroupAssignments = new Dictionary<string, int>();
    }

    /// <summary>
    /// תפקיד: סטטוס כללי של תשובת הפותר.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="ExternalSolverFallback.TrySolve"/>, <see cref="SolverResultMapper"/>.</remarks>
    public SolverResponseStatus Status { get; }

    /// <summary>
    /// תפקיד: שיוך יחידות שיבוץ לקבוצות — פורמט מועדף.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="SolverResultMapper.TryMapAssignment"/>.</remarks>
    public IReadOnlyDictionary<int, int> UnitGroupAssignments { get; }

    /// <summary>
    /// תפקיד: שיוך משתתפים לקבוצות — תאימות לאחור כשאין שיוך לפי יחידות.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="SolverResultMapper.TryResolveUnitGroupAssignments"/>.</remarks>
    public IReadOnlyDictionary<string, int> ParticipantGroupAssignments { get; }

    /// <summary>
    /// תפקיד: שגיאות שהוחזרו מהפותר.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="ExternalSolverFallback.TrySolve"/>.</remarks>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// תפקיד: מציין האם הקריאה נכשלה עקב חריגת זמן.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="ExternalSolverFallback.TrySolve"/> — מוביל ל-UnknownTimeout.</remarks>
    public bool IsTimeout { get; }
}

/// <summary>
/// תפקיד: סטטוס תשובת הפותר החיצוני.
/// </summary>
public enum SolverResponseStatus
{
    /// <summary>נמצאה חלוקה חוקית.</summary>
    Success,

    /// <summary>הבעיה לא ניתנת לפתרון.</summary>
    Infeasible,

    /// <summary>כישלון כללי (תקשורת, קלט לא תקין, timeout).</summary>
    Failed,
}
