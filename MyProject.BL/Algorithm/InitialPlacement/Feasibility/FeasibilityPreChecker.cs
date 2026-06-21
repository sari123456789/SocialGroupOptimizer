using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.InitialPlacement;

/// <summary>
/// תפקיד המחלקה: בדיקת היתכנות מוקדמת לפני בניית חלוקה.
/// המחלקה משתתפת בשלב הראשון של השיבוץ הראשוני — מסננת קלטים בלתי-פתירים.
/// </summary>
/// <remarks>
/// תפקיד הרכיב הוא מסנן מוקדם בלבד — לא מחלק משתתפים ולא מחשב ציון.
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

        // אוסף שגיאות מרכזי לכל שלבי ההיתכנות.
        var errors = new List<string>();

        // שלב א: בדיקות בסיסיות על מבנה הקלט.
        ValidateInputShape(input, errors);
        if (errors.Count > 0)
        {
            // אם מבנה הקלט כבר שבור, אין טעם לרוץ לבדיקות מתקדמות.
            return FeasibilityPreCheckResult.Infeasible(errors);
        }

        // שלב ב: הפרדת האילוצים לפי סוג, כדי לבדוק כל נושא בנפרד.
        // OfType<T> מסנן את הרשימה לאיברים מטיפוס מסוים בלבד.
         var groupCountConstraint = input.Constraints.OfType<GroupCountConstraint>().FirstOrDefault();
        // ToList מממש את השאילתה לרשימה ממשית בזיכרון.
        var groupSizeConstraints = input.Constraints.OfType<GroupSizeConstraint>().ToList();
        var mandatoryPairs = input.Constraints.OfType<MandatoryPairConstraint>().ToList();
        var forbiddenPairs = input.Constraints.OfType<ForbiddenPairConstraint>().ToList();


        CheckCapacity(input.Participants.Count, groupCountConstraint, groupSizeConstraints, errors);// בודק אם מספר המשתתפים מתאים לקיבולת המינימום והמקסימום הכוללת.
        CheckDirectPairContradictions(mandatoryPairs, forbiddenPairs, errors);// בודק אם אותו זוג מופיע גם כחובה וגם כאסור.
        CheckPairParticipantsExist(mandatoryPairs, forbiddenPairs, input.Participants, errors);// בודק שהמשתתפים שמופיעים באילוצי זוגות קיימים בקלט המשתתפים.

        // אם אילוץ מפנה למשתתף שלא קיים, עוצרים לפני בניית יחידות חובה.
        if (errors.Count > 0)
        {
            return FeasibilityPreCheckResult.Infeasible(errors);
        }

        // שלב ד: בניית יחידות חובה ואימות סתירות ביניהן.
        var mandatoryUnits = MandatoryGroupBuilder.Build(input, mandatoryPairs);
        // שתי המתודות הבאות מקומיות לקובץ:
        CheckForbiddenPairsInsideMandatoryUnits(mandatoryUnits, forbiddenPairs, errors);//זוג אסור באותה יחידה
        CheckMandatoryUnitSize(mandatoryUnits, groupCountConstraint, groupSizeConstraints, errors);//יחידת חובה גדולה מגודל מקסימום

        // שלב ה: בדיקה פשוטה על גרף קונפליקטים בין יחידות חובה.
        var conflictGraph = ConflictGraphBuilder.Build(mandatoryUnits, forbiddenPairs);
        // CheckObviousWholeGraphClique (מקומית לקובץ) בודקת מקרה קיצון:
        // אם כל היחידות סותרות את כולן, אולי כבר עכשיו אין פתרון.
        CheckObviousWholeGraphClique(conflictGraph.Adjacency, groupCountConstraint, errors);

        CheckMandatoryUnitsVsProportionalBalance(
            input,
            mandatoryUnits,
            groupCountConstraint,
            groupSizeConstraints,
            errors);

        // אם לא נמצאה סתירה מוקדמת, ממשיכים לניסיון בניית חלוקה.
        // תחביר תנאי מקוצר: אם אין שגיאות מחזירים "אפשרי", אחרת מחזירים "לא אפשרי" עם פירוט.
        return errors.Count == 0
            ? FeasibilityPreCheckResult.Feasible()
            : FeasibilityPreCheckResult.Infeasible(errors);
    }

    /// <summary>
    /// בודק שהקלט תקין מבחינה מבנית בלבד.
    /// </summary>
    private static void ValidateInputShape(InitialPlacementInput input, ICollection<string> errors)
    {
        // חשוב: המתודה הזו לא מפילה חריגות.
        // היא אוספת הודעות כדי להחזיר תשובה מרוכזת אחת בסוף.
        if (input.Participants is null)
        {
            errors.Add("Participants list cannot be null.");
            return;
        }

        if (input.Participants.Count == 0)
        {
            errors.Add("Participants list must contain at least one participant.");
        }

        // Any עם למבדה מחזיר true אם לפחות איבר אחד מקיים את התנאי.
        if (input.Participants.Any(participant => participant is null))
        {
            errors.Add("Participants list cannot contain null entries.");
        }

        if (input.Constraints is null)
        {
            errors.Add("Constraints list cannot be null.");
            return;
        }

        // גם כאן Any עוצר ברגע הראשון שבו נמצא איבר לא תקין.
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
        // כאן תלויים במידע "קבוע":
        // מספר קבוצות ידוע מראש + אילוץ גודל לכל קבוצה.
        // אם זה לא מתקיים, מדלגים כדי לא לייצר אזעקת שווא.
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
        // התחביר "&&" אומר שכל התנאים חייבים להיות true.
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
        // ToHashSet מייצר מבנה נתונים שמאפשר Contains מהיר.
        var forbiddenPairKeys = forbiddenPairs
            // תחביר "Select" ממיר כל אילוץ למפתח זוג אחיד שאינו תלוי סדר.
            .Select(pair => ParticipantPairKey.Create(pair.ParticipantA, pair.ParticipantB))
            // תחביר "ToHashSet" יוצר מבנה בדיקה מהיר מאוד לשאלה "האם קיים".
            .ToHashSet();

        foreach (var pair in mandatoryPairs)
        {
            // ParticipantPairKey.Create ממומש בקובץ DataStructures/ParticipantPairKey.cs.
            // הוא מנרמל את הסדר כך שהשוואת זוגות תהיה עקבית.
            var key = ParticipantPairKey.Create(pair.ParticipantA, pair.ParticipantB);
            if (forbiddenPairKeys.Contains(key))
            {
                errors.Add(
                    $"Participants {pair.ParticipantA} and {pair.ParticipantB} are both mandatory together and forbidden together.");
            }
        }
    }

    /// <summary>
    /// מוודא שכל המשתתפים שמופיעים באילוצי זוגות קיימים בקלט המשתתפים.
    /// </summary>
    private static void CheckPairParticipantsExist(
        IReadOnlyList<MandatoryPairConstraint> mandatoryPairs,
        IReadOnlyList<ForbiddenPairConstraint> forbiddenPairs,
        IReadOnlyList<MyProject.Core.Domain.Entities.Participant> participants,
        ICollection<string> errors)
    {
        HashSet<MyProject.Core.Domain.ValueObjects.ParticipantId> knownParticipants = participants
            .Select(participant => participant.Id)
            .ToHashSet();

        foreach (MandatoryPairConstraint pair in mandatoryPairs)
        {
            AddMissingParticipantError(pair.ParticipantA, "mandatory pair", knownParticipants, errors);
            AddMissingParticipantError(pair.ParticipantB, "mandatory pair", knownParticipants, errors);
        }

        foreach (ForbiddenPairConstraint pair in forbiddenPairs)
        {
            AddMissingParticipantError(pair.ParticipantA, "forbidden pair", knownParticipants, errors);
            AddMissingParticipantError(pair.ParticipantB, "forbidden pair", knownParticipants, errors);
        }
    }

    private static void AddMissingParticipantError(
        MyProject.Core.Domain.ValueObjects.ParticipantId participantId,
        string source,
        IReadOnlySet<MyProject.Core.Domain.ValueObjects.ParticipantId> knownParticipants,
        ICollection<string> errors)
    {
        if (!knownParticipants.Contains(participantId))
        {
            errors.Add($"Unknown participant '{participantId}' referenced in {source} constraints.");
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
            // TryGetUnitId ממומש בקובץ MandatoryGroups/MandatoryUnitMap.cs.
            // הוא מחזיר את מזהה היחידה של המשתתף אם קיים.
            if (!mandatoryUnits.TryGetUnitId(pair.ParticipantA, out var unitA)
                || !mandatoryUnits.TryGetUnitId(pair.ParticipantB, out var unitB))
            {
                // תחביר "out var" שולף ערך מפונקציה אם ההצלחה אמת; אם לא הצליח, מדלגים.
                // האופרטור "||" אומר: אם אחד הצדדים נכשל, ממשיכים ל-continue.
                continue;
            }

            if (unitA == unitB)
            {
                // משמעות לוגית: אותה יחידת חובה דורשת יחד,
                // אבל אילוץ אסור דורש הפרדה -> סתירה מיידית.
                errors.Add(
                    $"זוג איסור ({pair.ParticipantA}, {pair.ParticipantB}) נמצא בתוך אותה יחידת חובה — סתירה מובנית.");
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
            // unit.Key = מזהה יחידה, unit.Value = רשימת חברים ביחידה.
            if (unit.Value.Count > maximumGroupSize)
            {
                errors.Add(
                    $"יחידת חובה של {unit.Value.Count} משתתפים גדולה מגודל הקבוצה המקסימלי ({maximumGroupSize}).");
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
            // אם אין אילוץ מספר קבוצות או שאין עודף יחידות,
            // אין טעם לבדוק מצב Clique מלא.
            return;
        }

        // בדיקה פשוטה: האם כל צומת מחובר לכל צומת אחר.
        var unitIds = conflictGraph.Keys.ToList();
        // שתי לולאות for מקוננות:
        // עוברות על כל זוג יחידות פעם אחת בלבד (i<j).
        for (var i = 0; i < unitIds.Count; i++)
        {
            for (var j = i + 1; j < unitIds.Count; j++)
            {
                if (!conflictGraph[unitIds[i]].Contains(unitIds[j]))
                {
                    // ברגע שמצאנו זוג שלא מחובר, זה כבר לא Clique מלא.
                    return;
                }
            }
        }

        errors.Add(
            $"Conflict graph contains an obvious clique of {conflictGraph.Count} mandatory units, but only {groupCountConstraint.MaxGroups} groups are allowed.");
    }

    /// <summary>
    /// בודק אם יחידת חובה מרוכזת ברמה אחת שוברת איזון יחסי בקבוצה בגודל קבוע.
    /// </summary>
    private static void CheckMandatoryUnitsVsProportionalBalance(
        InitialPlacementInput input,
        MandatoryUnitMap mandatoryUnits,
        GroupCountConstraint? groupCountConstraint,
        IReadOnlyList<GroupSizeConstraint> groupSizeConstraints,
        ICollection<string> errors)
    {
        if (!HasUniformFixedGroupSize(groupCountConstraint, groupSizeConstraints, out var groupSize))
        {
            return;
        }

        var balanceConstraints = input.Constraints
            .OfType<ClassificationProportionalBalanceConstraint>()
            .ToList();

        if (balanceConstraints.Count == 0)
        {
            return;
        }

        var participantsById = input.Participants.ToDictionary(participant => participant.Id);
        var totalParticipants = input.Participants.Count;

        foreach (var balance in balanceConstraints)
        {
            var globalCounts = CountGlobalLevels(input, balance);

            foreach (var unit in mandatoryUnits.Units)
            {
                var unitLevelCounts = CountUnitLevelCounts(unit.Value, participantsById, balance);
                foreach (var (level, countInUnit) in unitLevelCounts)
                {
                    if (!globalCounts.TryGetValue(level, out var globalCount))
                    {
                        continue;
                    }

                    if (globalCount * groupSize % totalParticipants != 0)
                    {
                        continue;
                    }

                    var maxAllowedInGroup = globalCount * groupSize / totalParticipants;
                    if (countInUnit > maxAllowedInGroup)
                    {
                        errors.Add(
                            $"יחידת חובה של {unit.Value.Count} משתתפים סותרת אילוץ איזון במימד '{balance.TargetDimension}': " +
                            $"רמה '{level}' מופיעה {countInUnit} פעמים ביחידה, אך בכל קבוצה מותר לכל היותר {maxAllowedInGroup}.");
                    }
                }
            }
        }
    }
    /// <summary>
    /// פונקציה עוזרת שמוודאת אם יש מידע מספיק וקבוע על גודל הקבוצות כדי לבדוק את האיזון היחסי.
    /// </summary>
    private static bool HasUniformFixedGroupSize(
        GroupCountConstraint? groupCountConstraint,
        IReadOnlyList<GroupSizeConstraint> groupSizeConstraints,
        out int groupSize)
    {
        groupSize = 0;

        if (!HasFixedGroupSizeInformation(groupCountConstraint, groupSizeConstraints))
        {
            return false;
        }

        groupSize = groupSizeConstraints[0].MinSize;
        var expectedGroupSize = groupSize;
        return groupSizeConstraints.All(
            constraint => constraint.MinSize == expectedGroupSize
                && constraint.MaxCapacity.Value == expectedGroupSize);
    }

    /// <summary>
    /// פונקציה עוזרת לספור את מספר המשתתפים בכל רמה של מימד מסוים בכל הקבוצה.
    /// </summary>
    private static Dictionary<ClassificationLevelCode, int> CountGlobalLevels(
        InitialPlacementInput input,
        ClassificationProportionalBalanceConstraint balance)
    {
        var counts = balance.DimensionLevels.ToDictionary(level => level, _ => 0);

        foreach (var participant in input.Participants)
        {
            if (!TryGetParticipantLevel(participant, balance, out var level))
            {
                continue;
            }

            counts[level]++;
        }

        return counts;
    }

    /// <summary>
    /// פונקציה עוזרת לספור את מספר המשתתפים בכל רמה של מימד מסוים בתוך יחידת חובה.
    /// </summary>
    /// <param name="unitMembers"></param>
    /// <param name="participantsById"></param>
    /// <param name="balance"></param>
    /// <returns></returns>
    private static Dictionary<ClassificationLevelCode, int> CountUnitLevelCounts(
        IReadOnlyList<ParticipantId> unitMembers,
        IReadOnlyDictionary<ParticipantId, MyProject.Core.Domain.Entities.Participant> participantsById,
        ClassificationProportionalBalanceConstraint balance)
    {
        var counts = new Dictionary<ClassificationLevelCode, int>();

        foreach (var participantId in unitMembers)
        {
            if (!participantsById.TryGetValue(participantId, out var participant))
            {
                continue;
            }

            if (!TryGetParticipantLevel(participant, balance, out var level))
            {
                continue;
            }

            counts[level] = counts.GetValueOrDefault(level) + 1;
        }

        return counts;
    }
    /// <summary>
    /// פונקציה עוזרת שמנסה לשלוף את הרמה של משתתף מסוים במימד מסוים, אם קיימת.
    /// </summary>
    /// <param name="participant"></param>
    /// <param name="balance"></param>
    /// <param name="level"></param>
    /// <returns></returns>
    private static bool TryGetParticipantLevel(
        MyProject.Core.Domain.Entities.Participant participant,
        ClassificationProportionalBalanceConstraint balance,
        out ClassificationLevelCode level)
    {
        level = default;

        if (!participant.Classifications.TryGetValue(balance.TargetDimension, out var participantLevel))
        {
            return false;
        }

        if (!balance.DimensionLevels.Contains(participantLevel))
        {
            return false;
        }

        level = participantLevel;
        return true;
    }
}
