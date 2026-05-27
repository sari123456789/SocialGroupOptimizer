using MyProject.Core.Domain.Constraints;

namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// מבצע בדיקות היתכנות מהירות לפני ניסיון בניית הצבה ראשונית.
/// </summary>
/// <remarks>
/// תפקיד הרכיב הוא מסנן מוקדם בלבד.
/// הוא לא מחלק משתתפים, לא בונה קבוצות, ולא מחשב ציון.
/// </remarks>
public sealed class FeasibilityPreChecker
{
    /// <summary>
    /// בודק האם קיימת סתירה מוקדמת שמונעת בניית הצבה ראשונית.
    /// </summary>
    /// <remarks>
    /// תוצאה חיובית לא אומרת שיש חלוקה תקינה.
    /// היא רק אומרת שלא נמצאה סתירה ברורה בשלב המוקדם.
    /// </remarks>
    /// <param name="input">קלט ההצבה הראשונית.</param>
    /// <returns>תוצאת בדיקת ההיתכנות.</returns>
    /// <exception cref="ArgumentNullException">נזרק כאשר הקלט הוא null.</exception>
    public FeasibilityPreCheckResult Check(InitialPlacementInput input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var errors = new List<string>();

        // שלב א: בדיקות בסיסיות על מבנה הקלט.
        ValidateInputShape(input, errors);
        if (errors.Count > 0)
        {
            return FeasibilityPreCheckResult.Infeasible(errors);
        }

        // שלב ב: הפרדת האילוצים לפי סוג, כדי לבדוק כל נושא בנפרד.
        var groupCountConstraint = input.Constraints.OfType<GroupCountConstraint>().FirstOrDefault();
        var groupSizeConstraints = input.Constraints.OfType<GroupSizeConstraint>().ToList();
        var mandatoryPairs = input.Constraints.OfType<MandatoryPairConstraint>().ToList();
        var forbiddenPairs = input.Constraints.OfType<ForbiddenPairConstraint>().ToList();

        // שלב ג: בדיקות קיבולת וסתירות ישירות.
        CheckCapacity(input.Participants.Count, groupCountConstraint, groupSizeConstraints, errors);
        CheckDirectPairContradictions(mandatoryPairs, forbiddenPairs, errors);

        // שלב ד: בניית יחידות חובה ואימות סתירות ביניהן.
        var mandatoryUnits = MandatoryGroupBuilder.Build(input, mandatoryPairs);
        CheckForbiddenPairsInsideMandatoryUnits(mandatoryUnits, forbiddenPairs, errors);
        CheckMandatoryUnitSize(mandatoryUnits, groupCountConstraint, groupSizeConstraints, errors);

        // שלב ה: בדיקה פשוטה על גרף קונפליקטים בין יחידות חובה.
        var conflictGraph = ConflictGraphBuilder.Build(mandatoryUnits, forbiddenPairs);
        CheckObviousWholeGraphClique(conflictGraph.Adjacency, groupCountConstraint, errors);

        // אם לא נמצאה סתירה מוקדמת, ממשיכים לניסיון בניית חלוקה.
        return errors.Count == 0
            ? FeasibilityPreCheckResult.Feasible()
            : FeasibilityPreCheckResult.Infeasible(errors);
    }

    /// <summary>
    /// בודק שהקלט תקין מבחינה מבנית בלבד.
    /// </summary>
    private static void ValidateInputShape(InitialPlacementInput input, ICollection<string> errors)
    {
        if (input.Participants is null)
        {
            errors.Add("Participants list cannot be null.");
            return;
        }

        if (input.Participants.Count == 0)
        {
            errors.Add("Participants list must contain at least one participant.");
        }

        if (input.Participants.Any(participant => participant is null))
        {
            errors.Add("Participants list cannot contain null entries.");
        }

        if (input.Constraints is null)
        {
            errors.Add("Constraints list cannot be null.");
            return;
        }

        if (input.Constraints.Any(constraint => constraint is null))
        {
            errors.Add("Constraints list cannot contain null entries.");
        }
    }

    /// <summary>
    /// בודק האם מספר המשתתפים מתאים לקיבולת המינימום והמקסימום הכוללת.
    /// </summary>
    /// <remarks>
    /// הבדיקה מתבצעת רק כשיש מידע מלא וברור על מספר הקבוצות וגודל כל קבוצה.
    /// אם חסר מידע, הבדיקה מדולגת בלי לנחש.
    /// </remarks>
    private static void CheckCapacity(
        int participantCount,
        GroupCountConstraint? groupCountConstraint,
        IReadOnlyList<GroupSizeConstraint> groupSizeConstraints,
        ICollection<string> errors)
    {
        if (!HasFixedGroupSizeInformation(groupCountConstraint, groupSizeConstraints))
        {
            return;
        }

        // סכום המינימום והמקסימום של כל הקבוצות יחד.
        var totalMinimumCapacity = groupSizeConstraints.Sum(constraint => constraint.MinSize);
        var totalMaximumCapacity = groupSizeConstraints.Sum(constraint => constraint.MaxCapacity.Value);

        if (participantCount < totalMinimumCapacity)
        {
            errors.Add(
                $"Participant count {participantCount} is smaller than total minimum required capacity {totalMinimumCapacity}.");
        }

        if (participantCount > totalMaximumCapacity)
        {
            errors.Add(
                $"Participant count {participantCount} is greater than total maximum capacity {totalMaximumCapacity}.");
        }
    }

    /// <summary>
    /// בודק האם יש מידע מספיק וקבוע לחישוב קיבולת.
    /// </summary>
    private static bool HasFixedGroupSizeInformation(
        GroupCountConstraint? groupCountConstraint,
        IReadOnlyList<GroupSizeConstraint> groupSizeConstraints)
    {
        // דורשים מספר קבוצות קבוע, ואילוץ גודל לכל קבוצה.
        return groupCountConstraint is not null
            && groupCountConstraint.MinGroups == groupCountConstraint.MaxGroups
            && groupSizeConstraints.Count == groupCountConstraint.MinGroups;
    }

    /// <summary>
    /// בודק אם אותו זוג מופיע גם כחובה וגם כאסור.
    /// </summary>
    private static void CheckDirectPairContradictions(
        IReadOnlyList<MandatoryPairConstraint> mandatoryPairs,
        IReadOnlyList<ForbiddenPairConstraint> forbiddenPairs,
        ICollection<string> errors)
    {
        // מפתח אחיד לזוג, כדי שלא ישנה סדר המשתתפים.
        var forbiddenPairKeys = forbiddenPairs
            .Select(pair => ParticipantPairKey.Create(pair.ParticipantA, pair.ParticipantB))
            .ToHashSet();

        foreach (var pair in mandatoryPairs)
        {
            var key = ParticipantPairKey.Create(pair.ParticipantA, pair.ParticipantB);
            if (forbiddenPairKeys.Contains(key))
            {
                errors.Add(
                    $"Participants {pair.ParticipantA} and {pair.ParticipantB} are both mandatory together and forbidden together.");
            }
        }
    }

    /// <summary>
    /// בודק אם שני משתתפים אסורים נמצאים באותה יחידת חובה.
    /// </summary>
    private static void CheckForbiddenPairsInsideMandatoryUnits(
        MandatoryUnitMap mandatoryUnits,
        IReadOnlyList<ForbiddenPairConstraint> forbiddenPairs,
        ICollection<string> errors)
    {
        foreach (var pair in forbiddenPairs)
        {
            if (!mandatoryUnits.TryGetUnitId(pair.ParticipantA, out var unitA)
                || !mandatoryUnits.TryGetUnitId(pair.ParticipantB, out var unitB))
            {
                continue;
            }

            if (unitA == unitB)
            {
                errors.Add(
                    $"Forbidden participants {pair.ParticipantA} and {pair.ParticipantB} are inside the same mandatory unit.");
            }
        }
    }

    /// <summary>
    /// בודק אם יחידת חובה גדולה מדי לקבוצה אחת.
    /// </summary>
    private static void CheckMandatoryUnitSize(
        MandatoryUnitMap mandatoryUnits,
        GroupCountConstraint? groupCountConstraint,
        IReadOnlyList<GroupSizeConstraint> groupSizeConstraints,
        ICollection<string> errors)
    {
        if (!HasFixedGroupSizeInformation(groupCountConstraint, groupSizeConstraints))
        {
            return;
        }

        // לוקחים את גודל הקבוצה הגדולה ביותר כגבול עליון ליחידת חובה.
        var maximumGroupSize = groupSizeConstraints.Max(constraint => constraint.MaxCapacity.Value);

        foreach (var unit in mandatoryUnits.Units)
        {
            if (unit.Value.Count > maximumGroupSize)
            {
                errors.Add(
                    $"Mandatory unit with {unit.Value.Count} participants is larger than maximum group capacity {maximumGroupSize}.");
            }
        }
    }

    /// <summary>
    /// בודק מקרה פשוט שבו כל יחידות החובה סותרות זו את זו.
    /// </summary>
    /// <remarks>
    /// אם כל יחידה סותרת את כל השאר, כל יחידה חייבת להיות בקבוצה נפרדת.
    /// אם יש יותר יחידות כאלה ממספר הקבוצות המותר, אין פתרון.
    /// </remarks>
    private static void CheckObviousWholeGraphClique(
        IReadOnlyDictionary<int, HashSet<int>> conflictGraph,
        GroupCountConstraint? groupCountConstraint,
        ICollection<string> errors)
    {
        if (groupCountConstraint is null || conflictGraph.Count <= groupCountConstraint.MaxGroups)
        {
            return;
        }

        // בדיקה פשוטה: האם כל צומת מחובר לכל צומת אחר.
        var unitIds = conflictGraph.Keys.ToList();
        for (var i = 0; i < unitIds.Count; i++)
        {
            for (var j = i + 1; j < unitIds.Count; j++)
            {
                if (!conflictGraph[unitIds[i]].Contains(unitIds[j]))
                {
                    return;
                }
            }
        }

        errors.Add(
            $"Conflict graph contains an obvious clique of {conflictGraph.Count} mandatory units, but only {groupCountConstraint.MaxGroups} groups are allowed.");
    }
}
