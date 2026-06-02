using Microsoft.EntityFrameworkCore;
using MyProject.API.Placement.Excel;
using MyProject.Data;
using MyProject.Data.Import;
using MyProject.Data.Models;

namespace MyProject.API.Placement;

public sealed class AssignmentSingleSheetImporter
{
    private readonly ApplicationDbContext _db;
    private readonly ParticipantsExcelWorkbookReader _reader;
    private readonly ParticipantsExcelValidator _validator;

    public AssignmentSingleSheetImporter(
        ApplicationDbContext db,
        ParticipantsExcelWorkbookReader reader,
        ParticipantsExcelValidator validator)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<ExcelImportResult> ImportAsync(
        Stream excelStream,
        int managerId,
        string assignmentName,
        int groupCount,
        int minGroupSize,
        int maxGroupSize,
        CancellationToken cancellationToken = default)
    {
        var parseResult = _reader.Read(excelStream);
        var validationResult = _validator.Validate(
            parseResult,
            groupCount,
            minGroupSize,
            maxGroupSize);

        if (!validationResult.IsValid)
        {
            return new ExcelImportResult { Errors = validationResult.Errors.ToList() };
        }

        var rows = parseResult.Rows;
        var participants = rows
            .Select(row => new ParsedParticipantRecord(
                row.ParticipantId.Trim(),
                row.FullName.Trim(),
                row.Classifications))
            .ToList();

        var pairs = BuildPairs(rows);
        var mandatoryCount = pairs.Count(pair => pair.IsMandatory);
        var forbiddenCount = pairs.Count(pair => !pair.IsMandatory);

        var settings = new ParsedSettingsRecord(
            string.IsNullOrWhiteSpace(assignmentName) ? "חלוקה חדשה" : assignmentName.Trim(),
            groupCount,
            groupCount,
            minGroupSize,
            maxGroupSize);

        var assignmentId = await SaveAsync(
            participants,
            settings,
            pairs,
            managerId,
            cancellationToken);

        return new ExcelImportResult
        {
            AssignmentId = assignmentId,
            AssignmentName = settings.AssignmentName,
            ParticipantCount = participants.Count,
            MandatoryPairCount = mandatoryCount,
            ForbiddenPairCount = forbiddenCount,
            ClassificationRuleCount = 0,
        };
    }

    /// <summary>
    /// יוצר חלוקה חדשה ממשתתפים שנבחרו בממשק (ללא Excel).
    /// </summary>
    public async Task<ExcelImportResult> CreateFromParticipantsAsync(
        CreateAssignmentFromParticipantsRequest request,
        int managerId,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var errors = new List<string>();

        if (request.Participants.Count == 0)
        {
            errors.Add("יש לבחור לפחות משתתף אחד.");
        }

        if (request.GroupCount <= 0)
        {
            errors.Add("מספר קבוצות חייב להיות חיובי.");
        }

        if (request.MinGroupSize <= 0 || request.MaxGroupSize <= 0)
        {
            errors.Add("גודל קבוצה חייב להיות חיובי.");
        }

        if (request.MinGroupSize > request.MaxGroupSize)
        {
            errors.Add("גודל קבוצה מינימום גדול מהמקסימום.");
        }

        var minCapacity = request.GroupCount * request.MinGroupSize;
        var maxCapacity = request.GroupCount * request.MaxGroupSize;

        var parsedParticipants = new List<ParsedParticipantRecord>();
        var seenIdentities = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in request.Participants)
        {
            if (!TryNormalizeIdentity(entry.ParticipantId, out var identity, out var identityError))
            {
                errors.Add($"תעודת זהות {identityError}.");
                continue;
            }

            if (!seenIdentities.Add(identity))
            {
                continue;
            }

            parsedParticipants.Add(new ParsedParticipantRecord(
                identity,
                entry.DisplayName?.Trim() ?? string.Empty,
                entry.Classifications));
        }

        if (parsedParticipants.Count < minCapacity || parsedParticipants.Count > maxCapacity)
        {
            errors.Add(
                $"מספר המשתתפים ({parsedParticipants.Count}) לא מתאים לקיבולת הקבוצות ({minCapacity}-{maxCapacity}).");
        }

        if (errors.Count > 0)
        {
            return new ExcelImportResult { Errors = errors.Distinct(StringComparer.Ordinal).ToList() };
        }

        var settings = new ParsedSettingsRecord(
            string.IsNullOrWhiteSpace(request.AssignmentName) ? "חלוקה חדשה" : request.AssignmentName.Trim(),
            request.GroupCount,
            request.GroupCount,
            request.MinGroupSize,
            request.MaxGroupSize);

        var assignmentId = await SaveAsync(
            parsedParticipants,
            settings,
            Array.Empty<ParsedPairRecord>(),
            managerId,
            cancellationToken);

        return new ExcelImportResult
        {
            AssignmentId = assignmentId,
            AssignmentName = settings.AssignmentName,
            ParticipantCount = parsedParticipants.Count,
            MandatoryPairCount = 0,
            ForbiddenPairCount = 0,
            ClassificationRuleCount = 0,
        };
    }

    private static bool TryNormalizeIdentity(string raw, out string normalized, out string error)
    {
        normalized = string.Empty;
        error = "לא תקינה";

        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "חסרה";
            return false;
        }

        var trimmed = raw.Trim();
        var decimalSeparatorIndex = trimmed.IndexOf('.');
        if (decimalSeparatorIndex >= 0)
        {
            trimmed = trimmed[..decimalSeparatorIndex];
        }

        if (trimmed.Length != 9 || !trimmed.All(char.IsDigit))
        {
            return false;
        }

        normalized = trimmed;
        return true;
    }

    private static List<ParsedPairRecord> BuildPairs(IReadOnlyList<ParsedParticipantRow> rows)
    {
        var pairs = new List<ParsedPairRecord>();
        var seenMandatory = new HashSet<string>(StringComparer.Ordinal);
        var seenForbidden = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            foreach (var other in row.MandatoryWith)
            {
                var key = PairKey(row.ParticipantId, other);
                if (seenMandatory.Add(key))
                {
                    pairs.Add(new ParsedPairRecord(true, OrderPair(row.ParticipantId, other)));
                }
            }

            foreach (var other in row.ForbiddenWith)
            {
                var key = PairKey(row.ParticipantId, other);
                if (seenForbidden.Add(key))
                {
                    pairs.Add(new ParsedPairRecord(false, OrderPair(row.ParticipantId, other)));
                }
            }
        }

        return pairs;
    }

    private static (string A, string B) OrderPair(string participantA, string participantB) =>
        string.CompareOrdinal(participantA, participantB) <= 0
            ? (participantA, participantB)
            : (participantB, participantA);

    private static string PairKey(string participantA, string participantB)
    {
        var ordered = OrderPair(participantA, participantB);
        return $"{ordered.A}|{ordered.B}";
    }

    private async Task<int> SaveAsync(
        IReadOnlyList<ParsedParticipantRecord> participants,
        ParsedSettingsRecord settings,
        IReadOnlyList<ParsedPairRecord> pairs,
        int managerId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var manager = await _db.Managers
            .FirstOrDefaultAsync(entry => entry.ManagerId == managerId, cancellationToken)
            ?? throw new InvalidOperationException($"Manager {managerId} was not found.");

        var managementGroup = await _db.ManagementGroups
            .FirstOrDefaultAsync(entry => entry.ManagerId == managerId, cancellationToken);

        if (managementGroup is null)
        {
            managementGroup = new ManagementGroup
            {
                ManagerId = manager.ManagerId,
                ManagementGroupName = $"קבוצת ניהול - {manager.ManagerName}",
            };
            _db.ManagementGroups.Add(managementGroup);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var dimensionByCode = await ClassificationCatalogHelper.LoadDimensionLookupAsync(_db, cancellationToken);

        var levels = await _db.ClassificationLevels.ToListAsync(cancellationToken);
        var levelIdByDimensionAndCode = levels.ToDictionary(
            level => (level.ClassificationDimensionId, level.LevelCode),
            level => level.ClassificationLevelId);

        foreach (var participant in participants)
        {
            foreach (var (dimensionCode, levelCode) in participant.Classifications)
            {
                var dimension = await ClassificationCatalogHelper.GetOrCreateDimensionAsync(
                    _db,
                    dimensionByCode,
                    dimensionCode,
                    cancellationToken);

                await ClassificationCatalogHelper.GetOrCreateLevelIdAsync(
                    _db,
                    levelIdByDimensionAndCode,
                    dimension,
                    levelCode,
                    cancellationToken);
            }
        }

        var identityNumbers = participants.Select(entry => entry.IdentityNumber).ToList();
        var existingParticipants = await _db.Participants
            .Where(participant => identityNumbers.Contains(participant.IsraeliIdentityNumber))
            .ToDictionaryAsync(participant => participant.IsraeliIdentityNumber, cancellationToken);

        var dbParticipants = new List<Participant>();
        foreach (var participant in participants)
        {
            if (existingParticipants.TryGetValue(participant.IdentityNumber, out var existing))
            {
                if (!string.IsNullOrWhiteSpace(participant.Name))
                {
                    existing.ParticipantName = participant.Name;
                }

                dbParticipants.Add(existing);
                continue;
            }

            var created = new Participant
            {
                IsraeliIdentityNumber = participant.IdentityNumber,
                ParticipantName = string.IsNullOrWhiteSpace(participant.Name) ? null : participant.Name,
            };
            _db.Participants.Add(created);
            dbParticipants.Add(created);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var assignment = new Assignment
        {
            AssignmentName = settings.AssignmentName,
            ManagementGroupId = managementGroup.ManagementGroupId,
        };
        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);

        var participantAssignments = dbParticipants
            .Select(participant => new ParticipantAssignment
            {
                ParticipantId = participant.ParticipantId,
                AssignmentId = assignment.AssignmentId,
                ManagerId = manager.ManagerId,
            })
            .ToList();

        _db.ParticipantAssignments.AddRange(participantAssignments);
        await _db.SaveChangesAsync(cancellationToken);

        var assignmentByIdentity = participantAssignments
            .Join(
                dbParticipants,
                participantAssignment => participantAssignment.ParticipantId,
                participant => participant.ParticipantId,
                (participantAssignment, participant) => new
                {
                    participant.IsraeliIdentityNumber,
                    participantAssignment.ParticipantAssignmentId,
                })
            .ToDictionary(entry => entry.IsraeliIdentityNumber, entry => entry.ParticipantAssignmentId);

        foreach (var parsedParticipant in participants)
        {
            var participantAssignmentId = assignmentByIdentity[parsedParticipant.IdentityNumber];
            foreach (var (dimensionCode, levelCode) in parsedParticipant.Classifications)
            {
                var dimension = dimensionByCode[dimensionCode];
                var levelId = levelIdByDimensionAndCode[(dimension.ClassificationDimensionId, levelCode)];

                _db.ParticipantClassifications.Add(new ParticipantClassification
                {
                    ParticipantAssignmentId = participantAssignmentId,
                    ClassificationDimensionId = dimension.ClassificationDimensionId,
                    ClassificationLevelId = levelId,
                });
            }
        }

        _db.GroupCountConstraints.Add(new GroupCountConstraint
        {
            AssignmentId = assignment.AssignmentId,
            MinGroups = settings.MinGroups,
            MaxGroups = settings.MaxGroups,
        });

        for (var groupId = 1; groupId <= settings.MaxGroups; groupId++)
        {
            _db.GroupSizeConstraints.Add(new GroupSizeConstraint
            {
                AssignmentId = assignment.AssignmentId,
                GroupId = groupId,
                MinGroupSize = settings.MinGroupSize,
                MaxGroupSize = settings.MaxGroupSize,
            });
        }

        foreach (var pair in pairs)
        {
            var firstParticipantAssignmentId = assignmentByIdentity[pair.ParticipantA];
            var secondParticipantAssignmentId = assignmentByIdentity[pair.ParticipantB];

            if (pair.IsMandatory)
            {
                _db.MandatoryPairConstraints.Add(new MandatoryPairConstraint
                {
                    AssignmentId = assignment.AssignmentId,
                    FirstParticipantAssignmentId = firstParticipantAssignmentId,
                    SecondParticipantAssignmentId = secondParticipantAssignmentId,
                });
            }
            else
            {
                _db.ForbiddenPairConstraints.Add(new ForbiddenPairConstraint
                {
                    AssignmentId = assignment.AssignmentId,
                    FirstParticipantAssignmentId = firstParticipantAssignmentId,
                    SecondParticipantAssignmentId = secondParticipantAssignmentId,
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return assignment.AssignmentId;
    }

    private sealed record ParsedParticipantRecord(
        string IdentityNumber,
        string Name,
        IReadOnlyDictionary<string, string> Classifications);

    private sealed record ParsedSettingsRecord(
        string AssignmentName,
        int MinGroups,
        int MaxGroups,
        int MinGroupSize,
        int MaxGroupSize);

    private sealed record ParsedPairRecord(bool IsMandatory, (string A, string B) Participants)
    {
        public string ParticipantA => Participants.A;

        public string ParticipantB => Participants.B;
    }
}
