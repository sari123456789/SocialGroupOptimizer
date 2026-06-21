using System.Linq;

namespace MyProject.BL.Algorithm.InitialPlacement.Solver.Models;

/// <summary>
/// תפקיד: בקשה פנימית לפותר החיצוני — יחידות שיבוץ, קבוצות, זוגות אסורים ואילוצי סיווג.
/// </summary>
/// <remarks>נבנה ע"י <see cref="SolverInputBuilder"/>; נשלח דרך <see cref="ExternalSolverClient"/>.</remarks>
public sealed class SolverRequest
{
    /// <summary>
    /// תפקיד: בנאי ראשי — בונה רשימת משתתפים אוטומטית מיחידות השיבוץ.
    /// </summary>
    /// <param name="placementUnits">יחידות שיבוץ.</param>
    /// <param name="groups">קבוצות יעד עם גודל מינ/מקס.</param>
    /// <param name="forbiddenUnitPairs">זוגות יחידות שאסור לשבץ יחד.</param>
    /// <param name="classificationConstraints">אילוצי סיווג לפותר.</param>
    /// <remarks>נקרא מ- <see cref="SolverInputBuilder.TryBuild"/>.</remarks>
    public SolverRequest(
        IReadOnlyList<PlacementUnitRequest> placementUnits,
        IReadOnlyList<SolverGroupRequest> groups,
        IReadOnlyList<SolverForbiddenUnitPairRequest> forbiddenUnitPairs,
        IReadOnlyList<SolverClassificationConstraintRequest> classificationConstraints,
        int minGroups = 0)
    {
        PlacementUnits = placementUnits ?? throw new ArgumentNullException(nameof(placementUnits));
        Groups = groups ?? throw new ArgumentNullException(nameof(groups));
        ForbiddenUnitPairs = forbiddenUnitPairs ?? throw new ArgumentNullException(nameof(forbiddenUnitPairs));
        ClassificationConstraints = classificationConstraints ?? throw new ArgumentNullException(nameof(classificationConstraints));
        MinGroups = minGroups;
        Participants = BuildParticipantsFromPlacementUnits(placementUnits);
    }

    /// <summary>
    /// תפקיד: בנאי תאימות — מאפשר לספק רשימת משתתפים מפורשת.
    /// </summary>
    /// <param name="participants">משתתפים עם סיווגים.</param>
    /// <param name="placementUnits">יחידות שיבוץ.</param>
    /// <param name="groups">קבוצות יעד.</param>
    /// <param name="forbiddenUnitPairs">זוגות אסורים.</param>
    /// <param name="classificationConstraints">אילוצי סיווג.</param>
    /// <remarks>נקרא מ- <see cref="SolverInputBuilder.TryBuild"/> עם סיווגים מלאים.</remarks>
    public SolverRequest(
        IReadOnlyList<SolverParticipantRequest> participants,
        IReadOnlyList<PlacementUnitRequest> placementUnits,
        IReadOnlyList<SolverGroupRequest> groups,
        IReadOnlyList<SolverForbiddenUnitPairRequest> forbiddenUnitPairs,
        IReadOnlyList<SolverClassificationConstraintRequest> classificationConstraints,
        int minGroups = 0)
        : this(placementUnits, groups, forbiddenUnitPairs, classificationConstraints, minGroups)
    {
        Participants = participants ?? Array.Empty<SolverParticipantRequest>();
    }

    /// <summary>
    /// תפקיד: יחידות השיבוץ — הצמתים שהפותר משבץ.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="SolverResultMapper"/>, <see cref="SolverJobWireMapper"/>.</remarks>
    public IReadOnlyList<PlacementUnitRequest> PlacementUnits { get; }

    /// <summary>
    /// תפקיד: קבוצות יעד עם מגבלות גודל.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="SolverResultMapper"/>, <see cref="SolverJobWireMapper"/>.</remarks>
    public IReadOnlyList<SolverGroupRequest> Groups { get; }

    /// <summary>
    /// תפקיד: מספר הקבוצות המינימלי שחייבות להיות בשימוש; 0 = ללא חסם תחתון.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="SolverJobWireMapper.ToSubmitWire"/>.</remarks>
    public int MinGroups { get; }

    /// <summary>
    /// תפקיד: זוגות יחידות שאסור לשבץ באותה קבוצה.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="SolverJobWireMapper"/>.</remarks>
    public IReadOnlyList<SolverForbiddenUnitPairRequest> ForbiddenUnitPairs { get; }

    /// <summary>
    /// תפקיד: אילוצי סיווג שמועברים לפותר.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="SolverJobWireMapper"/>.</remarks>
    public IReadOnlyList<SolverClassificationConstraintRequest> ClassificationConstraints { get; }

    /// <summary>
    /// תפקיד: משתתפים עם סיווגים — לשימוש wire ותאימות.
    /// </summary>
    /// <remarks>נקרא מ- <see cref="SolverJobWireMapper.ToSubmitWire"/>.</remarks>
    public IReadOnlyList<SolverParticipantRequest> Participants { get; private set; }

    private static IReadOnlyList<SolverParticipantRequest> BuildParticipantsFromPlacementUnits(
        IReadOnlyList<PlacementUnitRequest> placementUnits) =>
        placementUnits
            .SelectMany(unit => unit.ParticipantIds)
            .Distinct(StringComparer.Ordinal)
            .Select(participantId => new SolverParticipantRequest(participantId, new Dictionary<string, string>()))
            .ToList();
}

/// <summary>
/// תפקיד: משתתף בבקשת פותר עם מפת סיווגים.
/// </summary>
/// <param name="ParticipantId">מזהה המשתתף.</param>
/// <param name="Classifications">מימד → רמה.</param>
/// <remarks>נקרא מ- <see cref="SolverJobWireMapper"/>.</remarks>
public sealed record SolverParticipantRequest(
    string ParticipantId,
    IReadOnlyDictionary<string, string> Classifications);

/// <summary>
/// תפקיד: קבוצת יעד בבקשת פותר.
/// </summary>
/// <param name="GroupId">מזהה הקבוצה.</param>
/// <param name="MinSize">גודל מינימלי.</param>
/// <param name="MaxSize">גודל מקסימלי.</param>
/// <remarks>נקרא מ- <see cref="SolverJobWireMapper"/>, <see cref="SolverResultMapper"/>.</remarks>
public sealed record SolverGroupRequest(
    int GroupId,
    int MinSize,
    int MaxSize);

/// <summary>
/// תפקיד: זוג יחידות שיבוץ שאסור לשבץ באותה קבוצה.
/// </summary>
/// <param name="FirstUnitId">מזהה יחידה ראשונה.</param>
/// <param name="SecondUnitId">מזהה יחידה שנייה.</param>
/// <remarks>נוצר ע"י <see cref="SolverInputBuilder"/>; נשלח דרך wire.</remarks>
public sealed record SolverForbiddenUnitPairRequest(
    int FirstUnitId,
    int SecondUnitId);

/// <summary>
/// תפקיד: אילוץ סיווג מנורמל לשליחה לפותר.
/// </summary>
/// <param name="ConstraintKind">שם סוג האילוץ.</param>
/// <param name="TargetDimension">מימד הסיווג.</param>
/// <param name="TargetLevel">רמה יעד (לאיזון פשוט).</param>
/// <param name="MinCountPerGroup">מינימום לקבוצה.</param>
/// <param name="MaxCountPerGroup">מקסימום לקבוצה.</param>
/// <param name="AllowedLevels">רמות מותרות (איזון יחסי/הומוגני).</param>
/// <param name="MaxScaledDeviation">סטייה מקסימלית מותרת (איזון יחסי).</param>
/// <remarks>נוצר ע"י <see cref="SolverInputBuilder"/>; נשלח דרך wire.</remarks>
public sealed record SolverClassificationConstraintRequest(
    string ConstraintKind,
    string TargetDimension,
    string? TargetLevel,
    int? MinCountPerGroup,
    int? MaxCountPerGroup,
    IReadOnlyList<string>? AllowedLevels,
    long? MaxScaledDeviation);
