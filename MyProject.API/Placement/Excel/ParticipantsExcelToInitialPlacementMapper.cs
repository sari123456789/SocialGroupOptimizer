using MyProject.BL.Algorithm.InitialPlacement;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;
using InitialPlacementExecutionContext = MyProject.BL.Algorithm.InitialPlacement.Runtime.ExecutionContext;

namespace MyProject.API.Placement.Excel;

public sealed class ParticipantsExcelToInitialPlacementMapper
{
    private static readonly ClassificationDimensionCode DefaultDimension = new("Default");
    private static readonly ClassificationLevelCode DefaultLevel = new("General");

    public ParticipantsExcelMappingResult Map(
        ParticipantsExcelParseResult parseResult,
        int groupCount,
        int minGroupSize,
        int maxGroupSize)
    {
        if (parseResult is null)
        {
            throw new ArgumentNullException(nameof(parseResult));
        }

        var errors = new List<string>();

        if (!parseResult.Success)
        {
            errors.Add("לא ניתן למפות קובץ שלא עבר parse תקין.");
            errors.AddRange(parseResult.Errors);
            return ParticipantsExcelMappingResult.Failed(errors);
        }

        if (parseResult.Rows.Count == 0)
        {
            return ParticipantsExcelMappingResult.Failed(new[]
            {
                "לא נמצאו משתתפים למיפוי.",
            });
        }

        var participantFullNamesById = new Dictionary<string, string>(StringComparer.Ordinal);
        var participants = new List<Participant>();

        foreach (var row in parseResult.Rows.OrderBy(entry => entry.RowNumber))
        {
            if (!TryNormalizeParticipantId(row.ParticipantId, out var normalizedParticipantId))
            {
                errors.Add($"שורה {row.RowNumber}: ParticipantId '{row.ParticipantId}' לא תקין.");
                continue;
            }

            participantFullNamesById[normalizedParticipantId] = row.FullName;

            try
            {
                var participant = MapParticipant(row, normalizedParticipantId);
                participants.Add(participant);
            }
            catch (ArgumentException ex)
            {
                errors.Add($"שורה {row.RowNumber}: {ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            return ParticipantsExcelMappingResult.Failed(errors);
        }

        var constraints = BuildConstraints(
            parseResult.Rows,
            groupCount,
            minGroupSize,
            maxGroupSize,
            errors);

        if (errors.Count > 0)
        {
            return ParticipantsExcelMappingResult.Failed(errors);
        }

        var executionContext = new InitialPlacementExecutionContext(
            AlgorithmRunId.New(),
            Environment.TickCount);

        var input = new InitialPlacementInput(executionContext, participants, constraints);

        return new ParticipantsExcelMappingResult(
            input,
            participantFullNamesById,
            Array.Empty<string>());
    }

    private static Participant MapParticipant(ParsedParticipantRow row, string normalizedParticipantId)
    {
        var participantId = new ParticipantId(normalizedParticipantId);
        var classifications = BuildClassifications(row);
        var preferences = BuildPreferences(row);

        return new Participant(participantId, classifications, preferences);
    }

    private static Dictionary<ClassificationDimensionCode, ClassificationLevelCode> BuildClassifications(
        ParsedParticipantRow row)
    {
        var classifications = new Dictionary<ClassificationDimensionCode, ClassificationLevelCode>();

        foreach (var (dimensionCode, levelCode) in row.Classifications)
        {
            var dimension = new ClassificationDimensionCode(dimensionCode);
            if (classifications.ContainsKey(dimension))
            {
                throw new ArgumentException($"Duplicate classification dimension '{dimensionCode}'.");
            }

            classifications[dimension] = new ClassificationLevelCode(levelCode);
        }

        if (classifications.Count == 0)
        {
            classifications[DefaultDimension] = DefaultLevel;
        }

        return classifications;
    }

    private static List<Preference> BuildPreferences(ParsedParticipantRow row)
    {
        var preferences = new List<Preference>(row.Preferences.Count);

        for (var index = 0; index < row.Preferences.Count; index++)
        {
            if (!TryNormalizeParticipantId(row.Preferences[index], out var preferredParticipantId))
            {
                throw new ArgumentException(
                    $"Preferences מכיל מזהה לא תקין '{row.Preferences[index]}'.");
            }

            preferences.Add(new Preference(new ParticipantId(preferredParticipantId), index + 1));
        }

        return preferences;
    }

    private static List<IConstraint> BuildConstraints(
        IReadOnlyList<ParsedParticipantRow> rows,
        int groupCount,
        int minGroupSize,
        int maxGroupSize,
        List<string> errors)
    {
        var constraints = new List<IConstraint>
        {
            new GroupCountConstraint(groupCount, groupCount),
        };

        for (var groupId = 1; groupId <= groupCount; groupId++)
        {
            constraints.Add(new GroupSizeConstraint(
                new GroupId(groupId),
                minGroupSize,
                new GroupCapacity(maxGroupSize)));
        }

        var mandatoryPairs = CollectUniquePairs(
            rows,
            row => row.MandatoryWith,
            errors,
            "MandatoryWith");

        foreach (var (firstParticipantId, secondParticipantId) in mandatoryPairs)
        {
            constraints.Add(new MandatoryPairConstraint(
                new ParticipantId(firstParticipantId),
                new ParticipantId(secondParticipantId)));
        }

        var forbiddenPairs = CollectUniquePairs(
            rows,
            row => row.ForbiddenWith,
            errors,
            "ForbiddenWith");

        foreach (var (firstParticipantId, secondParticipantId) in forbiddenPairs)
        {
            constraints.Add(new ForbiddenPairConstraint(
                new ParticipantId(firstParticipantId),
                new ParticipantId(secondParticipantId)));
        }

        return constraints;
    }

    private static IReadOnlyList<(string FirstParticipantId, string SecondParticipantId)> CollectUniquePairs(
        IReadOnlyList<ParsedParticipantRow> rows,
        Func<ParsedParticipantRow, IReadOnlyList<string>> pairSelector,
        List<string> errors,
        string columnName)
    {
        var uniquePairs = new Dictionary<string, (string FirstParticipantId, string SecondParticipantId)>(
            StringComparer.Ordinal);

        foreach (var row in rows.OrderBy(entry => entry.RowNumber))
        {
            if (!TryNormalizeParticipantId(row.ParticipantId, out var normalizedParticipantId))
            {
                continue;
            }

            foreach (var otherParticipantIdRaw in pairSelector(row))
            {
                if (!TryNormalizeParticipantId(otherParticipantIdRaw, out var normalizedOtherParticipantId))
                {
                    errors.Add(
                        $"שורה {row.RowNumber}: {columnName} מכיל מזהה לא תקין '{otherParticipantIdRaw}'.");
                    continue;
                }

                var pairKey = ParticipantPairNormalizer.CreatePairKey(
                    normalizedParticipantId,
                    normalizedOtherParticipantId);

                uniquePairs.TryAdd(
                    pairKey,
                    ParticipantPairNormalizer.NormalizePair(
                        normalizedParticipantId,
                        normalizedOtherParticipantId));
            }
        }

        return uniquePairs.Values.ToList();
    }

    private static bool TryNormalizeParticipantId(string raw, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(raw))
        {
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
}
