using MyProject.BL.Algorithm.InitialPlacement.Results;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.InitialPlacement.Placement;

/// <summary>
/// בונה חלוקה ראשונית בגישה חמדנית: מהיחידות הקשות לשיבוץ (הגדולות) לקלות.
/// </summary>
public sealed class GreedyPlacementBuilder
{
    /// <summary>
    /// מנסה לבנות חלוקה ראשונית עבור כל המשתתפים.
    /// </summary>
    /// <param name="input">קלט ההצבה הראשונית.</param>
    /// <param name="mandatoryUnits">יחידות החובה שנבנו מאילוצי זוגות חובה.</param>
    /// <returns>
    /// רשימת קבוצות מולאות, או <c>null</c> אם לא ניתן לשבץ את כל המשתתפים.
    /// </returns>
    public IReadOnlyList<Group>? TryBuild(InitialPlacementInput input, MandatoryUnitMap mandatoryUnits)
    {
        return TryBuild(input, mandatoryUnits, out _).Groups;
    }

    /// <summary>
    /// מנסה לבנות חלוקה ראשונית, עם הסבר בעברית אם השיבוץ נכשל.
    /// </summary>
    public GreedyBuildResult TryBuild(
        InitialPlacementInput input,
        MandatoryUnitMap mandatoryUnits,
        out IReadOnlyList<string> buildErrors)
    {
        buildErrors = Array.Empty<string>();
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (mandatoryUnits is null)
        {
            throw new ArgumentNullException(nameof(mandatoryUnits));
        }

        // ResolveGroupCount היא מתודה פנימית בהמשך הקובץ.
        // היא קוראת אילוץ מספר קבוצות מתוך הקלט ומחזירה כמה קבוצות לפתוח.
        var groupCount = ResolveGroupCount(input);
        if (groupCount == 0)
        {
            buildErrors = new[] { "לא הוגדר מספר קבוצות — לא ניתן לבנות חלוקה." };
            return new GreedyBuildResult(null, buildErrors);
        }

        // ResolveCapacities ממומשת בהמשך הקובץ.
        // הפלט: מילון קיבולות לפי מזהה קבוצה.
        var capacities = ResolveCapacities(input, groupCount);

        // InitSlots ממומשת בהמשך הקובץ.
        // הפלט: "מצב עבודה" ריק שבו כל קבוצה מכילה כרגע רשימת משתתפים ריקה.
        var slots = InitSlots(groupCount);

        var forbiddenPairs = input.Constraints
            .OfType<ForbiddenPairConstraint>()
            .ToList();

        // BuildForbiddenLookup היא מתודת עזר מקומית באותו קובץ.
        // היא ממירה אילוצי "אסור ביחד" למבנה בדיקה מהיר בזמן שיבוץ.
        var forbiddenLookup = BuildForbiddenLookup(forbiddenPairs);

        var unitsByDescendingSize = mandatoryUnits.Units
            // תחביר מיון יורד: קודם משבצים יחידות גדולות, כי הן קשות יותר להצבה.
            .OrderByDescending(kv => kv.Value.Count)
            .ToList();

        var assignedParticipants = new HashSet<ParticipantId>();

        foreach (var (_, members) in unitsByDescendingSize)
        {
            // תחביר "(_, members)" מתעלם מהמפתח ומושך רק את ערך היחידה (רשימת משתתפים).
            // FindGroupForUnit היא מתודה פנימית בהמשך הקובץ.
            // היא בודקת קיבולת + קונפליקטים כדי להחליט לאיזו קבוצה מותר להכניס את היחידה.
            var targetGroup = FindGroupForUnit(members, slots, capacities, forbiddenLookup);
            if (targetGroup < 0)
            {
                var reason = InfeasibilityExplanationBuilder.DescribeUnitPlacementFailure(
                    members,
                    slots,
                    capacities,
                    forbiddenLookup);
                buildErrors = string.IsNullOrWhiteSpace(reason)
                    ? new[] { "לא ניתן לשבץ יחידת חובה — אין קבוצה מתאימה." }
                    : new[] { reason };
                return new GreedyBuildResult(null, buildErrors);
            }

            // כאן ההשמה נעשית ברמת יחידה:
            // כל חברי members מוכנסים לאותה קבוצה targetGroup.
            foreach (var member in members)
            {
                slots[targetGroup].Add(member);
                assignedParticipants.Add(member);
            }
        }

        var singletons = input.Participants
            .Select(p => p.Id)
            // רק מי שלא נכנס דרך יחידות חובה ייכנס עכשיו כשיבוץ יחידני.
            .Where(id => !assignedParticipants.Contains(id))
            .ToList();

        foreach (var participantId in singletons)
        {
            // FindGroupForSingleton היא גרסה יחידנית לאותה בדיקת התאמה.
            var targetGroup = FindGroupForSingleton(participantId, slots, capacities, forbiddenLookup);
            if (targetGroup < 0)
            {
                var reason = InfeasibilityExplanationBuilder.DescribeUnitPlacementFailure(
                    new[] { participantId },
                    slots,
                    capacities,
                    forbiddenLookup);
                buildErrors = string.IsNullOrWhiteSpace(reason)
                    ? new[] { $"לא ניתן לשבץ את משתתף {participantId} — אין קבוצה מתאימה." }
                    : new[] { reason };
                return new GreedyBuildResult(null, buildErrors);
            }

            slots[targetGroup].Add(participantId);
        }

        return new GreedyBuildResult(BuildGroups(slots), Array.Empty<string>());
        // BuildGroups (מתודה בהמשך הקובץ) ממירה את מבנה ה-slots
        // ליישויות Group של שכבת Core.
    }

    private static int ResolveGroupCount(InitialPlacementInput input)
    {
        // GroupCountConstraint מגיע משכבת Core:
        // MyProject.Core.Domain.Constraints.GroupCountConstraint
        var countConstraint = input.Constraints
            .OfType<GroupCountConstraint>()
            .FirstOrDefault();

        // תחביר "?." מונע גישה לערך אם האובייקט ריק; במקרה כזה מוחזר ברירת המחדל מ-"??".
        return countConstraint?.MaxGroups ?? 0;
    }

    private static Dictionary<int, int> ResolveCapacities(InitialPlacementInput input, int groupCount)
    {
        // כאן ממירים אילוצי גודל למבנה מילון:
        // key = מזהה קבוצה, value = קיבולת מקסימלית.
        var sizeConstraints = input.Constraints
            .OfType<GroupSizeConstraint>()
            .ToDictionary(c => c.GroupId.Value, c => c.MaxCapacity.Value);

        var capacities = new Dictionary<int, int>(groupCount);
        for (var i = 1; i <= groupCount; i++)
        {
            capacities[i] = sizeConstraints.TryGetValue(i, out var cap)
                ? cap
                // אם אין אילוץ קיבולת מפורש לקבוצה, מתייחסים אליה כקיבולת פתוחה.
                : int.MaxValue;
        }

        return capacities;
    }

    private static Dictionary<int, List<ParticipantId>> InitSlots(int groupCount)
    {
        // מילון זמני לתהליך הבנייה:
        // key = מזהה קבוצה, value = רשימת משתתפים ששובצו לקבוצה.
        var slots = new Dictionary<int, List<ParticipantId>>(groupCount);
        for (var i = 1; i <= groupCount; i++)
        {
            slots[i] = new List<ParticipantId>();
        }

        return slots;
    }

    private static Dictionary<ParticipantId, HashSet<ParticipantId>> BuildForbiddenLookup(
        IReadOnlyList<ForbiddenPairConstraint> forbiddenPairs)
    {
        // המטרה: להפוך רשימת אילוצי זוגות אסורים
        // למבנה חיפוש מהיר בזמן אמת בשיבוץ.
        //
        // התוצאה:
        // key = משתתף
        // value = כל המשתתפים שאסור לו להיות איתם באותה קבוצה.
        var lookup = new Dictionary<ParticipantId, HashSet<ParticipantId>>();
        foreach (var pair in forbiddenPairs)
        {
            if (!lookup.TryGetValue(pair.ParticipantA, out var setA))
            {
                setA = new HashSet<ParticipantId>();
                lookup[pair.ParticipantA] = setA;
            }

            setA.Add(pair.ParticipantB);

            if (!lookup.TryGetValue(pair.ParticipantB, out var setB))
            {
                setB = new HashSet<ParticipantId>();
                lookup[pair.ParticipantB] = setB;
            }

            setB.Add(pair.ParticipantA);
        }

        return lookup;
    }

    private static int FindGroupForUnit(
        IReadOnlyList<ParticipantId> members,
        Dictionary<int, List<ParticipantId>> slots,
        Dictionary<int, int> capacities,
        Dictionary<ParticipantId, HashSet<ParticipantId>> forbiddenLookup)
    {
        // מעבר סדרתי על כל הקבוצות כדי למצוא את הראשונה שמתאימה.
        foreach (var groupIndex in slots.Keys.OrderBy(k => k))
        {
            if (slots[groupIndex].Count + members.Count > capacities[groupIndex])
            {
                // חישוב מהיר: אם אין מקום, אין טעם לבדוק איסורי זוגות לאותה קבוצה.
                continue;
            }

            if (HasForbiddenConflict(members, slots[groupIndex], forbiddenLookup))
            {
                // HasForbiddenConflict ממומשת בהמשך הקובץ.
                // היא בודקת אם יש לפחות זוג אסור אחד בין הנכנסים לבין מי שכבר בקבוצה.
                continue;
            }

            return groupIndex;
        }

        return -1;
    }

    private static int FindGroupForSingleton(
        ParticipantId participantId,
        Dictionary<int, List<ParticipantId>> slots,
        Dictionary<int, int> capacities,
        Dictionary<ParticipantId, HashSet<ParticipantId>> forbiddenLookup)
    {
        // אותה לוגיקה כמו FindGroupForUnit, אבל עבור משתתף יחיד.
        foreach (var groupIndex in slots.Keys.OrderBy(k => k))
        {
            if (slots[groupIndex].Count + 1 > capacities[groupIndex])
            {
                continue;
            }

            if (HasForbiddenConflict(new[] { participantId }, slots[groupIndex], forbiddenLookup))
            {
                continue;
            }

            return groupIndex;
        }

        return -1;
    }

    private static bool HasForbiddenConflict(
        IEnumerable<ParticipantId> incomingMembers,
        IReadOnlyList<ParticipantId> existingMembers,
        Dictionary<ParticipantId, HashSet<ParticipantId>> forbiddenLookup)
    {
        // בדיקה כפולה:
        // לכל נכנס בודקים אם אחד מהקיימים נמצא בקבוצת האסורים שלו.
        foreach (var incoming in incomingMembers)
        {
            if (!forbiddenLookup.TryGetValue(incoming, out var forbidden))
            {
                continue;
            }

            if (existingMembers.Any(existing => forbidden.Contains(existing)))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<Group> BuildGroups(Dictionary<int, List<ParticipantId>> slots)
    {
        // מעבר ממבנה עבודה זמני (slots) ליישויות דומיין סופיות:
        // Group מוגדרת בשכבת Core:
        // MyProject.Core.Domain.Entities.Group
        return slots
            .Where(kv => kv.Value.Count > 0)
            .OrderBy(kv => kv.Key)
            .Select(kv => new Group(new GroupId(kv.Key), kv.Value))
            .ToList();
    }
}
