using Google.OrTools.Sat;
using MyProject.SolverService.Contracts;

namespace MyProject.SolverService.Solving;

/// <summary>
/// מנוע השיבוץ — מתרגם בעיית חלוקה לאילוצי CP-SAT ומריץ את Google OR-Tools.
/// </summary>
/// <remarks>
/// <para><b>רעיון כללי:</b> במקום לנסות שיבוץ חמדני, בונים מודל מתמטי שבו הפותר מחפש השמה שעומדת בכל האילוצים.</para>
/// <para><b>יחידת שיבוץ (PlacementUnit):</b> הצומת בבעיה. יכולה להיות משתתף בודד או יחידת חובה (כמה אנשים שחייבים לשבת יחד).</para>
/// <para><b>משתנה ההחלטה המרכזי x[unit, group]:</b> משתנה בוליאני — 1 אם היחידה שובצה לקבוצה, 0 אחרת.</para>
/// <para><b>זרימה:</b> בניית משתנים → אילוצי שיבוץ → אילוצי גודל → אילוצי איסור → אילוצי סיווג → הרצת פותר → תרגום תוצאה.</para>
/// <para>נקרא מ-<see cref="SolverJobProcessor"/> אחרי ש-MyProject.BL תרגם את הדומיין ל-<see cref="SolverJobRequest"/>.</para>
/// </remarks>
public sealed class CpSatPlacementSolver
{
    /// <summary>
    /// בונה מודל CP-SAT מלא, מריץ פותר עם מגבלת זמן, ומחזיר שיבוץ יחידות→קבוצות או סטטוס כשלון.
    /// </summary>
    /// <param name="request">קלט wire — יחידות, קבוצות, איסורים, סיווגים ו-timeout.</param>
    /// <returns>תוצאה עם מיפוי unitId→groupId בהצלחה, או Infeasible/Timeout/Failed.</returns>
    public CpSatSolveResult Solve(SolverJobRequest request)
    {
        var model = new CpModel();
        var units = request.PlacementUnits;
        var groups = request.Groups;
        var totalParticipants = units.Sum(unit => unit.ParticipantIds.Count);

        // ── שלב 1: משתני החלטה ──────────────────────────────────────────────
        // לכל זוג (יחידה, קבוצה) יוצרים משתנה בוליאני.
        // אם x[u,g]=1 → יחידה u שובצה לקבוצה g.
        var x = new Dictionary<(int UnitId, int GroupId), BoolVar>();
        foreach (var unit in units)
        {
            foreach (var group in groups)
            {
                x[(unit.UnitId, group.GroupId)] = model.NewBoolVar($"x_u{unit.UnitId}_g{group.GroupId}");
            }
        }

        // ── שלב 2: כל יחידה בדיוק בקבוצה אחת 
        // AddExactlyOne = בדיוק אחד מהמשתנים x[u,*] יהיה 1.
        // מבטיח שאי אפשר לשבץ יחידה ב-0 קבוצות או ב-2+ קבוצות.
        foreach (var unit in units)
        {
            var literals = groups.Select(group => x[(unit.UnitId, group.GroupId)]).ToArray();
            model.AddExactlyOne(literals);
        }

        // ── שלב 3: גודל קבוצות (עם קבוצות ריקות מותרות) 
        // load = כמה משתתפים בקבוצה (סכום גדלי יחידות ששובצו אליה).
        // used = האם הקבוצה לא ריקה.
        //
        // למה used נפרד? כי רוצים לאפשר קבוצות ריקות — לא חייבים למלא את כל
        // מספר הקבוצות המקסימלי. מינימום גודל חל רק על קבוצה שבאמת בשימוש.
        var usedGroupVars = new List<BoolVar>();
        foreach (var group in groups)
        {
            // משקל הטרם = מספר המשתתפים ביחידה (יחידה שלמה נשלחת לקבוצה אחת).
            var loadTerms = units
                .Select(unit => LinearExpr.Term(x[(unit.UnitId, group.GroupId)], unit.ParticipantIds.Count))
                .ToArray();
            var load = LinearExpr.Sum(loadTerms);

            var used = model.NewBoolVar($"used_g{group.GroupId}");
            // אם הקבוצה בשימוש — לפחות משתתף אחד (load≥1). אחרת load=0.
            model.Add(load >= 1).OnlyEnforceIf(used);
            model.Add(load == 0).OnlyEnforceIf(used.Not());

            // מינימום רק לקבוצה פעילה; מקסימום תמיד (קבוצה ריקה = 0 ≤ MaxSize).
            model.Add(load >= group.MinSize).OnlyEnforceIf(used);
            model.Add(load <= group.MaxSize);

            usedGroupVars.Add(used);
        }

        // אם הוגדר MinGroups — לפחות כך קבוצות חייבות להיות לא-ריקות.
        if (request.MinGroups > 0)
        {
            model.Add(LinearExpr.Sum(usedGroupVars) >= Math.Min(request.MinGroups, groups.Count));
        }

        // ── שלב 4: זוגות אסור בין יחידות 
        // לכל זוג אסור (u1,u2) ולכל קבוצה g:
        //   x[u1,g] + x[u2,g] ≤ 1
        // כלומר: לא יכולות שתי היחידות להיות באותה קבוצה.
        // (איסור בתוך אותה יחידת חובה כבר נחסם ב-BL לפני שליחה לפותר.)
        foreach (var pair in request.ForbiddenUnitPairs)
        {
            foreach (var group in groups)
            {
                model.Add(
                    x[(pair.FirstUnitId, group.GroupId)] + x[(pair.SecondUnitId, group.GroupId)] <= 1);
            }
        }

        // ── שלב 5: אילוצי סיווג 
        var classByParticipant = request.Participants.ToDictionary(
            participant => participant.ParticipantId,
            participant => participant.Classifications,
            StringComparer.Ordinal);

        AddClassificationConstraints(model, request, units, groups, x, classByParticipant, totalParticipants);

        // ── שלב 6: הרצת הפותר 
        var solver = new CpSolver();

        var timeoutSeconds = Math.Max(1.0, request.TimeoutMs / 1000.0); 
        solver.StringParameters = $"max_time_in_seconds:{timeoutSeconds}";

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var status = solver.Solve(model);
        stopwatch.Stop();

        return MapResult(status, solver, x, units, groups, stopwatch.ElapsedMilliseconds, request.TimeoutMs);
    }

    /// <summary>
    /// מוסיף אילוצי סיווג למודל לפי סוג האילוץ ב-wire.
    /// </summary>
    /// <remarks>
    /// שני סוגים נתמכים — תואמים לאילוצי Core:
    /// הפרדה הומוגנית (רמה אחת לקבוצה) ואיזון יחסי (יחס רמות דומה לגלובלי).
    /// </remarks>
    private static void AddClassificationConstraints(
        CpModel model,
        SolverJobRequest request,
        List<PlacementUnitWire> units,
        List<SolverGroupWire> groups,
        Dictionary<(int UnitId, int GroupId), BoolVar> x,
        Dictionary<string, Dictionary<string, string>> classByParticipant,
        int totalParticipants)
    {
        foreach (var constraint in request.ClassificationConstraints)
        {
            switch (constraint.ConstraintKind)
            {
                case "ClassificationHomogeneousGroupConstraint":
                    AddHomogeneousConstraint(model, units, groups, x, classByParticipant, constraint, totalParticipants);
                    break;
                case "ClassificationProportionalBalanceConstraint":
                    AddProportionalConstraint(model, units, groups, x, classByParticipant, constraint, totalParticipants);
                    break;
            }
        }
    }

    /// <summary>
    /// הפרדה הומוגנית: בכל קבוצה — לכל היותר רמת סיווג אחת במימד הנבחר.
    /// </summary>
    /// <remarks>
    /// <para>לכל קבוצה ולכל רמה אפשרית יוצרים דגל hasLevel — "האם יש בקבוצה לפחות משתתף אחד ברמה זו".</para>
    /// <para>אילוץ סופי: סכום hasLevel על כל הרמות ≤ 1 — לא יכולות להתקיים שתי רמות בו-זמנית.</para>
    /// </remarks>
    private static void AddHomogeneousConstraint(
        CpModel model,
        List<PlacementUnitWire> units,
        List<SolverGroupWire> groups,
        Dictionary<(int UnitId, int GroupId), BoolVar> x,
        Dictionary<string, Dictionary<string, string>> classByParticipant,
        ClassificationConstraintWire constraint,
        int totalParticipants)
    {
        var levels = constraint.AllowedLevels ?? new List<string>();
        if (levels.Count == 0)
        {
            return;
        }

        foreach (var group in groups)
        {
            var hasLevelVars = new List<BoolVar>();
            foreach (var level in levels)
            {
                // כמה משתתפים ברמה level נמצאים בקבוצה (תלוי בשיבוץ x).
                var count = BuildLevelCountVar(
                    model,
                    units,
                    group,
                    x,
                    classByParticipant,
                    constraint.TargetDimension,
                    level,
                    totalParticipants,
                    $"hom_g{group.GroupId}_{level}");

                // hasLevel=1 אם count≥1, אחרת 0.
                var hasLevel = model.NewBoolVar($"has_g{group.GroupId}_{level}");
                model.Add(count >= 1).OnlyEnforceIf(hasLevel);
                model.Add(count == 0).OnlyEnforceIf(hasLevel.Not());
                hasLevelVars.Add(hasLevel);
            }

            // לכל היותר רמה אחת "פעילה" בקבוצה.
            model.Add(LinearExpr.Sum(hasLevelVars) <= 1);
        }
    }

    /// <summary>
    /// איזון יחסי: בכל קבוצה, חלוקת הרמות דומה לחלוקה הגלובלית.
    /// </summary>
    /// <remarks>
    /// <para>בדומיין (Core) הבדיקה היא: g/n ≈ G/N</para>
    /// <para>כאן: g = מספר משתתפים ברמה בקבוצה, n = גודל הקבוצה,</para>
    /// <para>G = מספר גלובלי ברמה, N = סך כל המשתתפים.</para>
    /// <para>מימוש ללא שברים: |N·g − G·n| ≤ MaxScaledDeviation</para>
    /// </remarks>
    private static void AddProportionalConstraint(
        CpModel model,
        List<PlacementUnitWire> units,
        List<SolverGroupWire> groups,
        Dictionary<(int UnitId, int GroupId), BoolVar> x,
        Dictionary<string, Dictionary<string, string>> classByParticipant,
        ClassificationConstraintWire constraint,
        int totalParticipants)
    {
        var levels = constraint.AllowedLevels ?? new List<string>();
        var maxDeviation = constraint.MaxScaledDeviation ?? 0L;
        if (levels.Count == 0 || totalParticipants == 0)
        {
            return;
        }

        // ספירה גלובלית קבועה (לא תלויה בשיבוץ) — G בנוסחה.
        var globalCounts = levels.ToDictionary(
            level => level,
            level => CountParticipantsWithLevel(units, classByParticipant, constraint.TargetDimension, level),
            StringComparer.Ordinal);

        foreach (var group in groups)
        {
            // n = גודל הקבוצה כמשתנה (תלוי באילו יחידות שובצו אליה).
            var groupSizeTerms = units
                .Select(unit => LinearExpr.Term(x[(unit.UnitId, group.GroupId)], unit.ParticipantIds.Count))
                .ToArray();
            var groupSize = model.NewIntVar(0, totalParticipants, $"gs_g{group.GroupId}");
            model.Add(groupSize == LinearExpr.Sum(groupSizeTerms));

            foreach (var level in levels)
            {
                // g = כמה בקבוצה ברמה level.
                var count = BuildLevelCountVar(
                    model,
                    units,
                    group,
                    x,
                    classByParticipant,
                    constraint.TargetDimension,
                    level,
                    totalParticipants,
                    $"prop_g{group.GroupId}_{level}");

                var globalCount = globalCounts[level];
                // deviation = N·g − G·n  (totalParticipants = N)
                var deviation = model.NewIntVar(-totalParticipants * totalParticipants, totalParticipants * totalParticipants,
                    $"dev_g{group.GroupId}_{level}");
                model.Add(deviation == totalParticipants * count - globalCount * groupSize);
                model.Add(deviation <= maxDeviation);
                model.Add(deviation >= -maxDeviation);
            }
        }
    }

    /// <summary>
    /// בונה משתנה שלם: כמה משתתפים ברמה מסוימת נמצאים בקבוצה (לאחר שיבוץ).
    /// </summary>
    /// <remarks>
    /// <para>לכל יחידה u: סופרים כמה מחבריה שייכים לרמה level.</para>
    /// <para>אם היחידה שובצה לקבוצה (x[u,g]=1), תרומתה = matchingCount.</para>
    /// <para>אם לא שובצה (x=0), תרומה 0.</para>
    /// <para>דוגמה: יחידת חובה {א,ב} כשא=ט1 וב=י2 — לרמה ט1 תורם 1, לרמה י2 תורם 1 (לא את שניהם יחד).</para>
    /// </remarks>
    private static IntVar BuildLevelCountVar(
        CpModel model,
        List<PlacementUnitWire> units,
        SolverGroupWire group,
        Dictionary<(int UnitId, int GroupId), BoolVar> x,
        Dictionary<string, Dictionary<string, string>> classByParticipant,
        string dimension,
        string level,
        int totalParticipants,
        string name)
    {
        var terms = new List<LinearExpr>();
        foreach (var unit in units)
        {
            // כמה חברי היחידה ברמה level במימד dimension.
            var matchingCount = unit.ParticipantIds.Count(participantId =>
                classByParticipant.TryGetValue(participantId, out var map)
                && map.TryGetValue(dimension, out var participantLevel)
                && string.Equals(participantLevel, level, StringComparison.Ordinal));

            if (matchingCount > 0)
            {
                // matchingCount * x[unit, group] — תרומה רק אם היחידה שובצה לקבוצה.
                terms.Add(LinearExpr.Term(x[(unit.UnitId, group.GroupId)], matchingCount));
            }
        }

        var count = model.NewIntVar(0, totalParticipants, name);
        model.Add(count == (terms.Count == 0 ? LinearExpr.Constant(0) : LinearExpr.Sum(terms)));
        return count;
    }

    /// <summary>
    /// סופר משתתפים ברמה נתונה בכל היחידות — ערך קבוע לפני הרצת הפותר (G בנוסחת האיזון).
    /// </summary>
    private static int CountParticipantsWithLevel(
        List<PlacementUnitWire> units,
        Dictionary<string, Dictionary<string, string>> classByParticipant,
        string dimension,
        string level)
    {
        var count = 0;
        foreach (var unit in units)
        {
            foreach (var participantId in unit.ParticipantIds)
            {
                if (classByParticipant.TryGetValue(participantId, out var map)
                    && map.TryGetValue(dimension, out var participantLevel)
                    && string.Equals(participantLevel, level, StringComparison.Ordinal))
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// מתרגם סטטוס CP-SAT לתוצאה שה-API מחזיר.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>Optimal/Feasible → Success + מיפוי unit→group מערכי x.</item>
    ///   <item>Infeasible → אין פתרון שעומד בכל האילוצים.</item>
    ///   <item>Unknown ליד timeout → Timeout (נגמר הזמן).</item>
    ///   <item>אחר → Failed עם הודעת סטטוס.</item>
    /// </list>
    /// </remarks>
    private static CpSatSolveResult MapResult(
        CpSolverStatus status,
        CpSolver solver,
        Dictionary<(int UnitId, int GroupId), BoolVar> x,
        List<PlacementUnitWire> units,
        List<SolverGroupWire> groups,
        long wallTimeMs,
        int timeoutMs)
    {
        var meta = new SolverMetaWire
        {
            Engine = "OR-Tools-CP-SAT",
            WallTimeMs = wallTimeMs,
            ObjectiveValue = solver.ObjectiveValue,
        };

        if (status is CpSolverStatus.Optimal or CpSolverStatus.Feasible)
        {
            var assignments = new Dictionary<int, int>();
            foreach (var unit in units)
            {
                foreach (var group in groups)
                {
                    // בגלל AddExactlyOne — בדיוק ערך 1 אחד לכל יחידה.
                    if (solver.Value(x[(unit.UnitId, group.GroupId)]) == 1)
                    {
                        assignments[unit.UnitId] = group.GroupId;
                        break;
                    }
                }
            }

            return CpSatSolveResult.Success(assignments, meta);
        }

        if (status == CpSolverStatus.Infeasible)
        {
            return CpSatSolveResult.Infeasible(meta);
        }

        if (status == CpSolverStatus.Unknown && wallTimeMs >= timeoutMs - 50)
        {
            // CP-SAT מחזיר Unknown גם כשנגמר זמן — מבדילים מכשל טכני לפי קרבה ל-timeout.
            return CpSatSolveResult.Timeout(meta);
        }

        return CpSatSolveResult.Failed(
            meta,
            new[] { $"CP-SAT returned status {status}." });
    }
}

/// <summary>
/// תוצאת ריצת CP-SAT — סטטוס, שיבוץ יחידות לקבוצות, מטא-נתונים ושגיאות.
/// </summary>
public sealed class CpSatSolveResult
{
    private CpSatSolveResult(
        SolverJobStatus status,
        Dictionary<int, int> unitGroupAssignments,
        SolverMetaWire? meta,
        IReadOnlyList<string> errors,
        bool isTimeout)
    {
        Status = status;
        UnitGroupAssignments = unitGroupAssignments;
        Meta = meta;
        Errors = errors;
        IsTimeout = isTimeout;
    }

    /// <summary>סטטוס סופי — Success / Infeasible / Timeout / Failed.</summary>
    public SolverJobStatus Status { get; }

    /// <summary>מיפוי unitId→groupId — רלוונטי רק כש-Status הוא Success.</summary>
    public Dictionary<int, int> UnitGroupAssignments { get; }

    /// <summary>מנוע, זמן ריצה וערך אובייקטיבי מהפותר.</summary>
    public SolverMetaWire? Meta { get; }

    /// <summary>הודעות שגיאה — ריק בהצלחה.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>true כשהפותר נעצר בגלל timeout (לא בהכרח Infeasible).</summary>
    public bool IsTimeout { get; }

    public static CpSatSolveResult Success(Dictionary<int, int> assignments, SolverMetaWire meta) =>
        new(SolverJobStatus.Success, assignments, meta, Array.Empty<string>(), false);

    public static CpSatSolveResult Infeasible(SolverMetaWire meta) =>
        new(SolverJobStatus.Infeasible, new Dictionary<int, int>(), meta, Array.Empty<string>(), false);

    public static CpSatSolveResult Timeout(SolverMetaWire meta) =>
        new(SolverJobStatus.Timeout, new Dictionary<int, int>(), meta, new[] { "Solver timed out." }, true);

    public static CpSatSolveResult Failed(SolverMetaWire meta, IReadOnlyList<string> errors) =>
        new(SolverJobStatus.Failed, new Dictionary<int, int>(), meta, errors, false);
}
