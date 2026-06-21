using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.Services;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.BL.Algorithm.InitialPlacement.Placement;

/// <summary>
/// מנסה לתקן חלוקה לא חוקית באמצעות החלפות מקומיות (Swaps).
/// </summary>
/// <remarks>
/// <para>
/// נועד לטפל במקרים שבהם הבנייה החמדנית לא הצליחה לשבץ את כל המשתתפים,
/// או שהחלוקה הגולמית עוברת ל-<see cref="InitialFeasibilityDecider"/> ונכשלת.
/// </para>
/// <para>
/// לאחר כל Swap — בדיקת חוקיות מלאה דרך <see cref="IAssignmentValidator"/>.
/// </para>
/// </remarks>
public sealed class LocalRepairEngine
{
    // המנוע הזה מופעל מתוך InitialPlacementOrchestrator.Run
    // רק כאשר בנייה חמדנית יצרה חלוקה שאינה חוקית.

    private readonly IAssignmentValidator _validator;

    /// <summary>
    /// מאתחל מופע חדש של <see cref="LocalRepairEngine"/>.
    /// </summary>
    /// <param name="validator">מאמת האילוצים לשימוש.</param>
    public LocalRepairEngine(IAssignmentValidator validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <summary>
    /// מנסה לתקן את החלוקה הנתונה באמצעות Swaps בין קבוצות.
    /// </summary>
    /// <param name="groups">המצב הנוכחי (ישתנה במקום אם יימצא Swap מתאים).</param>
    /// <param name="input">קלט ההצבה הראשונית.</param>
    /// <param name="mandatoryUnits">מיפוי יחידות החובה — כל תיקון עובד ברמת יחידה שלמה.</param>
    /// <param name="maxAttempts">מספר מקסימלי של ניסיונות Swap.</param>
    /// <param name="errors">שגיאות האחרונות אם לא הצלחנו לתקן.</param>
    /// <returns><c>true</c> אם החלוקה הפכה חוקית; <c>false</c> אם נגמרו הניסיונות.</returns>
    public bool TryRepair(
        List<Group> groups,
        InitialPlacementInput input,
        MandatoryUnitMap mandatoryUnits,
        int maxAttempts,
        out IReadOnlyList<string> errors)
    {
        // groups:
        // מצב השיבוץ הנוכחי שנכשל באימות, ומעודכן "במקום" אם נמצא תיקון.
        //
        // input:
        // כולל אילוצים, משתתפים, והקשר ריצה.
        //
        // mandatoryUnits:
        // מבטיחים בעזרתו שכל מהלך יעבוד על יחידה שלמה ולא על חלק יחידה.

        if (groups is null)
        {
            throw new ArgumentNullException(nameof(groups));
        }

        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (mandatoryUnits is null)
        {
            throw new ArgumentNullException(nameof(mandatoryUnits));
        }

        if (maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Repair attempts must be greater than zero.");
        }

        var constraints = input.Constraints;
        // טבלת חיפוש מהירה: לכל משתתף, מי אסור להיות איתו באותה קבוצה.
        var forbiddenLookup = BuildForbiddenLookup(input);

        // בניית ייצוג יחידתי: כל משתתף ממופה ליחידת שיבוץ שלמה (יחידת חובה או יחידת יחיד
        var participantToUnitId = BuildParticipantToUnitId(input, mandatoryUnits, out var unitIdToMembers);
        // BuildAllGroupIds + BuildGroupCapacities (אותו קובץ) מגדירות את גבולות המשחק לתיקון.
        var allGroupIds = BuildAllGroupIds(input, groups);
        var capacities = BuildGroupCapacities(input, allGroupIds);

        if (!TryBuildUnitState(
                groups,
                unitIdToMembers,
                participantToUnitId,
                allGroupIds,
                out var unitIdToGroupId,
                out var groupIdToUnitIds,
                out var splitError))
        {
            // TryBuildUnitState מזהה כבר בהתחלה מצב פיצול יחידה ומחזיר שגיאה ברורה.
            errors = new[] { splitError };
            return false;
        }

        // for עם מונה: אתחול; תנאי; צעד קידום.
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            // בכל איטרציה בונים קבוצות מחדש מהמצב היחידתי, כדי להבטיח שלא מפצלים יחידות.
            var currentGroups = BuildGroupsFromUnitState(groupIdToUnitIds, unitIdToMembers);
            var currentAssignment = new Assignment(currentGroups);
            if (_validator.IsValid(currentAssignment, constraints, out errors))
            {
                ApplyUnitStateToGroups(groups, currentGroups);
                return true;
            }

            if (!TryApplyOneUnitLevelOperation(
                    unitIdToMembers,
                    unitIdToGroupId,
                    groupIdToUnitIds,
                    capacities,
                    forbiddenLookup,
                    constraints))
            {
                // לא נמצא מהלך חוקי נוסף, לכן עוצרים את הלולאה מוקדם.
                break;
            }
        }

        var finalGroups = BuildGroupsFromUnitState(groupIdToUnitIds, unitIdToMembers);
        var finalAssignment = new Assignment(finalGroups);
        if (_validator.IsValid(finalAssignment, constraints, out errors))
        {
            ApplyUnitStateToGroups(groups, finalGroups);
            return true;
        }

        errors = errors
            // Concat מחבר רצפים בלי לשנות את המקור.
            .Concat(new[] { "No legal unit-level move or swap found without splitting mandatory units." })
            // Distinct מסיר כפילויות הודעות.
            .Distinct()
            .ToList();
        return false;
    }
    /// <summary>
    /// תפקיד הפונקציה: בונה טבלת חיפוש של איסורים בין משתתפים.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static Dictionary<ParticipantId, HashSet<ParticipantId>> BuildForbiddenLookup(InitialPlacementInput input)
    {
        // מקור הנתונים:
        // input.Constraints (מוגדר ב-InitialPlacementInput)
        // מסונן ל-ForbiddenPairConstraint בלבד.
        var lookup = new Dictionary<ParticipantId, HashSet<ParticipantId>>();
        foreach (var pair in input.Constraints.OfType<ForbiddenPairConstraint>())
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
    /// תפקיד הפונקציה: בונה מיפוי משתתף ליחידת שיבוץ שלמה, תוך שמירה על יחידות החובה.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="mandatoryUnits"></param>
    /// <param name="unitIdToMembers" ></param>
    private static Dictionary<ParticipantId, int> BuildParticipantToUnitId(
        InitialPlacementInput input,
        MandatoryUnitMap mandatoryUnits,
        out Dictionary<int, IReadOnlyList<ParticipantId>> unitIdToMembers)
    {
        // 1) משתמשים במיפוי יחידות החובה כדי למ משתתפים ליחידות.
        unitIdToMembers = mandatoryUnits.Units
            .ToDictionary(
                kv => kv.Key,
                kv => (IReadOnlyList<ParticipantId>)kv.Value.ToList());
        // 2) כל משתתף שלא נמצא ביחידת חובה מקבל יחידת יחיד חדשה.
        var participantToUnitId = new Dictionary<ParticipantId, int>();
        foreach (var unit in unitIdToMembers)
        {
            foreach (var participantId in unit.Value)
            {
                // גישה למילון בסוגריים מרובעים: key -> value.
                participantToUnitId[participantId] = unit.Key;
            }
        }
        // מזהים את המזהה הבא ליחידה חדשה (למשתתפים שלא שייכים ליחידת חובה).
        var nextUnitId = unitIdToMembers.Count == 0 ? 0 : unitIdToMembers.Keys.Max() + 1;
        foreach (var participant in input.Participants)
        {
            // אם המשתתף כבר שייך ליחידת חובה, אין צורך להוסיף אותו שוב.
            if (participantToUnitId.ContainsKey(participant.Id))
            {
                continue;
            }

            // כל משתתף שלא שייך ליחידת חובה הופך ליחידת יחיד בגודל 1.
            participantToUnitId[participant.Id] = nextUnitId;
            unitIdToMembers[nextUnitId] = new[] { participant.Id };
            nextUnitId++;
        }

        return participantToUnitId; // מחזיר את המיפוי הסופי של משתתף ליחידת שיבוץ, כאשר כל יחידה היא יחידת חובה או יחידת יחיד.
    }

    private static IReadOnlyList<GroupId> BuildAllGroupIds(InitialPlacementInput input, IReadOnlyList<Group> groups)
    {
        // משלב שני מקורות:
        // 1) קבוצות שכבר קיימות בפועל ב-groups.
        // 2) קבוצות שמופיעות באילוצי גודל, גם אם כרגע ריקות.
        // כך התיקון יכול לשקול מהלכים גם לקבוצות ריקות.
        var sizeConstraintGroupIds = input.Constraints
            .OfType<GroupSizeConstraint>()
            .Select(constraint => constraint.GroupId);

        return groups
            .Select(group => group.Id)
            .Concat(sizeConstraintGroupIds)
            .Distinct()
            .OrderBy(groupId => groupId.Value)
            .ToList();
    }

    private static Dictionary<GroupId, int> BuildGroupCapacities(
        InitialPlacementInput input,
        IReadOnlyList<GroupId> allGroupIds)
    {
        var configuredCapacities = input.Constraints
            .OfType<GroupSizeConstraint>()
            .ToDictionary(constraint => constraint.GroupId, constraint => constraint.MaxCapacity.Value);

        return allGroupIds.ToDictionary(
            groupId => groupId,
            // תנאי מקוצר: אם יש cap מוגדר משתמשים בו, אחרת קיבולת פתוחה.
            groupId => configuredCapacities.TryGetValue(groupId, out var cap) ? cap : int.MaxValue);
    }

    private static bool TryBuildUnitState(
        IReadOnlyList<Group> groups,
        IReadOnlyDictionary<int, IReadOnlyList<ParticipantId>> unitIdToMembers,
        IReadOnlyDictionary<ParticipantId, int> participantToUnitId,
        IReadOnlyList<GroupId> allGroupIds,
        out Dictionary<int, GroupId> unitIdToGroupId,
        out Dictionary<GroupId, List<int>> groupIdToUnitIds,
        out string splitError)
    {
        // שני מבני מצב משלימים:
        // unitIdToGroupId: איפה כל יחידה נמצאת כרגע.
        // groupIdToUnitIds: אילו יחידות נמצאות בכל קבוצה.
        //
        // למה שניים?
        // כי מהלך תיקון צריך לפעמים לענות מהר לשתי שאלות בכיוונים הפוכים.
        unitIdToGroupId = new Dictionary<int, GroupId>();
        groupIdToUnitIds = allGroupIds.ToDictionary(groupId => groupId, _ => new List<int>());
        splitError = string.Empty;

        var assignedMembersByUnit = unitIdToMembers.Keys.ToDictionary(unitId => unitId, _ => new HashSet<ParticipantId>());

        foreach (var group in groups)
        {
            foreach (var participantId in group.ParticipantIds)
            {
                if (!participantToUnitId.TryGetValue(participantId, out var unitId))
                {
                    splitError = $"Participant {participantId} has no placement unit.";
                    return false;
                }

                if (unitIdToGroupId.TryGetValue(unitId, out var existingGroupId)
                    && existingGroupId != group.Id)
                {
                    // אם אותה יחידה הופיעה בשתי קבוצות שונות, זה פיצול אסור של יחידת שיבוץ.
                    splitError = $"Unit {unitId} is split between groups {existingGroupId} and {group.Id}.";
                    return false;
                }

                unitIdToGroupId[unitId] = group.Id;
                assignedMembersByUnit[unitId].Add(participantId);
            }
        }

        foreach (var unit in unitIdToMembers)
        {
            var assignedCount = assignedMembersByUnit[unit.Key].Count;
            if (assignedCount > 0 && assignedCount != unit.Value.Count)
            {
                // מצב אסור: חלק מחברי היחידה בפנים וחלק בחוץ -> פיצול יחידה.
                splitError = $"Unit {unit.Key} is partially assigned and therefore split.";
                return false;
            }
        }

        foreach (var unit in unitIdToGroupId)
        {
            groupIdToUnitIds[unit.Value].Add(unit.Key);
        }

        return true;
    }

    private bool TryApplyOneUnitLevelOperation(
        IReadOnlyDictionary<int, IReadOnlyList<ParticipantId>> unitIdToMembers,
        Dictionary<int, GroupId> unitIdToGroupId,
        Dictionary<GroupId, List<int>> groupIdToUnitIds,
        IReadOnlyDictionary<GroupId, int> capacities,
        IReadOnlyDictionary<ParticipantId, HashSet<ParticipantId>> forbiddenLookup,
        IReadOnlyList<IConstraint> constraints)
    {
        // אסטרטגיית מהלכים:
        // שלב 1 - מנסים Move (יחידה אחת עוברת קבוצה).
        // שלב 2 - אם לא הצליח, מנסים Swap (יחידה מול יחידה בין קבוצות).
        //
        // כל מועמד עובר:
        // סינון מהיר (קיבולת/קונפליקט) -> אימות מלא עם IAssignmentValidator.
        var groupIds = groupIdToUnitIds.Keys.OrderBy(groupId => groupId.Value).ToList();

        foreach (var sourceGroupId in groupIds)
        {
            foreach (var unitId in groupIdToUnitIds[sourceGroupId].ToList())
            {
                foreach (var targetGroupId in groupIds.Where(groupId => groupId != sourceGroupId))
                {
                    // מהלך ראשון: העברה של יחידה שלמה מקבוצה לקבוצה.
                    if (!CanMoveUnit(
                            unitId,
                            sourceGroupId,
                            targetGroupId,
                            groupIdToUnitIds,
                            unitIdToMembers,
                            capacities,
                            forbiddenLookup))
                    {
                        continue;
                    }

                    var candidateUnitToGroup = CloneUnitToGroup(unitIdToGroupId);
                    var candidateGroupToUnits = CloneGroupToUnits(groupIdToUnitIds);
                    // עובדים על עותק זמני כדי לא לשנות מצב עד שהמהלך מאושר.

                    candidateGroupToUnits[sourceGroupId].Remove(unitId);
                    candidateGroupToUnits[targetGroupId].Add(unitId);
                    candidateUnitToGroup[unitId] = targetGroupId;

                    if (IsValidCandidate(candidateGroupToUnits, unitIdToMembers, constraints))
                    {
                        ApplyCandidateState(unitIdToGroupId, groupIdToUnitIds, candidateUnitToGroup, candidateGroupToUnits);
                        return true;
                    }
                }
            }
        }

        for (var i = 0; i < groupIds.Count - 1; i++)
        {
            for (var j = i + 1; j < groupIds.Count; j++)
            {
                var groupA = groupIds[i];
                var groupB = groupIds[j];

                foreach (var unitInA in groupIdToUnitIds[groupA].ToList())
                {
                    foreach (var unitInB in groupIdToUnitIds[groupB].ToList())
                    {
                        // מהלך שני: החלפה של יחידה שלמה מול יחידה שלמה.
                        if (!CanSwapUnits(
                                unitInA,
                                unitInB,
                                groupA,
                                groupB,
                                groupIdToUnitIds,
                                unitIdToMembers,
                                capacities,
                                forbiddenLookup))
                        {
                            continue;
                        }

                        var candidateUnitToGroup = CloneUnitToGroup(unitIdToGroupId);
                        var candidateGroupToUnits = CloneGroupToUnits(groupIdToUnitIds);
                        // גם כאן השינוי על עותק בלבד, ורק אחר כך מחילים.

                        candidateGroupToUnits[groupA].Remove(unitInA);
                        candidateGroupToUnits[groupA].Add(unitInB);
                        candidateGroupToUnits[groupB].Remove(unitInB);
                        candidateGroupToUnits[groupB].Add(unitInA);
                        candidateUnitToGroup[unitInA] = groupB;
                        candidateUnitToGroup[unitInB] = groupA;

                        if (IsValidCandidate(candidateGroupToUnits, unitIdToMembers, constraints))
                        {
                            ApplyCandidateState(unitIdToGroupId, groupIdToUnitIds, candidateUnitToGroup, candidateGroupToUnits);
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// תפקיד הפונקציה: בודקת האם ניתן להעביר יחידת שיבוץ שלמה לקבוצה אחרת.
    /// קלט עיקרי: מזהי יחידה וקבוצות, קיבולות וטבלת איסורים.
    /// פלט עיקרי: true אם המהלך אפשרי מבחינת קיבולת ואיסורים.
    /// </summary>
    private static bool CanMoveUnit(
        int unitId,
        GroupId sourceGroupId,
        GroupId targetGroupId,
        IReadOnlyDictionary<GroupId, List<int>> groupIdToUnitIds,
        IReadOnlyDictionary<int, IReadOnlyList<ParticipantId>> unitIdToMembers,
        IReadOnlyDictionary<GroupId, int> capacities,
        IReadOnlyDictionary<ParticipantId, HashSet<ParticipantId>> forbiddenLookup)
    {
        // בדיקת "אפשר מהלך" זולה לפני אימות מלא:
        // 1) קיבולת היעד אחרי המהלך.
        // 2) קונפליקטים אסורים מול יחידות שכבר ביעד.
        var unitSize = unitIdToMembers[unitId].Count;
        var targetLoad = GetGroupLoad(targetGroupId, groupIdToUnitIds, unitIdToMembers);
        if (targetLoad + unitSize > capacities[targetGroupId])
        {
            // סינון מהיר: קיבולת לפני בדיקת מאמת מלא.
            return false;
        }

        var targetUnits = groupIdToUnitIds[targetGroupId];
        if (HasForbiddenConflict(unitId, targetUnits, unitIdToMembers, forbiddenLookup))
        {
            return false;
        }

        return sourceGroupId != targetGroupId;
    }

    /// <summary>
    /// תפקיד הפונקציה: בודקת האם ניתן להחליף שתי יחידות שיבוץ בין שתי קבוצות.
    /// קלט עיקרי: שתי יחידות, שתי קבוצות, קיבולות ואיסורים.
    /// פלט עיקרי: true אם שני הצדדים נשארים חוקיים אחרי ההחלפה.
    /// </summary>
    private static bool CanSwapUnits(
        int unitInA,
        int unitInB,
        GroupId groupA,
        GroupId groupB,
        IReadOnlyDictionary<GroupId, List<int>> groupIdToUnitIds,
        IReadOnlyDictionary<int, IReadOnlyList<ParticipantId>> unitIdToMembers,
        IReadOnlyDictionary<GroupId, int> capacities,
        IReadOnlyDictionary<ParticipantId, HashSet<ParticipantId>> forbiddenLookup)
    {
        // Swap בודק שני צדדים:
        // האם כל קבוצה נשארת בתוך קיבולת
        // והאם היחידה החדשה לא מסתכסכת עם מי שנשאר בקבוצה.
        var unitASize = unitIdToMembers[unitInA].Count;
        var unitBSize = unitIdToMembers[unitInB].Count;

        var groupALoad = GetGroupLoad(groupA, groupIdToUnitIds, unitIdToMembers);
        var groupBLoad = GetGroupLoad(groupB, groupIdToUnitIds, unitIdToMembers);

        if (groupALoad - unitASize + unitBSize > capacities[groupA])
        {
            return false;
        }

        if (groupBLoad - unitBSize + unitASize > capacities[groupB])
        {
            return false;
        }

        var remainingA = groupIdToUnitIds[groupA].Where(unitId => unitId != unitInA).ToList();
        if (HasForbiddenConflict(unitInB, remainingA, unitIdToMembers, forbiddenLookup))
        {
            return false;
        }

        var remainingB = groupIdToUnitIds[groupB].Where(unitId => unitId != unitInB).ToList();
        if (HasForbiddenConflict(unitInA, remainingB, unitIdToMembers, forbiddenLookup))
        {
            return false;
        }

        return true;
    }

    private static int GetGroupLoad(
        GroupId groupId,
        IReadOnlyDictionary<GroupId, List<int>> groupIdToUnitIds,
        IReadOnlyDictionary<int, IReadOnlyList<ParticipantId>> unitIdToMembers)
    {
        return groupIdToUnitIds[groupId].Sum(unitId => unitIdToMembers[unitId].Count);
    }

    /// <summary>
    /// תפקיד הפונקציה: בודקת קונפליקט איסור בין יחידה נכנסת ליחידות קיימות בקבוצה.
    /// קלט עיקרי: יחידה נכנסת, יחידות בקבוצה, חברי יחידות וטבלת איסורים.
    /// פלט עיקרי: true אם נמצא זוג אסור.
    /// </summary>
    private static bool HasForbiddenConflict(
        int incomingUnitId,
        IEnumerable<int> existingUnitIds,
        IReadOnlyDictionary<int, IReadOnlyList<ParticipantId>> unitIdToMembers,
        IReadOnlyDictionary<ParticipantId, HashSet<ParticipantId>> forbiddenLookup)
    {
        // זו בדיקה ברמת יחידה:
        // משווים כל חבר ביחידה הנכנסת מול כל חבר ביחידות שכבר קיימות בקבוצה.
        // אם נמצא זוג אסור אחד - הקבוצה לא חוקית ליחידה הזו.
        var incomingMembers = unitIdToMembers[incomingUnitId];
        foreach (var existingUnitId in existingUnitIds)
        {
            var existingMembers = unitIdToMembers[existingUnitId];
            foreach (var incoming in incomingMembers)
            {
                if (!forbiddenLookup.TryGetValue(incoming, out var forbidden))
                {
                    continue;
                }

                if (existingMembers.Any(forbidden.Contains))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsValidCandidate(
        Dictionary<GroupId, List<int>> candidateGroupToUnits,
        IReadOnlyDictionary<int, IReadOnlyList<ParticipantId>> unitIdToMembers,
        IReadOnlyList<IConstraint> constraints)
    {
        // BuildGroupsFromUnitState (בהמשך הקובץ) בונה קבוצות חוקיות מבחינת שלמות יחידה.
        var candidateGroups = BuildGroupsFromUnitState(candidateGroupToUnits, unitIdToMembers);
        var candidateAssignment = new Assignment(candidateGroups);
        // האישור הסופי תמיד עובר דרך המאמת המרכזי, ולא דרך בדיקות ידניות בלבד.
        return _validator.IsValid(candidateAssignment, constraints, out _);
    }

    private static Dictionary<int, GroupId> CloneUnitToGroup(Dictionary<int, GroupId> source)
    {
        return source.ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private static Dictionary<GroupId, List<int>> CloneGroupToUnits(Dictionary<GroupId, List<int>> source)
    {
        return source.ToDictionary(kv => kv.Key, kv => kv.Value.ToList());
    }

    private static void ApplyCandidateState(
        Dictionary<int, GroupId> unitIdToGroupId,
        Dictionary<GroupId, List<int>> groupIdToUnitIds,
        Dictionary<int, GroupId> candidateUnitToGroupId,
        Dictionary<GroupId, List<int>> candidateGroupToUnitIds)
    {
        unitIdToGroupId.Clear();
        foreach (var unit in candidateUnitToGroupId)
        {
            unitIdToGroupId[unit.Key] = unit.Value;
        }

        groupIdToUnitIds.Clear();
        foreach (var group in candidateGroupToUnitIds)
        {
            groupIdToUnitIds[group.Key] = group.Value.ToList();
        }
    }

    /// <summary>
    /// תפקיד הפונקציה: בונה רשימת Group מתוך מצב יחידות — בלי לפצל יחידות חובה.
    /// קלט עיקרי: מיפוי קבוצה→יחידות ומיפוי יחידה→משתתפים.
    /// פלט עיקרי: קבוצות Core מלאות.
    /// </summary>
    private static IReadOnlyList<Group> BuildGroupsFromUnitState(
        IReadOnlyDictionary<GroupId, List<int>> groupIdToUnitIds,
        IReadOnlyDictionary<int, IReadOnlyList<ParticipantId>> unitIdToMembers)
    {
        // SelectMany:
        // "משטח" כמה רשימות חברים לרשימה אחת של משתתפים לכל קבוצה.
        //
        // Group נוצרת כאן מחדש מהמצב היחידתי,
        // ולכן אין מצב של פיצול יחידה בתוך קבוצה שנבנתה כך.
        return groupIdToUnitIds
            .OrderBy(kv => kv.Key.Value)
            .Where(kv => kv.Value.Count > 0)
            .Select(kv =>
            {
                var members = kv.Value
                    .SelectMany(unitId => unitIdToMembers[unitId])
                    .ToList();
                return new Group(kv.Key, members);
            })
            .ToList();
    }

    private static void ApplyUnitStateToGroups(List<Group> groups, IReadOnlyList<Group> rebuiltGroups)
    {
        // החלת מצב אטומית:
        // קודם מנקים את הרשימה הישנה, ואז מחליפים אותה בפלט שנבנה מהיחידות.
        groups.Clear();
        groups.AddRange(rebuiltGroups);
    }
}
