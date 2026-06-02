using System.Linq;

namespace MyProject.BL.Algorithm.InitialPlacement.Solver.Models;

/// <summary>
/// תפקיד: יחידת שיבוץ בבקשת פותר — משתתף יחיד או יחידת חובה (כמה משתתפים שחייבים להישאר יחד).
/// </summary>
/// <remarks>נוצר ע"י <see cref="SolverInputBuilder"/>; נשלח לשירות דרך <see cref="SolverJobWireMapper"/>.</remarks>
public sealed class PlacementUnitRequest
{
    /// <summary>
    /// תפקיד: בנאי תאימות — ממיר דגל בוליאני ל-<see cref="PlacementUnitKind"/>.
    /// </summary>
    /// <param name="unitId">מזהה יחידה.</param>
    /// <param name="participantIds">משתתפי היחידה.</param>
    /// <param name="isMandatoryUnit">true ליחידת חובה; false למשתתף בודד.</param>
    /// <remarks>אין שימושים בייצור — העדפה לבנאי עם <see cref="PlacementUnitKind"/>.</remarks>
    public PlacementUnitRequest(
        int unitId,
        IReadOnlyList<string> participantIds,
        bool isMandatoryUnit)
        : this(
            unitId,
            participantIds,
            isMandatoryUnit ? PlacementUnitKind.MandatoryUnit : PlacementUnitKind.SingleParticipant)
    {
    }

    /// <summary>
    /// תפקיד: מאתחל יחידת שיבוץ עם סוג מפורש.
    /// </summary>
    /// <param name="unitId">מזהה יחידה (לא שלילי).</param>
    /// <param name="participantIds">רשימת משתתפים (לא ריקה, ללא כפילויות).</param>
    /// <param name="kind">סוג היחידה — בודד או חובה.</param>
    /// <remarks>נקרא מ- <see cref="SolverInputBuilder"/>.</remarks>
    public PlacementUnitRequest(
        int unitId,
        IReadOnlyList<string> participantIds,
        PlacementUnitKind kind)
    {
        if (unitId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitId), "Unit id cannot be negative.");
        }

        if (participantIds is null)
        {
            throw new ArgumentNullException(nameof(participantIds));
        }

        if (participantIds.Count == 0)
        {
            throw new ArgumentException("Placement unit must contain at least one participant.", nameof(participantIds));
        }

        if (participantIds.Any(id => string.IsNullOrWhiteSpace(id)))
        {
            throw new ArgumentException("Placement unit cannot contain empty participant ids.", nameof(participantIds));
        }

        if (participantIds.Distinct(StringComparer.Ordinal).Count() != participantIds.Count)
        {
            throw new ArgumentException("Placement unit cannot contain duplicate participant ids.", nameof(participantIds));
        }

        UnitId = unitId;
        ParticipantIds = participantIds;
        Kind = kind;
    }

    /// <summary>
    /// תפקיד: מזהה ייחודי של יחידת השיבוץ.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="SolverResultMapper"/>, <see cref="SolverJobWireMapper"/>.</remarks>
    public int UnitId { get; }

    /// <summary>
    /// תפקיד: מזהי המשתתפים ביחידה.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="SolverResultMapper"/>, <see cref="SolverJobWireMapper"/>.</remarks>
    public IReadOnlyList<string> ParticipantIds { get; }

    /// <summary>
    /// תפקיד: סוג היחידה — בודד או חובה.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="SolverJobWireMapper"/> לתרגום wire.</remarks>
    public PlacementUnitKind Kind { get; }

    /// <summary>
    /// תפקיד: קיצור נוח — האם זו יחידת חובה.
    /// </summary>
    /// <remarks>אין שימושים בייצור — נגזר מ-<see cref="Kind"/>.</remarks>
    public bool IsMandatoryUnit => Kind == PlacementUnitKind.MandatoryUnit;
}

/// <summary>
/// תפקיד: סוג יחידת שיבוץ בבקשת פותר.
/// </summary>
public enum PlacementUnitKind
{
    /// <summary>משתתף בודד.</summary>
    SingleParticipant = 0,

    /// <summary>יחידת חובה — כמה משתתפים שחייבים להיות באותה קבוצה.</summary>
    MandatoryUnit = 1,
}
