namespace MyProject.SolverService.Contracts;

/// <summary>
/// סוג יחידת שיבוץ — משתתף בודד או יחידת חובה (כמה משתתפים שחייבים להיות יחד).
/// </summary>
public enum PlacementUnitKindWire
{
    /// <summary>משתתף יחיד — אין לו זוגות MustLink.</summary>
    SingleParticipant,

    /// <summary>יחידת חובה — לפחות שני משתתפים, לא ניתן לפצל.</summary>
    MandatoryUnit,
}
