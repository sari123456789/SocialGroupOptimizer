using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// תפקיד: מיפוי דו-כיווני בין משתתפים ליחידות חובה — מאפשר לשאול לאיזו יחידה שייך משתתף ולקבל את כל חברי יחידה.
/// </summary>
/// <remarks>
/// נוצר ע"י <see cref="MandatoryGroupBuilder.Build"/> ומועבר לאורך כל זרימת ההצבה הראשונית.
/// </remarks>
public sealed class MandatoryUnitMap
{
    /// <summary>
    /// תפקיד: מאתחל מיפוי יחידות חובה ממילונים מוכנים.
    /// </summary>
    /// <param name="unitIdByParticipant">מיפוי מזהה משתתף למזהה יחידת חובה.</param>
    /// <param name="units">מיפוי מזהה יחידה לרשימת משתתפיה.</param>
    /// <remarks>נקרא מ- <see cref="MandatoryGroupBuilder.Build"/> בלבד.</remarks>
    public MandatoryUnitMap(
        IReadOnlyDictionary<ParticipantId, int> unitIdByParticipant,
        IReadOnlyDictionary<int, List<ParticipantId>> units)
    {
        UnitIdByParticipant = unitIdByParticipant;
        Units = units;
    }

    /// <summary>
    /// תפקיד: מחזיר את כל יחידות החובה וחבריהן.
    /// </summary>
    /// <remarks>
    /// נקרא מ- <see cref="GreedyPlacementBuilder"/>, <see cref="LocalRepairEngine"/>,
    /// <see cref="SolverInputBuilder"/>, <see cref="FeasibilityPreChecker"/>,
    /// <see cref="InfeasibilityExplanationBuilder"/>, <see cref="ProblemDifficultyAnalyzer"/>.
    /// </remarks>
    public IReadOnlyDictionary<int, List<ParticipantId>> Units { get; }

    private IReadOnlyDictionary<ParticipantId, int> UnitIdByParticipant { get; }

    /// <summary>
    /// תפקיד: מחפש את מזהה יחידת החובה של משתתף נתון.
    /// </summary>
    /// <param name="participantId">מזהה המשתתף לחיפוש.</param>
    /// <param name="unitId">מזהה היחידה — מוגדר רק כשהחיפוש הצליח.</param>
    /// <returns>true אם המשתתף נמצא במיפוי; false אחרת.</returns>
    /// <remarks>
    /// נקרא מ- <see cref="ConflictGraphBuilder.Build"/>, <see cref="FeasibilityPreChecker"/>,
    /// <see cref="GreedyPlacementBuilder"/>, <see cref="LocalRepairEngine"/>.
    /// </remarks>
    public bool TryGetUnitId(ParticipantId participantId, out int unitId) =>
        // false = משתתף בודד (לא ביחידת חובה); true = unitId מצביע על רשימה ב-Units.
        UnitIdByParticipant.TryGetValue(participantId, out unitId);
}
