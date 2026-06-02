using Google.OrTools.Sat;
using MyProject.SolverService.Contracts;

namespace MyProject.SolverService.Solving;

public sealed class CpSatPlacementSolver
{
    public CpSatSolveResult Solve(SolverJobRequest request)
    {
        var model = new CpModel();
        var units = request.PlacementUnits;
        var groups = request.Groups;
        var totalParticipants = units.Sum(unit => unit.ParticipantIds.Count);

        var x = new Dictionary<(int UnitId, int GroupId), BoolVar>();
        foreach (var unit in units)
        {
            foreach (var group in groups)
            {
                x[(unit.UnitId, group.GroupId)] = model.NewBoolVar($"x_u{unit.UnitId}_g{group.GroupId}");
            }
        }

        foreach (var unit in units)
        {
            var literals = groups.Select(group => x[(unit.UnitId, group.GroupId)]).ToArray();
            model.AddExactlyOne(literals);
        }

        foreach (var group in groups)
        {
            var loadTerms = units
                .Select(unit => LinearExpr.Term(x[(unit.UnitId, group.GroupId)], unit.ParticipantIds.Count))
                .ToArray();
            var load = LinearExpr.Sum(loadTerms);
            model.Add(load >= group.MinSize);
            model.Add(load <= group.MaxSize);
        }

        foreach (var pair in request.ForbiddenUnitPairs)
        {
            foreach (var group in groups)
            {
                model.Add(
                    x[(pair.FirstUnitId, group.GroupId)] + x[(pair.SecondUnitId, group.GroupId)] <= 1);
            }
        }

        var classByParticipant = request.Participants.ToDictionary(
            participant => participant.ParticipantId,
            participant => participant.Classifications,
            StringComparer.Ordinal);

        AddClassificationConstraints(model, request, units, groups, x, classByParticipant, totalParticipants);

        var solver = new CpSolver();
        var timeoutSeconds = Math.Max(1.0, request.TimeoutMs / 1000.0);
        solver.StringParameters = $"max_time_in_seconds:{timeoutSeconds}";

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var status = solver.Solve(model);
        stopwatch.Stop();

        return MapResult(status, solver, x, units, groups, stopwatch.ElapsedMilliseconds, request.TimeoutMs);
    }

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
                case "ClassificationBalanceConstraint":
                    AddBalanceConstraint(model, units, groups, x, classByParticipant, constraint, totalParticipants);
                    break;
                case "ClassificationHomogeneousGroupConstraint":
                    AddHomogeneousConstraint(model, units, groups, x, classByParticipant, constraint, totalParticipants);
                    break;
                case "ClassificationProportionalBalanceConstraint":
                    AddProportionalConstraint(model, units, groups, x, classByParticipant, constraint, totalParticipants);
                    break;
            }
        }
    }

    private static void AddBalanceConstraint(
        CpModel model,
        List<PlacementUnitWire> units,
        List<SolverGroupWire> groups,
        Dictionary<(int UnitId, int GroupId), BoolVar> x,
        Dictionary<string, Dictionary<string, string>> classByParticipant,
        ClassificationConstraintWire constraint,
        int totalParticipants)
    {
        if (constraint.TargetLevel is null
            || constraint.MinCountPerGroup is null
            || constraint.MaxCountPerGroup is null)
        {
            return;
        }

        foreach (var group in groups)
        {
            var count = BuildLevelCountVar(
                model,
                units,
                group,
                x,
                classByParticipant,
                constraint.TargetDimension,
                constraint.TargetLevel,
                totalParticipants,
                $"bal_g{group.GroupId}_{constraint.TargetLevel}");

            model.Add(count >= constraint.MinCountPerGroup.Value);
            model.Add(count <= constraint.MaxCountPerGroup.Value);
        }
    }

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

                var hasLevel = model.NewBoolVar($"has_g{group.GroupId}_{level}");
                model.Add(count >= 1).OnlyEnforceIf(hasLevel);
                model.Add(count == 0).OnlyEnforceIf(hasLevel.Not());
                hasLevelVars.Add(hasLevel);
            }

            model.Add(LinearExpr.Sum(hasLevelVars) <= 1);
        }
    }

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

        var globalCounts = levels.ToDictionary(
            level => level,
            level => CountParticipantsWithLevel(units, classByParticipant, constraint.TargetDimension, level),
            StringComparer.Ordinal);

        foreach (var group in groups)
        {
            var groupSizeTerms = units
                .Select(unit => LinearExpr.Term(x[(unit.UnitId, group.GroupId)], unit.ParticipantIds.Count))
                .ToArray();
            var groupSize = model.NewIntVar(0, totalParticipants, $"gs_g{group.GroupId}");
            model.Add(groupSize == LinearExpr.Sum(groupSizeTerms));

            foreach (var level in levels)
            {
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
                var deviation = model.NewIntVar(-totalParticipants * totalParticipants, totalParticipants * totalParticipants,
                    $"dev_g{group.GroupId}_{level}");
                model.Add(deviation == totalParticipants * count - globalCount * groupSize);
                model.Add(deviation <= maxDeviation);
                model.Add(deviation >= -maxDeviation);
            }
        }
    }

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
            var matchingCount = unit.ParticipantIds.Count(participantId =>
                classByParticipant.TryGetValue(participantId, out var map)
                && map.TryGetValue(dimension, out var participantLevel)
                && string.Equals(participantLevel, level, StringComparison.Ordinal));

            if (matchingCount > 0)
            {
                terms.Add(LinearExpr.Term(x[(unit.UnitId, group.GroupId)], matchingCount));
            }
        }

        var count = model.NewIntVar(0, totalParticipants, name);
        model.Add(count == (terms.Count == 0 ? LinearExpr.Constant(0) : LinearExpr.Sum(terms)));
        return count;
    }

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
            return CpSatSolveResult.Timeout(meta);
        }

        return CpSatSolveResult.Failed(
            meta,
            new[] { $"CP-SAT returned status {status}." });
    }
}

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

    public SolverJobStatus Status { get; }

    public Dictionary<int, int> UnitGroupAssignments { get; }

    public SolverMetaWire? Meta { get; }

    public IReadOnlyList<string> Errors { get; }

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
