using MyProject.BL.Algorithm.InitialPlacement.Results;
using MyProject.BL.Algorithm.LocalSearch.RuntimeData;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.InitialPlacement.Placement;

/// <summary>
/// תפקיד המחלקה: בניית חלוקה ראשונית בגישה חמדנית.
/// המחלקה משתתפת בשלב השיבוץ הראשוני — אחרי בדיקת היתכנות ובניית יחידות חובה.
/// </summary>
/// <remarks>
/// סדר שיבוץ: יחידות חובה מהגדולות לקטנות, ואז משתתפים יחידניים.
/// </remarks>
public sealed class GreedyPlacementBuilder
{
    /// <summary>
    /// תפקיד הפונקציה: מנסה לבנות חלוקה ראשונית עבור כל המשתתפים.
    /// קלט עיקרי: קלט שיבוץ, מפת יחידות חובה.
    /// פלט עיקרי: רשימת קבוצות או null בכישלון.
    /// </summary>
    public IReadOnlyList<Group>? TryBuild(InitialPlacementInput input, MandatoryUnitMap mandatoryUnits)
    {
        return TryBuild(input, mandatoryUnits, out _).Groups;
    }

    /// <summary>
    /// תפקיד הפונקציה: מנסה לבנות חלוקה עם פירוט שגיאות בעברית בכישלון.
    /// קלט עיקרי: קלט שיבוץ, יחידות חובה.
    /// פלט עיקרי: GreedyBuildResult — קבוצות או הודעות כשלון.
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

        // ResolveGroupCount קובע כמה "מקומות" קבוצה לפתוח לפי אילוצים ומספר משתתפים.
        var groupCount = ResolveGroupCount(input);
        if (groupCount == 0)
        {
            buildErrors = new[] { "לא הוגדר מספר קבוצות — לא ניתן לבנות חלוקה." };
            return new GreedyBuildResult(null, buildErrors);
        }

        // מילון של כמה משתתפים מקסימום עבור כל קבוצה.
        var capacities = ResolveCapacities(input, groupCount);

        // הפלט: "מצב עבודה" ריק שבו כל קבוצה מכילה כרגע רשימת משתתפים ריקה.
        var slots = InitSlots(groupCount);

        var forbiddenPairs = input.Constraints//אילוצי אסור ביחד מהקלט.
            .OfType<ForbiddenPairConstraint>()
            .ToList();

        // היא ממירה אילוצי "אסור ביחד" למבנה בדיקה מהיר בזמן שיבוץ.
        var forbiddenLookup = BuildForbiddenLookup(forbiddenPairs);
        var useFixedLayout = UsesFixedGroupLayout(input);// האם יש אילוצי קיבולת שמחייבים גודל קבוע לכל קבוצה.
        var candidateScorer = useFixedLayout ? null : BuildCandidateScorer(input);// בונה אובייקט שמחשב ניקוד למועמדים לקבוצות — נבנה רק אם אין אילוצי קיבולת שמחייבים גודל קבוע.
        var preferenceIndex = useFixedLayout ? null : ParticipantPreferenceIndex.Create(input.Participants);// אינדקס העדפות משתתפים — נבנה רק אם אין אילוצי קיבולת שמחייבים גודל קבוע.
        var remainingToAssign = input.Participants.Count; //כמות משתתפים שעדיין לא שובצו.

        var unitsByDescendingSize = mandatoryUnits.Units
            // תחביר מיון יורד: קודם משבצים יחידות גדולות, כי הן קשות יותר להצבה.
            .OrderByDescending(kv => kv.Value.Count)
            .ToList();
        //אוסף של משתתפים ששובצו
        var assignedParticipants = new HashSet<ParticipantId>();

        foreach (var (_, members) in unitsByDescendingSize)
        {
            // מזהה יחידה לא חשוב כאן רק חברי היחידה
            //בדיקת קיבולת וקונפליקטים אסורים כדי למצוא קבוצה מתאימה ליחידה כולה.
            var targetGroup = FindGroupForUnit(
                members,
                slots,
                capacities,
                forbiddenLookup,
                candidateScorer,
                remainingToAssign,
                useFixedLayout);
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

            remainingToAssign -= members.Count;
        }

        var singletons = input.Participants
            .Select(p => p.Id)
            .Where(id => !assignedParticipants.Contains(id));

        if (!useFixedLayout && preferenceIndex is not null)
        {
            singletons = singletons
                .OrderByDescending(id =>
                    preferenceIndex.GetPreferredByParticipant(id).Count
                    + preferenceIndex.GetParticipantsWhoPrefer(id).Count)
                .ThenBy(id => id.Value, StringComparer.Ordinal);
        }

        foreach (var participantId in singletons)
        {
            // FindGroupForSingleton היא גרסה יחידנית לאותה בדיקת התאמה.
            var targetGroup = FindGroupForSingleton(
                participantId,
                slots,
                capacities,
                forbiddenLookup,
                candidateScorer,
                remainingToAssign,
                useFixedLayout);
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
            remainingToAssign--;
        }

        return new GreedyBuildResult(BuildGroups(slots), Array.Empty<string>());
        // BuildGroups (מתודה בהמשך הקובץ) ממירה את מבנה ה-slots
        // ליישויות Group של שכבת Core.
    }
    /// <summary>
    /// תפקיד הפונקציה: קובעת כמה קבוצות לפתוח לפי אילוצי GroupCount ו-GroupSize.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static int ResolveGroupCount(InitialPlacementInput input)
    {
        var countConstraint = input.Constraints
            .OfType<GroupCountConstraint>()
            .FirstOrDefault();

        if (countConstraint is null)
        {
            // אם אין אילוץ ספציפי למספר קבוצות, נבדוק אילוצי גודל קבוצות כדי לקבוע את המספר המינימלי של קבוצות.
            var sizeOnly = input.Constraints
                .OfType<GroupSizeConstraint>()
                .Select(constraint => constraint.GroupId.Value)
                .DefaultIfEmpty(0)
                .Max();

            return sizeOnly;
        }

        if (countConstraint.MinGroups == countConstraint.MaxGroups)
        {
            return countConstraint.MinGroups;
        }

        var participantCount = input.Participants.Count;//כמות המשתתפים הכוללת בקלט.
        var sizeConstraints = input.Constraints.OfType<GroupSizeConstraint>().ToList();// רשימת אילוצי גודל קבוצות.
        if (sizeConstraints.Count == 0 || participantCount == 0)
        {
            return countConstraint.MinGroups;
        }

        var minSize = sizeConstraints.Min(constraint => constraint.MinSize);// הגודל המינימלי האפשרי של כל קבוצה.
        var maxSize = sizeConstraints.Max(constraint => constraint.MaxCapacity.Value);// הגודל המקסימלי האפשרי של כל קבוצה.

        var minFeasibleGroups = (int)Math.Ceiling(participantCount / (double)maxSize);// המספר המינימלי של קבוצות שיכולים להכיל את כל המשתתפים.
        var maxFeasibleGroups = participantCount / minSize;// המספר המקסימלי של קבוצות שיכולים להכיל את כל המשתתפים.

        var targetMin = Math.Max(countConstraint.MinGroups, minFeasibleGroups);// המספר המינימלי של קבוצות שמקיים את כל האילוצים.
        var targetMax = Math.Min(countConstraint.MaxGroups, maxFeasibleGroups);// המספר המקסימלי של קבוצות שמקיים את כל האילוצים.

        return targetMin;
    }
    /// <summary>
    /// בודקת אם אילוצי כמות קבוצות וגודלי קבוצות מחייבים גודל קבוע 
    /// של כל קבוצה (כלומר, אין אפשרות לשנות את מספר הקבוצות או את גדלי הקבוצות).
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static bool UsesFixedGroupLayout(InitialPlacementInput input)
    {
        var countConstraint = input.Constraints.OfType<GroupCountConstraint>().FirstOrDefault();//אילוץ כמות קבוצות
        if (countConstraint is null || countConstraint.MinGroups != countConstraint.MaxGroups)
        {
            return false;
        }

        var sizeConstraints = input.Constraints.OfType<GroupSizeConstraint>().ToList();//אילוצי גודל קבוצות
        if (sizeConstraints.Count != countConstraint.MinGroups)
        {
            return false;
        }

        return sizeConstraints.All(constraint =>
            constraint.MinSize == constraint.MaxCapacity.Value);// מינימום שווה למקסימום.
    }

    /// <summary>
    /// תפקיד הפונקציה: ממירה אילוצי קיבולת למבנה מילון נוח לשימוש.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="groupCount"></param>
    /// <returns></returns>
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

    /// <summary>
    /// תפקיד הפונקציה: מייצרת מבנה slots ריק לפי מספר קבוצות.
    /// </summary>
    /// <param name="groupCount"></param>
    /// <returns></returns>
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
    /// <summary>
    /// תפקיד הפונקציה: ממירה רשימת אילוצי זוגות אסורים למבנה חיפוש מהיר.
    /// </summary>
    /// <param name="forbiddenPairs"></param>
    /// <returns></returns>
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

    /// <summary>
    /// תפקיד הפונקציה: בונה אובייקט GreedyGroupCandidateScorer שמחשב ניקוד למועמדים לקבוצות.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static GreedyGroupCandidateScorer BuildCandidateScorer(InitialPlacementInput input)
    {
        var participantsById = input.Participants.ToDictionary(participant => participant.Id);//מילון משתתפים לפי מזהה
        var outgoingPreferences = new Dictionary<ParticipantId, IReadOnlyDictionary<ParticipantId, int>>();//מילון העדפות יוצאות לפי משתתף
        var requestedByParticipant = new Dictionary<ParticipantId, Dictionary<ParticipantId, int>>();//מילון העדפות נכנסות לפי משתתף

        foreach (var participant in input.Participants)
        {
            var outgoingRanks = new Dictionary<ParticipantId, int>();//מילון שמיועד למשתתפים שהמשתתף הנוכחי מעדיף, עם דירוג ההעדפה שלהם.
            foreach (var preference in participant.Preferences)
            {
                outgoingRanks[preference.PreferredParticipantId] = preference.Rank;// מוסיפים את ההעדפה היוצאת למשתתף הנוכחי.

                //אם אין עדיין רשימה של משתתפים שמבקשים את המשתתף המועדף, יוצרים מילון חדש.
                if (!requestedByParticipant.TryGetValue(preference.PreferredParticipantId, out var requesters))
                {
                    requesters = new Dictionary<ParticipantId, int>();//מילון שמיועד למשתתפים שמבקשים את המשתתף המועדף, עם דירוג ההעדפה שלהם.
                    requestedByParticipant[preference.PreferredParticipantId] = requesters;// מוסיפים את המילון החדש למשתתף המועדף.
                }

                requesters[participant.Id] = preference.Rank;//הוספת המשתתף הנוכחי למילון המעדיפים - למשתתף המועדף
            }

            outgoingPreferences[participant.Id] = outgoingRanks;//הוספת המילון של העדפות יוצאות למשתתף הנוכחי למילון הראשי.
        }
        // המרה של requestedByParticipant למבנה קריא בלבד (IReadOnlyDictionary)
        var requestedByParticipantReadOnly = requestedByParticipant.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlyDictionary<ParticipantId, int>)entry.Value);

        return new GreedyGroupCandidateScorer(
            participantsById,//מילון משתתפים לפי מזהה
            outgoingPreferences,//מילון העדפות יוצאות לפי משתתף
            requestedByParticipantReadOnly);//מילון העדפות נכנסות לפי משתתף
    }

    /// <summary>
    /// תפקיד הפונקציה: בוחרת קבוצת יעד ליחידת שיבוץ — בודקת קיבולת וזוגות אסורים.
    /// קלט עיקרי: חברי היחידה, מצב קבוצות, קיבולות וטבלת איסורים.
    /// פלט עיקרי: מזהה קבוצה או -1 אם אין מקום מתאים.
    /// </summary>
    private static int FindGroupForUnit(
        IReadOnlyList<ParticipantId> members,//חברי היחידה
        Dictionary<int, List<ParticipantId>> slots,//מצב הקבוצות הנוכחי
        Dictionary<int, int> capacities,//קיבולת מקסימלית לכל קבוצה
        Dictionary<ParticipantId, HashSet<ParticipantId>> forbiddenLookup,//טבלת איסורים
        GreedyGroupCandidateScorer? candidateScorer,//אובייקט שמחשב ניקוד למועמדים לקבוצות
        int remainingParticipantsToAssign,//כמות משתתפים שעדיין לא שובצו
        bool useFixedLayout)// האם יש אילוצי קיבולת שמחייבים גודל קבוע לכל קבוצה 
    {
        //אם יש אילוצי קיבולת קשוחים מחפשים את הקבוצה הראשונה שמתאימה לפי סדר עולה של מזהי קבוצות.
        if (useFixedLayout)
        {
            foreach (var groupIndex in slots.Keys.OrderBy(k => k))
            {
                if (slots[groupIndex].Count + members.Count > capacities[groupIndex])
                {
                    continue;
                }

                if (HasForbiddenConflict(members, slots[groupIndex], forbiddenLookup))//אם יש קונפליקט אסור בקבוצה
                {
                    continue;
                }

                return groupIndex;// מחזירים את הקבוצה הראשונה שעומדת בתנאים של קיבולת ואיסורים.
            }

            return -1;//לא נמצאה קבוצה מתאימה.
        }

        if (candidateScorer is null)
        {
            throw new InvalidOperationException("Flexible placement requires a candidate scorer.");
        }

        var bestGroup = -1;
        var bestScore = double.NegativeInfinity;//ניקוד טוב ביותר שנמצא עד כה
        var validGroups = new List<int>();// רשימת קבוצות תקינות שעומדות בתנאים של קיבולת ואיסורים

        //הוספת הקבוצות התקינות לרשימת הקבוצות התקינות לפי סדר עולה של מזהי קבוצות.
        foreach (var groupIndex in slots.Keys.OrderBy(k => k))
        {
            if (slots[groupIndex].Count + members.Count > capacities[groupIndex])
            {
                continue;
            }

            if (HasForbiddenConflict(members, slots[groupIndex], forbiddenLookup))
            {
                continue;
            }

            validGroups.Add(groupIndex);
        }

        if (validGroups.Count == 0)
        {
            return -1;
        }

        var minLoad = validGroups.Min(groupIndex => slots[groupIndex].Count);//כמות המשתתפים המינימלית בקבוצות התקינות
        var leastLoadedGroups = validGroups //רשימת הקבוצות פחות נטענות
            .Where(groupIndex => slots[groupIndex].Count == minLoad)
            .OrderBy(groupIndex => groupIndex);

        foreach (var groupIndex in leastLoadedGroups) //בוחרים את הקבוצה עם הניקוד הטוב ביותר מבין הקבוצות הפחות נטענות
        {
            var score = candidateScorer.ScoreCandidate(
                members,//חברי היחידה
                slots[groupIndex],//חברי הקבוצה הנוכחית
                capacities[groupIndex],//קיבולת הקבוצה הנוכחית
                remainingParticipantsToAssign);//כמות משתתפים שעדיין לא שובצו

            if (score > bestScore || (score == bestScore && (bestGroup < 0 || groupIndex < bestGroup)))
            {
                bestScore = score;
                bestGroup = groupIndex;
            }
        }

        return bestGroup;
    }

    /// <summary>
    /// תפקיד הפונקציה: בוחרת קבוצה למשתתף יחיד — עוטפת את FindGroupForUnit ליחידה בגודל 1.
    /// קלט עיקרי: מזהה משתתף ומצב השיבוץ.
    /// פלט עיקרי: מזהה קבוצה או -1.
    /// </summary>
    private static int FindGroupForSingleton(
        ParticipantId participantId,
        Dictionary<int, List<ParticipantId>> slots,
        Dictionary<int, int> capacities,
        Dictionary<ParticipantId, HashSet<ParticipantId>> forbiddenLookup,
        GreedyGroupCandidateScorer? candidateScorer,
        int remainingParticipantsToAssign,
        bool useFixedLayout)
    {
        return FindGroupForUnit(
            new[] { participantId },
            slots,
            capacities,
            forbiddenLookup,
            candidateScorer,
            remainingParticipantsToAssign,
            useFixedLayout);
    }

    /// <summary>
    /// תפקיד הפונקציה: בודקת האם הכנסת משתתפים לקבוצה תיצור זוג אסור.
    /// קלט עיקרי: נכנסים, חברים קיימים, טבלת איסורים.
    /// פלט עיקרי: true אם יש קונפליקט.
    /// </summary>
    private static bool HasForbiddenConflict(
        IEnumerable<ParticipantId> incomingMembers,//משתתפים שאנחנו רוצים להכניס לקבוצה
        IReadOnlyList<ParticipantId> existingMembers,//משתתפים שכבר נמצאים בקבוצה
        Dictionary<ParticipantId, HashSet<ParticipantId>> forbiddenLookup)//טבלת איסורים שממפה כל משתתף לקבוצת המשתתפים שאסור לו להיות איתם באותה קבוצה
    {
        // בדיקה כפולה:
        // לכל נכנס בודקים אם אחד מהקיימים נמצא בקבוצת האסורים שלו.
        foreach (var incoming in incomingMembers)
        {
            if (!forbiddenLookup.TryGetValue(incoming, out var forbidden))// אם אין רשימת אסורים למשתתף הנכנס, אין קונפליקט
            {
                continue;
            }

            if (existingMembers.Any(existing => forbidden.Contains(existing)))// אם אחד מהקיימים נמצא ברשימת האסורים של הנכנס, יש קונפליקט
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// תפקיד הפונקציה: ממירה מצב slots זמני לרשימת ישויות Group של Core.
    /// קלט עיקרי: מילון קבוצה → משתתפים.
    /// פלט עיקרי: רשימת קבוצות לא ריקות, ממוינות.
    /// </summary>
    private static IReadOnlyList<Group> BuildGroups(Dictionary<int, List<ParticipantId>> slots)
    {
        return slots
            .Where(kv => kv.Value.Count > 0)
            .OrderBy(kv => kv.Key)
            .Select(kv => new Group(new GroupId(kv.Key), kv.Value))
            .ToList();
    }
}
