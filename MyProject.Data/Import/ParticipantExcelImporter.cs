using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using MyProject.Data.Models;

namespace MyProject.Data.Import;

public sealed class ParticipantExcelImporter
{
    private readonly ApplicationDbContext _db;

    public ParticipantExcelImporter(ApplicationDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<ExcelImportResult> ImportAsync(
        Stream excelStream,
        int managerId,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        using var workbook = new XLWorkbook(excelStream);

        var participantsSheet = workbook.Worksheets.FirstOrDefault(
            worksheet => string.Equals(
                worksheet.Name,
                ExcelTemplateDefinitions.ParticipantsSheet,
                StringComparison.Ordinal));

        if (participantsSheet is null)
        {
            return Failed($"חסר גיליון '{ExcelTemplateDefinitions.ParticipantsSheet}'.");
        }

        var settingsSheet = workbook.Worksheets.FirstOrDefault(
            worksheet => string.Equals(
                worksheet.Name,
                ExcelTemplateDefinitions.SettingsSheet,
                StringComparison.Ordinal));

        if (settingsSheet is null)
        {
            return Failed($"חסר גיליון '{ExcelTemplateDefinitions.SettingsSheet}'.");
        }

        var pairsSheet = workbook.Worksheets.FirstOrDefault(
            worksheet => string.Equals(
                worksheet.Name,
                ExcelTemplateDefinitions.PairsSheet,
                StringComparison.Ordinal));

        var classificationRulesSheet = workbook.Worksheets.FirstOrDefault(
            worksheet => string.Equals(
                worksheet.Name,
                ExcelTemplateDefinitions.ClassificationRulesSheet,
                StringComparison.Ordinal));

        var parsedParticipants = ParseParticipants(participantsSheet, errors);
        var settings = ParseSettings(settingsSheet, errors);
        var pairs = pairsSheet is null
            ? new List<ParsedPair>()
            : ParsePairs(pairsSheet, errors);
        var classificationRules = classificationRulesSheet is null
            ? new List<ParsedClassificationRule>()
            : ParseClassificationRules(classificationRulesSheet, errors);

        ValidateParticipants(parsedParticipants, errors);
        ValidateSettings(settings, parsedParticipants.Count, errors);
        ValidatePairs(pairs, parsedParticipants, errors);
        ValidateClassificationRules(classificationRules, parsedParticipants, errors);

        if (errors.Count > 0)
        {
            return new ExcelImportResult { Errors = errors };
        }

        var assignmentId = await SaveAsync(
            parsedParticipants,
            settings!,
            pairs,
            classificationRules,
            managerId,
            cancellationToken);

        return new ExcelImportResult
        {
            AssignmentId = assignmentId,
            AssignmentName = settings!.AssignmentName,
            ParticipantCount = parsedParticipants.Count,
            MandatoryPairCount = pairs.Count(pair => pair.IsMandatory),
            ForbiddenPairCount = pairs.Count(pair => !pair.IsMandatory),
            ClassificationRuleCount = classificationRules.Count,
        };
    }

    private static ExcelImportResult Failed(string error) =>
        new() { Errors = new[] { error } };

    private static List<ParsedParticipant> ParseParticipants(IXLWorksheet sheet, List<string> errors)
    {
        var headerRow = sheet.Row(1);
        var lastColumn = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

        if (lastColumn == 0)
        {
            errors.Add($"גיליון '{ExcelTemplateDefinitions.ParticipantsSheet}': חסרה שורת כותרות.");
            return new List<ParsedParticipant>();
        }

        var columnIndexByHeader = new Dictionary<string, int>(StringComparer.Ordinal);
        var classificationColumns = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var column = 1; column <= lastColumn; column++)
        {
            var header = ReadCellText(headerRow.Cell(column));
            if (string.IsNullOrWhiteSpace(header))
            {
                continue;
            }

            if (string.Equals(header, ExcelTemplateDefinitions.IdentityColumn, StringComparison.Ordinal)
                || string.Equals(header, ExcelTemplateDefinitions.NameColumn, StringComparison.Ordinal))
            {
                columnIndexByHeader[header] = column;
                continue;
            }

            if (classificationColumns.ContainsKey(header))
            {
                errors.Add(
                    $"גיליון '{ExcelTemplateDefinitions.ParticipantsSheet}': עמודת סיווג '{header}' מופיעה פעמיים.");
                continue;
            }

            classificationColumns[header] = column;
        }

        if (!columnIndexByHeader.ContainsKey(ExcelTemplateDefinitions.IdentityColumn))
        {
            errors.Add(
                $"גיליון '{ExcelTemplateDefinitions.ParticipantsSheet}': חסרה עמודה '{ExcelTemplateDefinitions.IdentityColumn}'.");
        }

        if (classificationColumns.Count == 0)
        {
            errors.Add(
                $"גיליון '{ExcelTemplateDefinitions.ParticipantsSheet}': חסרה לפחות עמודת סיווג אחת.");
        }

        var participants = new List<ParsedParticipant>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            if (IsRowEmpty(row, lastColumn))
            {
                continue;
            }

            var identityRaw = columnIndexByHeader.TryGetValue(
                ExcelTemplateDefinitions.IdentityColumn,
                out var identityColumn)
                ? ReadCellText(row.Cell(identityColumn))
                : string.Empty;

            if (!TryNormalizeIdentity(identityRaw, out var identity, out var identityError))
            {
                errors.Add(
                    $"גיליון '{ExcelTemplateDefinitions.ParticipantsSheet}', שורה {rowNumber}: תעודת זהות {identityError}.");
                continue;
            }

            var name = columnIndexByHeader.TryGetValue(ExcelTemplateDefinitions.NameColumn, out var nameColumn)
                ? ReadCellText(row.Cell(nameColumn))
                : string.Empty;

            var classifications = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (dimensionCode, column) in classificationColumns)
            {
                var levelCode = ReadCellText(row.Cell(column));
                if (string.IsNullOrWhiteSpace(levelCode))
                {
                    errors.Add(
                        $"גיליון '{ExcelTemplateDefinitions.ParticipantsSheet}', שורה {rowNumber}: חסר ערך סיווג במימד '{dimensionCode}'.");
                    continue;
                }

                classifications[dimensionCode] = levelCode.Trim();
            }

            participants.Add(new ParsedParticipant(rowNumber, identity, name, classifications));
        }

        if (participants.Count == 0)
        {
            errors.Add($"גיליון '{ExcelTemplateDefinitions.ParticipantsSheet}': לא נמצאו משתתפים.");
        }

        return participants;
    }

    private static ParsedSettings? ParseSettings(IXLWorksheet sheet, List<string> errors)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var key = ReadCellText(sheet.Cell(rowNumber, 1));
            var value = ReadCellText(sheet.Cell(rowNumber, 2));
            if (string.IsNullOrWhiteSpace(key) && string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                errors.Add($"גיליון '{ExcelTemplateDefinitions.SettingsSheet}', שורה {rowNumber}: חסרה הגדרה.");
                continue;
            }

            values[key.Trim()] = value.Trim();
        }

        if (!values.TryGetValue(ExcelTemplateDefinitions.SettingAssignmentName, out var assignmentName)
            || string.IsNullOrWhiteSpace(assignmentName))
        {
            errors.Add($"גיליון '{ExcelTemplateDefinitions.SettingsSheet}': חסר '{ExcelTemplateDefinitions.SettingAssignmentName}'.");
        }

        if (!TryReadPositiveInt(values, ExcelTemplateDefinitions.SettingMinGroups, out var minGroups, errors, ExcelTemplateDefinitions.SettingsSheet))
        {
            minGroups = 0;
        }

        if (!TryReadPositiveInt(values, ExcelTemplateDefinitions.SettingMaxGroups, out var maxGroups, errors, ExcelTemplateDefinitions.SettingsSheet))
        {
            maxGroups = 0;
        }

        if (!TryReadPositiveInt(values, ExcelTemplateDefinitions.SettingMinGroupSize, out var minGroupSize, errors, ExcelTemplateDefinitions.SettingsSheet))
        {
            minGroupSize = 0;
        }

        if (!TryReadPositiveInt(values, ExcelTemplateDefinitions.SettingMaxGroupSize, out var maxGroupSize, errors, ExcelTemplateDefinitions.SettingsSheet))
        {
            maxGroupSize = 0;
        }

        if (string.IsNullOrWhiteSpace(assignmentName)
            || errors.Any(error => error.Contains(ExcelTemplateDefinitions.SettingsSheet, StringComparison.Ordinal)))
        {
            return null;
        }

        return new ParsedSettings(assignmentName.Trim(), minGroups, maxGroups, minGroupSize, maxGroupSize);
    }

    private static List<ParsedPair> ParsePairs(IXLWorksheet sheet, List<string> errors)
    {
        var pairs = new List<ParsedPair>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var type = ReadCellText(sheet.Cell(rowNumber, 1));
            var participantA = ReadCellText(sheet.Cell(rowNumber, 2));
            var participantB = ReadCellText(sheet.Cell(rowNumber, 3));

            if (string.IsNullOrWhiteSpace(type)
                && string.IsNullOrWhiteSpace(participantA)
                && string.IsNullOrWhiteSpace(participantB))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                errors.Add($"גיליון '{ExcelTemplateDefinitions.PairsSheet}', שורה {rowNumber}: חסר סוג זוג.");
                continue;
            }

            bool? isMandatory = type.Trim() switch
            {
                ExcelTemplateDefinitions.PairTypeMandatory => true,
                ExcelTemplateDefinitions.PairTypeForbidden => false,
                _ => null,
            };

            if (isMandatory is null)
            {
                errors.Add(
                    $"גיליון '{ExcelTemplateDefinitions.PairsSheet}', שורה {rowNumber}: סוג '{type}' לא תקין (חובה/איסור).");
                continue;
            }

            if (!TryNormalizeIdentity(participantA, out var identityA, out var errorA))
            {
                errors.Add(
                    $"גיליון '{ExcelTemplateDefinitions.PairsSheet}', שורה {rowNumber}: משתתף_א {errorA}.");
                continue;
            }

            if (!TryNormalizeIdentity(participantB, out var identityB, out var errorB))
            {
                errors.Add(
                    $"גיליון '{ExcelTemplateDefinitions.PairsSheet}', שורה {rowNumber}: משתתף_ב {errorB}.");
                continue;
            }

            pairs.Add(new ParsedPair(rowNumber, isMandatory.Value, identityA, identityB));
        }

        return pairs;
    }

    private static List<ParsedClassificationRule> ParseClassificationRules(
        IXLWorksheet sheet,
        List<string> errors)
    {
        var rules = new List<ParsedClassificationRule>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var dimension = ReadCellText(sheet.Cell(rowNumber, 1));
            var ruleType = ReadCellText(sheet.Cell(rowNumber, 2));

            if (string.IsNullOrWhiteSpace(dimension) && string.IsNullOrWhiteSpace(ruleType))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(dimension))
            {
                errors.Add(
                    $"גיליון '{ExcelTemplateDefinitions.ClassificationRulesSheet}', שורה {rowNumber}: חסר מימד.");
                continue;
            }

            bool? isBalance = ruleType.Trim() switch
            {
                ExcelTemplateDefinitions.RuleTypeBalance => true,
                ExcelTemplateDefinitions.RuleTypeSeparation => false,
                _ => null,
            };

            if (isBalance is null)
            {
                errors.Add(
                    $"גיליון '{ExcelTemplateDefinitions.ClassificationRulesSheet}', שורה {rowNumber}: סוג אילוץ '{ruleType}' לא תקין (איזון/הפרדה).");
                continue;
            }

            rules.Add(new ParsedClassificationRule(rowNumber, dimension.Trim(), isBalance.Value));
        }

        return rules;
    }

    private static void ValidateParticipants(IReadOnlyList<ParsedParticipant> participants, List<string> errors)
    {
        var duplicates = participants
            .GroupBy(participant => participant.IdentityNumber)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        foreach (var duplicate in duplicates)
        {
            errors.Add($"גיליון '{ExcelTemplateDefinitions.ParticipantsSheet}': תעודת זהות כפולה '{duplicate}'.");
        }

        foreach (var participant in participants.Where(participant => participant.Classifications.Count == 0))
        {
            errors.Add(
                $"גיליון '{ExcelTemplateDefinitions.ParticipantsSheet}', שורה {participant.RowNumber}: חסר סיווג.");
        }
    }

    private static void ValidateSettings(
        ParsedSettings? settings,
        int participantCount,
        List<string> errors)
    {
        if (settings is null)
        {
            return;
        }

        if (settings.MinGroups > settings.MaxGroups)
        {
            errors.Add(
                $"גיליון '{ExcelTemplateDefinitions.SettingsSheet}': מספר קבוצות מינימום גדול מהמקסימום.");
        }

        if (settings.MinGroupSize > settings.MaxGroupSize)
        {
            errors.Add(
                $"גיליון '{ExcelTemplateDefinitions.SettingsSheet}': גודל קבוצה מינימום גדול מהמקסימום.");
        }

        var minCapacity = settings.MinGroups * settings.MinGroupSize;
        var maxCapacity = settings.MaxGroups * settings.MaxGroupSize;

        if (participantCount < minCapacity || participantCount > maxCapacity)
        {
            errors.Add(
                $"גיליון '{ExcelTemplateDefinitions.SettingsSheet}': מספר המשתתפים ({participantCount}) לא מתאים לקיבולת הקבוצות ({minCapacity}-{maxCapacity}).");
        }
    }

    private static void ValidatePairs(
        IReadOnlyList<ParsedPair> pairs,
        IReadOnlyList<ParsedParticipant> participants,
        List<string> errors)
    {
        var identities = participants.Select(participant => participant.IdentityNumber).ToHashSet(StringComparer.Ordinal);
        var seenPairs = new HashSet<string>(StringComparer.Ordinal);

        foreach (var pair in pairs)
        {
            if (pair.ParticipantA == pair.ParticipantB)
            {
                errors.Add(
                    $"גיליון '{ExcelTemplateDefinitions.PairsSheet}', שורה {pair.RowNumber}: משתתף_א ומשתתף_ב זהים.");
            }

            if (!identities.Contains(pair.ParticipantA))
            {
                errors.Add(
                    $"גיליון '{ExcelTemplateDefinitions.PairsSheet}', שורה {pair.RowNumber}: משתתף_א '{pair.ParticipantA}' לא קיים בגיליון משתתפים.");
            }

            if (!identities.Contains(pair.ParticipantB))
            {
                errors.Add(
                    $"גיליון '{ExcelTemplateDefinitions.PairsSheet}', שורה {pair.RowNumber}: משתתף_ב '{pair.ParticipantB}' לא קיים בגיליון משתתפים.");
            }

            var pairKey = string.CompareOrdinal(pair.ParticipantA, pair.ParticipantB) <= 0
                ? $"{pair.ParticipantA}|{pair.ParticipantB}"
                : $"{pair.ParticipantB}|{pair.ParticipantA}";

            if (!seenPairs.Add(pairKey))
            {
                errors.Add(
                    $"גיליון '{ExcelTemplateDefinitions.PairsSheet}', שורה {pair.RowNumber}: זוג כפול.");
            }
        }
    }

    private static void ValidateClassificationRules(
        IReadOnlyList<ParsedClassificationRule> rules,
        IReadOnlyList<ParsedParticipant> participants,
        List<string> errors)
    {
        if (participants.Count == 0)
        {
            return;
        }

        var dimensions = participants
            .SelectMany(participant => participant.Classifications.Keys)
            .ToHashSet(StringComparer.Ordinal);

        var seenDimensions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rule in rules)
        {
            if (!dimensions.Contains(rule.DimensionCode))
            {
                errors.Add(
                    $"גיליון '{ExcelTemplateDefinitions.ClassificationRulesSheet}', שורה {rule.RowNumber}: מימד '{rule.DimensionCode}' לא קיים בגיליון משתתפים.");
            }

            if (!seenDimensions.Add(rule.DimensionCode))
            {
                errors.Add(
                    $"גיליון '{ExcelTemplateDefinitions.ClassificationRulesSheet}', שורה {rule.RowNumber}: מימד '{rule.DimensionCode}' מופיע פעמיים.");
            }
        }
    }

    private async Task<int> SaveAsync(
        IReadOnlyList<ParsedParticipant> participants,
        ParsedSettings settings,
        IReadOnlyList<ParsedPair> pairs,
        IReadOnlyList<ParsedClassificationRule> classificationRules,
        int managerId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var manager = await _db.Managers
            .FirstOrDefaultAsync(entry => entry.ManagerId == managerId, cancellationToken);

        if (manager is null)
        {
            throw new InvalidOperationException($"Manager {managerId} was not found.");
        }

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

        var existingParticipants = await _db.Participants
            .Where(participant => participants.Select(row => row.IdentityNumber).Contains(participant.IsraeliIdentityNumber))
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

        foreach (var rule in classificationRules)
        {
            var dimension = dimensionByCode[rule.DimensionCode];
            _db.AssignmentClassificationConstraints.Add(new AssignmentClassificationConstraint
            {
                AssignmentId = assignment.AssignmentId,
                ClassificationDimensionId = dimension.ClassificationDimensionId,
                IsBalanceOrSeparation = rule.IsBalance,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return assignment.AssignmentId;
    }

    private static bool TryReadPositiveInt(
        IReadOnlyDictionary<string, string> values,
        string key,
        out int parsed,
        List<string> errors,
        string sheetName)
    {
        parsed = 0;
        if (!values.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            errors.Add($"גיליון '{sheetName}': חסר '{key}'.");
            return false;
        }

        if (!int.TryParse(raw, out parsed) || parsed <= 0)
        {
            errors.Add($"גיליון '{sheetName}': '{key}' חייב להיות מספר חיובי.");
            return false;
        }

        return true;
    }

    private static bool IsRowEmpty(IXLRow row, int lastColumn)
    {
        for (var column = 1; column <= lastColumn; column++)
        {
            if (!string.IsNullOrWhiteSpace(ReadCellText(row.Cell(column))))
            {
                return false;
            }
        }

        return true;
    }

    private static string ReadCellText(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        if (cell.DataType == XLDataType.Number)
        {
            return ((long)cell.GetDouble()).ToString();
        }

        return cell.GetFormattedString().Trim();
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

    private sealed record ParsedParticipant(
        int RowNumber,
        string IdentityNumber,
        string Name,
        IReadOnlyDictionary<string, string> Classifications);

    private sealed record ParsedSettings(
        string AssignmentName,
        int MinGroups,
        int MaxGroups,
        int MinGroupSize,
        int MaxGroupSize);

    private sealed record ParsedPair(
        int RowNumber,
        bool IsMandatory,
        string ParticipantA,
        string ParticipantB);

    private sealed record ParsedClassificationRule(
        int RowNumber,
        string DimensionCode,
        bool IsBalance);
}
