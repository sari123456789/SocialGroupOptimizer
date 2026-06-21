using MyProject.BL.Algorithm.InitialPlacement.Solver.Models;
using MyProject.BL.Algorithm.InitialPlacement.Solver.Wire;

namespace MyProject.BL.Algorithm.InitialPlacement.Solver;

/// <summary>
/// תפקיד: מתרגם בקשת פותר פנימית (<see cref="SolverRequest"/>) לפורמט wire לשליחה ב-HTTP.
/// </summary>
/// <remarks>נקרא מ- <see cref="ExternalSolverClient.SolveAsync"/> לפני שליחת ה-job.</remarks>
public static class SolverJobWireMapper
{
    /// <summary>
    /// תפקיד: בונה אובייקט wire לשליחת job לשירות הפותר.
    /// </summary>
    /// <param name="request">בקשת הפותר הפנימית.</param>
    /// <param name="timeoutMs">מגבלת זמן במילישניות לשירות.</param>
    /// <returns>מבנה wire מוכן ל-serialization JSON.</returns>
    /// <remarks>נקרא מ- <see cref="ExternalSolverClient.SolveAsync"/>.</remarks>
    public static SolverJobSubmitWire ToSubmitWire(SolverRequest request, int timeoutMs)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return new SolverJobSubmitWire
        {
            RequestId = Guid.NewGuid(),
            TimeoutMs = timeoutMs, // מגבלת זמן לריצת הסולבר (במילישניות
            MinGroups = request.MinGroups, //מינימום קבוצות
            // ממיר את רשימת המשתתפים הפנימית לרשימת משתתפים wire. מספר זהות וסיווגים לכל משתתף.
            Participants = request.Participants
                .Select(participant => new SolverParticipantWire
                {
                    ParticipantId = participant.ParticipantId,
                    Classifications = participant.Classifications.ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value),
                })
                .ToList(),
            // ממיר את רשימת יחידות ההצבה הפנימית לרשימת יחידות wire. מספר זהות, רשימת משתתפים וסוג היחידה.
            PlacementUnits = request.PlacementUnits 
                .Select(unit => new PlacementUnitWire
                {
                    UnitId = unit.UnitId,
                    ParticipantIds = unit.ParticipantIds.ToList(),
                    // enum פנימי → enum wire ל-serialization JSON.
                    Kind = unit.Kind == PlacementUnitKind.MandatoryUnit
                        ? PlacementUnitKindWire.MandatoryUnit
                        : PlacementUnitKindWire.SingleParticipant,
                })
                .ToList(),
            // ממיר את רשימת הקבוצות הפנימית לרשימת קבוצות wire. מספר זהות, גודל מינימום ומקסימום לכל קבוצה.
            Groups = request.Groups
                .Select(group => new SolverGroupWire
                {
                    GroupId = group.GroupId,
                    MinSize = group.MinSize,
                    MaxSize = group.MaxSize,
                })
                .ToList(),
            // ממיר את רשימת הזוגות האסורים הפנימית לרשימת זוגות אסורים wire. מספר זהות של כל יחידה בזוג.
            ForbiddenUnitPairs = request.ForbiddenUnitPairs
                .Select(pair => new ForbiddenUnitPairWire
                {
                    FirstUnitId = pair.FirstUnitId,
                    SecondUnitId = pair.SecondUnitId,
                })
                .ToList(),
            // ממיר את רשימת המגבלות הפנימית לרשימת מגבלות
            // wire.
            // סוג המגבלה, מימד היעד, רמת היעד, מספר מינימום ומקסימום לכל קבוצה, רשימת רמות מותרות, סטיית תקן מקסימלית.
            ClassificationConstraints = request.ClassificationConstraints
                .Select(constraint => new ClassificationConstraintWire
                {
                    ConstraintKind = constraint.ConstraintKind,
                    TargetDimension = constraint.TargetDimension,
                    TargetLevel = constraint.TargetLevel,
                    MinCountPerGroup = constraint.MinCountPerGroup,
                    MaxCountPerGroup = constraint.MaxCountPerGroup,
                    AllowedLevels = constraint.AllowedLevels?.ToList(),
                    MaxScaledDeviation = constraint.MaxScaledDeviation,
                })
                .ToList(),
        };
    }
}
