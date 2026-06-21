using Microsoft.EntityFrameworkCore;
using MyProject.API.Placement.Excel;
using MyProject.Data;
using MyProject.Data.Import;
using MyProject.Data.Models;

namespace MyProject.API.Placement;

/// <summary>
/// מייבא חלוקה מלאה מקובץ Excel בעל גיליון יחיד ושומר אותה כחלוקה במערכת.
/// </summary>
/// <remarks>
/// מחלקה זו מטפלת במסלול ייבוא שבו קובץ אחד מכיל גם משתתפים וגם הגדרות
/// ואילוצים. היא אחראית על קריאת הנתונים, בניית Assignment, יצירת שיוכי
/// משתתפים לחלוקה, שמירת סיווגים, שמירת זוגות חובה/אסורים ושמירת העדפות
/// חברתיות. לאחר הייבוא, הנתונים נשמרים באותן טבלאות שבהן משתמשת המערכת
/// גם בעריכה ידנית, ולכן המשך העבודה במסכים ובאלגוריתם זהה לחלוקה שנוצרה
/// ידנית.
///
/// המחלקה אינה מריצה את האלגוריתם בעצמה. הפלט שלה הוא תוצאת ייבוא שמסבירה
/// האם השמירה הצליחה ומה מזהה החלוקה שנוצרה. לאחר מכן ניתן להריץ אימות
/// או חלוקה דרך השירותים הרגילים.
/// </remarks>
public sealed class AssignmentSingleSheetImporter
{
    // הקשר למסד — כל השמירה עוברת דרכו
    private readonly ApplicationDbContext _db;
    // קורא את הגיליון מהאקסל
    private readonly ParticipantsExcelWorkbookReader _reader;
    // בודק שהנתונים עומדים בכללים (גודל קבוצות וכו')
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

    // ייבוא מלא מקובץ אקסל — המסלול הראשי
    public async Task<ExcelImportResult> ImportAsync(
        Stream excelStream,
        int managerId,
        string assignmentName,
        int groupCount,
        int minGroupSize,
        int maxGroupSize,
        CancellationToken cancellationToken = default)
    {
        // קודם קוראים את השורות מהגיליון
        var parseResult = _reader.Read(excelStream);
        // ואז בודקים שהכל תקין ביחס להגדרות הקבוצות
        var validationResult = _validator.Validate(
            parseResult,
            groupCount,
            minGroupSize,
            maxGroupSize);

        // אם הוולידציה נכשלה — מחזירים רק שגיאות, בלי לגעת במסד
        if (!validationResult.IsValid)
        {
            return new ExcelImportResult { Errors = validationResult.Errors.ToList() };
        }

        var rows = parseResult.Rows;
        // הופכים כל שורת אקסל לרשומת משתתף מנורמלת
        var participants = rows
            .Select(row => new ParsedParticipantRecord(
                row.ParticipantId.Trim(),
                row.FullName.Trim(),
                row.Classifications,
                row.Preferences))
            .ToList();

        // אוספים זוגות חובה ואיסור מכל השורות
        var pairs = BuildPairs(rows);
        var mandatoryCount = pairs.Count(pair => pair.IsMandatory);
        var forbiddenCount = pairs.Count(pair => !pair.IsMandatory);

        // בונים את הגדרות החלוקה (שם, מספר קבוצות, גדלים)
        var settings = new ParsedSettingsRecord(
            string.IsNullOrWhiteSpace(assignmentName) ? "חלוקה חדשה" : assignmentName.Trim(),
            groupCount,
            groupCount,
            minGroupSize,
            maxGroupSize);

        // שומרים הכל למסד בתוך טרנזקציה
        var assignmentId = await SaveAsync(
            participants,
            settings,
            pairs,
            managerId,
            cancellationToken);

        // מחזירים סיכום מוצלח ללקוח
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

        // חייב להיות לפחות משתתף אחד
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

        // כמה משתתפים בדיוק צריכים להיכנס לפי הגדרות הקבוצות
        var minCapacity = request.GroupCount * request.MinGroupSize;
        var maxCapacity = request.GroupCount * request.MaxGroupSize;

        var parsedParticipants = new List<ParsedParticipantRecord>();
        // כדי שלא יכנסו שני משתתפים עם אותה ת.ז.
        var seenIdentities = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in request.Participants)
        {
            // מנרמלים ת.ז. — 9 ספרות, בלי .0 מאקסל
            if (!TryNormalizeIdentity(entry.ParticipantId, out var identity, out var identityError))
            {
                errors.Add($"תעודת זהות {identityError}.");
                continue;
            }

            // כפילות — מדלגים בשקט (כבר נוסף)
            if (!seenIdentities.Add(identity))
            {
                continue;
            }

            parsedParticipants.Add(new ParsedParticipantRecord(
                identity,
                entry.DisplayName?.Trim() ?? string.Empty,
                entry.Classifications,
                entry.Preferences ?? new List<string>()));
        }

        // בודקים שהמספר מתאים לקיבולת
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

        // במסלול מהממשק אין זוגות — שולחים רשימה ריקה
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

    // מנקה ת.ז. — חותך .0 מאקסל ומוודא 9 ספרות
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
        // אקסל לפעמים שומר מספר עם נקודה עשרונית
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

    // אוסף את כל זוגות החובה והאיסור מהשורות, בלי כפילויות
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

    // מסדר שני מזהים בסדר קבוע — כדי שלא יהיו כפילויות (א,ב) ו-(ב,א)
    private static (string A, string B) OrderPair(string participantA, string participantB) =>
        string.CompareOrdinal(participantA, participantB) <= 0
            ? (participantA, participantB)
            : (participantB, participantA);

    // מפתח ייחודי לזוג — לזיהוי כפילויות
    private static string PairKey(string participantA, string participantB)
    {
        var ordered = OrderPair(participantA, participantB);
        return $"{ordered.A}|{ordered.B}";
    }

    // השמירה האמיתית — הכל בתוך טרנזקציה אחת
    private async Task<int> SaveAsync(
        IReadOnlyList<ParsedParticipantRecord> participants,
        ParsedSettingsRecord settings,
        IReadOnlyList<ParsedPairRecord> pairs,
        int managerId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        // מוודאים שהמנהל קיים
        var manager = await _db.Managers
            .FirstOrDefaultAsync(entry => entry.ManagerId == managerId, cancellationToken)
            ?? throw new InvalidOperationException($"Manager {managerId} was not found.");

        // כל חלוקה שייכת לקבוצת ניהול — מוצאים או יוצרים
        var managementGroup = await _db.ManagementGroups
            .FirstOrDefaultAsync(entry => entry.ManagerId == managerId, cancellationToken);

        if (managementGroup is null)
        {
            managementGroup = new ManagementGroup
            {
                ManagerId = manager.ManagerId,
                ManagementGroupName = $"קבוצת ניהול - {manager.ManagerName}",
            };
            //הוספת קבוצת הניהול
            _db.ManagementGroups.Add(managementGroup);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // טוענים מימדי סיווג — ויוצרים חסרים לפי הצורך
        var dimensionByCode = await ClassificationCatalogHelper.LoadDimensionLookupAsync(_db, cancellationToken);

        // טוענים רמות סיווג — ויוצרים חסרים לפי הצורך
        var levels = await _db.ClassificationLevels.ToListAsync(cancellationToken);
        var levelIdByDimensionAndCode = levels.ToDictionary(
            level => (level.ClassificationDimensionId, level.LevelCode),
            level => level.ClassificationLevelId);

        // לפני שמירת משתתפים — מוודאים שכל מימד ורמה קיימים במאגר
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

        // משתתפים שכבר במערכת — מעדכנים שם; חדשים — יוצרים
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
            // הוספת משתתף חדש
            _db.Participants.Add(created);
            dbParticipants.Add(created);
        }

        await _db.SaveChangesAsync(cancellationToken);

        // יוצרים את רשומת החלוקה עצמה
        var assignment = new Assignment
        {
            AssignmentName = settings.AssignmentName,
            ManagementGroupId = managementGroup.ManagementGroupId,
        };
        // הוספת החלוקה
        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);

        // מקשרים כל משתתף לחלוקה
        var participantAssignments = dbParticipants
            .Select(participant => new ParticipantAssignment
            {
                ParticipantId = participant.ParticipantId,
                AssignmentId = assignment.AssignmentId,
                ManagerId = manager.ManagerId,
            })
            .ToList();

        // הוספת שיוכי המשתתפים לחלוקה
        _db.ParticipantAssignments.AddRange(participantAssignments);
        await _db.SaveChangesAsync(cancellationToken);

        // מילון ת.ז. → מזהה שיוך (צריך לזוגות ולהעדפות)
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

        // שומרים סיווג לכל משתתף בחלוקה
        foreach (var parsedParticipant in participants)
        {
            var participantAssignmentId = assignmentByIdentity[parsedParticipant.IdentityNumber];
            foreach (var (dimensionCode, levelCode) in parsedParticipant.Classifications)
            {
                var dimension = dimensionByCode[dimensionCode];
                var levelId = levelIdByDimensionAndCode[(dimension.ClassificationDimensionId, levelCode)];

                // הוספת סיווג משתתף לחלוקה
                _db.ParticipantClassifications.Add(new ParticipantClassification
                {
                    ParticipantAssignmentId = participantAssignmentId,
                    ClassificationDimensionId = dimension.ClassificationDimensionId,
                    ClassificationLevelId = levelId,
                });
            }
        }

        // העדפות חברתיות — לפי סדר ברשימה
        await ReplaceSocialPreferencesAsync(
            assignment.AssignmentId,
            participants,
            assignmentByIdentity,
            cancellationToken);

        // הוספת אילוץ מספר קבוצות כולל
        _db.GroupCountConstraints.Add(new GroupCountConstraint
        {
            AssignmentId = assignment.AssignmentId,
            MinGroups = settings.MinGroups,
            MaxGroups = settings.MaxGroups,
        });

        // הוספת אילוץ גודל לכל קבוצה בנפרד
        for (var groupId = 1; groupId <= settings.MaxGroups; groupId++)
        {
            // הוספת אילוץ גודל קבוצה
            _db.GroupSizeConstraints.Add(new GroupSizeConstraint
            {
                AssignmentId = assignment.AssignmentId,
                GroupId = groupId,
                MinGroupSize = settings.MinGroupSize,
                MaxGroupSize = settings.MaxGroupSize,
            });
        }

        // זוגות חובה ואיסור
        foreach (var pair in pairs)
        {
            var firstParticipantAssignmentId = assignmentByIdentity[pair.ParticipantA];
            var secondParticipantAssignmentId = assignmentByIdentity[pair.ParticipantB];

            if (pair.IsMandatory)
            {
                //הוספת זוג חובה
                _db.MandatoryPairConstraints.Add(new MandatoryPairConstraint
                {
                    AssignmentId = assignment.AssignmentId,
                    FirstParticipantAssignmentId = firstParticipantAssignmentId,
                    SecondParticipantAssignmentId = secondParticipantAssignmentId,
                });
            }
            else
            {
                // הוספת זוג אסור
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

    // מחליף את כל ההעדפות החברתיות של החלוקה (בייבוא חדש אין ישנות, אבל אותה לוגיקה)
    private async Task ReplaceSocialPreferencesAsync(
        int assignmentId,
        IReadOnlyList<ParsedParticipantRecord> participants,
        IReadOnlyDictionary<string, int> assignmentByIdentity,
        CancellationToken cancellationToken)
    {
        var existingPreferences = await _db.SocialPreferences
            .Where(preference => preference.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        if (existingPreferences.Count > 0)
        {
            // מחיקת כל ההעדפות החברתיות הקיימות של החלוקה לפני שמוסיפים חדשות
            _db.SocialPreferences.RemoveRange(existingPreferences);
        }

        foreach (var participant in participants)
        {
            if (!assignmentByIdentity.TryGetValue(participant.IdentityNumber, out var fromParticipantAssignmentId))
            {
                continue;
            }

            var seenPreferredParticipants = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < participant.Preferences.Count; index++)
            {
                var rawPreferredIdentity = participant.Preferences[index];
                if (!TryNormalizeIdentity(rawPreferredIdentity, out var preferredIdentity, out _))
                {
                    continue;
                }

                // לא מעדיפים את עצמנו
                if (string.Equals(preferredIdentity, participant.IdentityNumber, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!seenPreferredParticipants.Add(preferredIdentity))
                {
                    continue;
                }

                // המועדף חייב להיות בחלוקה
                if (!assignmentByIdentity.TryGetValue(preferredIdentity, out var toParticipantAssignmentId))
                {
                    continue;
                }

                // הוספת העדפה חברתית — עם משקל לפי סדר הרשימה (1,2,3...)
                _db.SocialPreferences.Add(new SocialPreference
                {
                    AssignmentId = assignmentId,
                    FromParticipantAssignmentId = fromParticipantAssignmentId,
                    ToParticipantAssignmentId = toParticipantAssignmentId,
                    PreferenceWeight = index + 1,
                });
            }
        }
    }

    // רשומת ביניים — משתתף אחרי פרסור
    private sealed record ParsedParticipantRecord(
        string IdentityNumber,
        string Name,
        IReadOnlyDictionary<string, string> Classifications,
        IReadOnlyList<string> Preferences);

    // הגדרות החלוקה לשמירה
    private sealed record ParsedSettingsRecord(
        string AssignmentName,
        int MinGroups,
        int MaxGroups,
        int MinGroupSize,
        int MaxGroupSize);

    // זוג חובה או איסור
    private sealed record ParsedPairRecord(bool IsMandatory, (string A, string B) Participants)
    {
        public string ParticipantA => Participants.A;

        public string ParticipantB => Participants.B;
    }
}