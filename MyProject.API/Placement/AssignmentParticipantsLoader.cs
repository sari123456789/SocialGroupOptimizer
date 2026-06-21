using Microsoft.EntityFrameworkCore;
using MyProject.Data;

namespace MyProject.API.Placement;

/// <summary>
/// טוען את משתתפי החלוקה ואת המידע הנלווה אליהם לצורך הצגה ועריכה במסך.
/// </summary>
/// <remarks>
/// מחלקה זו אינה בונה קלט לאלגוריתם, אלא DTO שמותאם לממשק המשתמש.
/// היא קוראת מהמסד את ParticipantAssignments של חלוקה מסוימת, מחברת אותם
/// לישויות Participant, טוענת סיווגים והעדפות חברתיות, ומחזירה רשימה נוחה
/// לתצוגה. בנוסף היא מחשבת קבוצות סיווג לצורך סיכום במסך, למשל כמה
/// משתתפים קיימים בכל רמה של מימד סיווג.
///
/// הקלט המרכזי הוא assignmentId ו-managerId. לפני טעינת הנתונים מתבצעת
/// בדיקת בעלות דרך AssignmentPlacementLoader, כדי לוודא שהחלוקה אכן שייכת
/// למנהל המחובר. הפלט הוא AssignmentParticipantsDto או null אם אין הרשאה
/// או שהחלוקה אינה קיימת.
/// </remarks>
//  טוענת משתתפים ל-DTO;
public sealed class AssignmentParticipantsLoader
{
    private readonly ApplicationDbContext _db;
    //  טוען עזר לבדיקת בעלות חלוקה
    private readonly AssignmentPlacementLoader _assignmentLoader;

    public AssignmentParticipantsLoader(
        ApplicationDbContext db,
        AssignmentPlacementLoader assignmentLoader)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _assignmentLoader = assignmentLoader ?? throw new ArgumentNullException(nameof(assignmentLoader));
    }

    /// <summary>
    /// מחזיר את כל המשתתפים של חלוקה, כולל סיווגים והעדפות חברתיות מדורגות.
    /// </summary>
    /// <remarks>
    /// זרימת הפעולה:
    /// 1. בדיקת הרשאה לפי managerId.
    /// 2. טעינת רשומת Assignment בסיסית.
    /// 3. טעינת שיוכי משתתפים לחלוקה.
    /// 4. טעינת משתתפים, סיווגים, מימדים, רמות והעדפות חברתיות.
    /// 5. בניית lookup-ים פנימיים כדי להמיר מזהי DB למזהי תעודת זהות ושמות.
    /// 6. הרכבת ParticipantListItemDto לכל משתתף.
    /// 7. חישוב סיכום סיווגים והחזרת AssignmentParticipantsDto.
    /// </remarks>
    // מתודה ציבורית אסינכרונית — מחזירה DTO משתתפים או null
    public async Task<AssignmentParticipantsDto?> GetParticipantsAsync(
        int assignmentId,
        int managerId,
        CancellationToken cancellationToken = default)
    {
        // קריאה אסינכרונית — האם החלוקה שייכת למנהל
        var belongsToManager = await _assignmentLoader.AssignmentBelongsToManagerAsync(
            assignmentId,
            managerId,
            cancellationToken);

        if (!belongsToManager)
        {
            return null;
        }

        // שאילתת EF — טעינת רשומת החלוקה לפי מזהה
        var assignment = await _db.Assignments
            // AsNoTracking — קריאה בלבד, ללא מעקב שינויים
            .AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.AssignmentId == assignmentId, cancellationToken);

        if (assignment is null)
        {
            return null;
        }

        var participantAssignments = await _db.ParticipantAssignments
            .AsNoTracking()
            .Where(entry => entry.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        if (participantAssignments.Count == 0)
        {
            // החזרת DTO מינימלי — רק כותרת החלוקה
            return new AssignmentParticipantsDto
            {
                AssignmentId = assignment.AssignmentId,
                AssignmentName = assignment.AssignmentName,
            };
        }

        // LINQ — איסוף מזהי שיוך משתתף (מפתח DB לשורת שיוך)
        var participantAssignmentIds = participantAssignments
            // בחירת ParticipantAssignmentId מכל שורה
            .Select(entry => entry.ParticipantAssignmentId)
            // המרה לרשימה
            .ToList();

        // LINQ — מזהי משתתף ייחודיים ב-DB (לא ת.ז.)
        var dbParticipantIds = participantAssignments
            // בחירת ParticipantId מכל שיוך
            .Select(entry => entry.ParticipantId)
            // הסרת כפילויות
            .Distinct()
            // רשימה לשימוש ב-Contains בשאילתה
            .ToList();

        // שאילתה — טעינת ישויות Participant לפי מזהי DB
        var participants = await _db.Participants
            // קריאה בלבד
            .AsNoTracking()
            // סינון למשתתפים הרלוונטיים בלבד
            .Where(entry => dbParticipantIds.Contains(entry.ParticipantId))
            // מילון: מפתח ParticipantId → ישות Participant
            .ToDictionaryAsync(entry => entry.ParticipantId, cancellationToken);

        // שאילתה — שורות סיווג לכל שיוכי המשתתפים
        var classificationRows = await _db.ParticipantClassifications
            // ללא מעקב
            .AsNoTracking()
            // סינון לפי רשימת מזהי שיוך
            .Where(entry => participantAssignmentIds.Contains(entry.ParticipantAssignmentId))
            // רשימה בזיכרון
            .ToListAsync(cancellationToken);

        // שאילתה — כל מימדי הסיווג (טבלת עזר קטנה)
        var dimensions = await _db.ClassificationDimensions
            // קריאה בלבד
            .AsNoTracking()
            // מילון: מזהה מימד → ישות מימד
            .ToDictionaryAsync(entry => entry.ClassificationDimensionId, cancellationToken);

        // שאילתה — כל רמות הסיווג
        var levels = await _db.ClassificationLevels
            // ללא מעקב
            .AsNoTracking()
            // מילון: מזהה רמה → ישות רמה
            .ToDictionaryAsync(entry => entry.ClassificationLevelId, cancellationToken);

        // LINQ — קיבוץ שורות סיווג לפי מזהה שיוך משתתף
        var classificationsByParticipantAssignmentId = classificationRows
            // GroupBy לפי ParticipantAssignmentId
            .GroupBy(entry => entry.ParticipantAssignmentId)
            // מילון: מזהה שיוך → רשימת שורות סיווג
            .ToDictionary(
                // מפתח — מזהה הקבוצה
                group => group.Key,
                // ערך — כל השורות בקבוצה
                group => group.ToList());

        // שאילתה — העדפות חברתיות של החלוקה
        var socialPreferences = await _db.SocialPreferences
            // קריאה בלבד
            .AsNoTracking()
            // סינון לפי מזהה החלוקה
            .Where(entry => entry.AssignmentId == assignmentId)
            // רשימה בזיכרון
            .ToListAsync(cancellationToken);

        // LINQ — מיפוי מזהה שיוך → מספר תעודת זהות (ParticipantId בליבה)
        var identityByParticipantAssignmentId = participantAssignments
            // רק שיוכים שיש להם משתתף במילון
            .Where(entry => participants.ContainsKey(entry.ParticipantId))
            // בניית מילון lookup
            .ToDictionary(
                // מפתח — מזהה שיוך
                entry => entry.ParticipantAssignmentId,
                // ערך — ת.ז. מהישות Participant
                entry => participants[entry.ParticipantId].IsraeliIdentityNumber);

        // LINQ — מיפוי מזהה שיוך → שם תצוגה
        var displayNameByParticipantAssignmentId = participantAssignments
            // סינון לשיוכים עם משתתף קיים
            .Where(entry => participants.ContainsKey(entry.ParticipantId))
            // מילון lookup לשמות
            .ToDictionary(
                // מפתח — מזהה שיוך
                entry => entry.ParticipantAssignmentId,
                // ערך — שם המשתתף
                entry => participants[entry.ParticipantId].ParticipantName);

        // LINQ — קיבוץ העדפות לפי משתתף מקור (From)
        var preferencesByFromParticipantAssignmentId = socialPreferences
            // GroupBy לפי מזהה שיוך המעדיף
            .GroupBy(entry => entry.FromParticipantAssignmentId)
            // מילון: מזהה מקור → רשימה ממוינת לפי משקל
            .ToDictionary(
                // מפתח — מזהה שיוך מקור
                group => group.Key,
                // ערך — העדפות ממוינות לפי PreferenceWeight
                group => group.OrderBy(entry => entry.PreferenceWeight).ToList());

        // יצירת רשימה ריקה לאיסוף פריטי משתתף ל-DTO
        var participantItems = new List<ParticipantListItemDto>();

        // לולאה — עיבוד כל שיוך משתתף לחלוקה
        foreach (var participantAssignment in participantAssignments)
        {
            // TryGetValue — חיפוש משתתף במילון; דילוג אם חסר
            if (!participants.TryGetValue(participantAssignment.ParticipantId, out var participant))
            {
                // continue — דילוג על שיוך יתום
                continue;
            }

            // מילון חדש לסיווגים — מפתח קוד מימד, ערך קוד רמה
            var classifications = new Dictionary<string, string>(StringComparer.Ordinal);

            // ניסיון לשלוף שורות סיווג לשיוך הנוכחי
            if (classificationsByParticipantAssignmentId.TryGetValue(
                    // מזהה השיוך הנוכחי
                    participantAssignment.ParticipantAssignmentId,
                    // out — רשימת שורות סיווג אם קיימת
                    out var rows))
            {
                // לולאה פנימית — כל שורת סיווג
                foreach (var row in rows)
                {
                    // חיפוש מימד לפי מזהה; דילוג אם חסר
                    if (!dimensions.TryGetValue(row.ClassificationDimensionId, out var dimension))
                    {
                        continue;
                    }

                    // חיפוש רמה לפי מזהה; דילוג אם חסר
                    if (!levels.TryGetValue(row.ClassificationLevelId, out var level))
                    {
                        // continue — רמה לא נמצאה
                        continue;
                    }

                    // הוספה למילון — קוד מימד → קוד רמה
                    classifications[dimension.DimensionCode] = level.LevelCode;
                }
            }

            // רשימה ריקה להעדפות מדורגות של המשתתף
            var preferences = new List<ParticipantPreferenceItemDto>();
            // ניסיון לשלוף העדפות יוצאות מהמשתתף הנוכחי
            if (preferencesByFromParticipantAssignmentId.TryGetValue(
                    // מזהה שיוך כמקור העדפות
                    participantAssignment.ParticipantAssignmentId,
                    // out — שורות העדפה ממוינות
                    out var preferenceRows))
            {
                // דירוג התחלתי — 1 לראשונה ברשימה
                var rank = 1;
                // לולאה — כל שורת העדפה (יעד)
                foreach (var preferenceRow in preferenceRows)
                {
                    // חיפוש ת.ז. של משתתף היעד; דילוג אם חסר
                    if (!identityByParticipantAssignmentId.TryGetValue(
                            // מזהה שיוך היעד
                            preferenceRow.ToParticipantAssignmentId,
                            // out — מספר תעודת זהות
                            out var preferredParticipantId))
                    {
                        continue;
                    }

                    // ניסיון לשלוף שם תצוגה של היעד 
                    displayNameByParticipantAssignmentId.TryGetValue(
                        // מזהה שיוך היעד
                        preferenceRow.ToParticipantAssignmentId,
                        out var preferredDisplayName);

                    // הוספת פריט העדפה לרשימה
                    preferences.Add(new ParticipantPreferenceItemDto
                    {
                        Rank = rank++,
                        ParticipantId = preferredParticipantId,
                        DisplayName = preferredDisplayName,
                    });
                }
            }

            // הוספת פריט משתתף מלא לרשימת התוצאה
            participantItems.Add(new ParticipantListItemDto
            {
                // מזהה ליבה — תעודת זהות ישראלית
                ParticipantId = participant.IsraeliIdentityNumber,
                // שם להצגה בממשק
                DisplayName = participant.ParticipantName,
                // מילון סיווגים שנבנה
                Classifications = classifications,
                // רשימת העדפות מדורגות
                Preferences = preferences,
            });
        }

        // מיון סופי — לפי שם תצוגה, או ת.ז. אם שם חסר
        participantItems = participantItems
            // OrderBy עם StringComparer.Ordinal ליציבות
            .OrderBy(entry => entry.DisplayName ?? entry.ParticipantId, StringComparer.Ordinal)
            // החלפת הרשימה בממוינת
            .ToList();

        // חישוב קבוצות סיווג לסיכום במסך
        var classificationGroups = participantItems
            // פיצול כל משתתף לזוגות מימד-רמה
            .SelectMany(entry => entry.Classifications.Select(classification => new
            {
                // מפתח — קוד מימד
                classification.Key,
                // ערך — קוד רמה
                classification.Value,
            }))
            // קיבוץ לפי שילוב מימד+רמה
            .GroupBy(entry => new { entry.Key, entry.Value })
            // המרה ל-DTO של קבוצת סיווג
            .Select(group => new ClassificationGroupDto
            {
                // קוד המימד מהמפתח
                DimensionCode = group.Key.Key,
                // קוד הרמה מהמפתח
                LevelCode = group.Key.Value,
                // מספר המשתתפים בקבוצה זו
                ParticipantCount = group.Count(),
            })
            // מיון ראשוני לפי קוד מימד
            .OrderBy(entry => entry.DimensionCode, StringComparer.Ordinal)
            // מיון משני לפי קוד רמה
            .ThenBy(entry => entry.LevelCode, StringComparer.Ordinal)
            .ToList();

        // החזרת DTO מלא עם כל הנתונים שנאספו
        return new AssignmentParticipantsDto
        {
            AssignmentId = assignment.AssignmentId,
            AssignmentName = assignment.AssignmentName,
            ClassificationGroups = classificationGroups,
            Participants = participantItems,
        };
    }
}