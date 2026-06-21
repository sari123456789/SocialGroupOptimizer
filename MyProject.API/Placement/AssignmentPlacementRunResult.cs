using MyProject.Core.Domain.Entities;
using MyProject.BL.Algorithm.Improvement;
using MyProject.BL.Algorithm.InitialPlacement.Results;

namespace MyProject.API.Placement;

/// <summary>
/// תוצאת הרצה מלאה — Initial Placement ואופציונלית שיפור Local Search.
/// </summary>
public sealed class AssignmentPlacementRunResult
{
    /// <summary>תוצאת החלוקה הראשונית — ללא שינוי.</summary>
    public InitialPlacementResult PlacementResult { get; init; } = null!;

    /// <summary>חלוקה סופית לשמירה/הצגה — null אם Initial Placement לא הצליח.</summary>
    public Assignment? FinalAssignment { get; init; }

    /// <summary>תוצאת שיפור — null אם לא הורץ או fallback.</summary>
    /// <remarks>מאפיין אופציונלי שמחזיק את תוצאת השיפור.</remarks>
    public AssignmentImprovementResult? ImprovementResult { get; init; }

    /// <summary>האם ניסו להריץ שיפור (גם אם נכשל עם fallback).</summary>
    public bool WasImprovementAttempted { get; init; }

    /// <summary>האם חזרנו לחלוקה הראשונית אחרי כשלון שיפור.</summary>
    /// <remarks>מאפיין בוליאני שמציין אם נעשה שימוש בפתרון חלופי.</remarks>
    public bool UsedFallback { get; init; }


    /// <summary>אזהרה — למשל fallback או Assignment חסר.</summary>
    /// <remarks>מאפיין אופציונלי שמכיל טקסט אזהרה.</remarks>
    public string? Warning { get; init; }

}