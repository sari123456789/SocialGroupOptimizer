using Microsoft.EntityFrameworkCore;
using MyProject.BL.Algorithm.InitialPlacement;
using MyProject.BL.Logic.Configuration;
using MyProject.Core.Domain.Constraints;
using MyProject.Data;
using MyProject.Data.Mapping;
using InitialPlacementExecutionContext = MyProject.BL.Algorithm.InitialPlacement.Runtime.ExecutionContext;

namespace MyProject.API.Placement;

/// <summary>
/// טוען נתוני חלוקה מהמסד וממירם ל-<see cref="InitialPlacementInput"/> לשימוש האלגוריתם.
/// </summary>
/// <remarks>
/// המחלקה מרכזת את אחת ההמרות החשובות במערכת: מעבר מנתונים שמורים בטבלאות
/// למסמך קלט אלגוריתמי מלא. היא שומרת על הפרדה בין Entity Framework לבין
/// שכבת האלגוריתם. האלגוריתם אינו צריך לדעת על ParticipantAssignmentId,
/// ManagementGroupId או טבלאות סיווג; הוא מקבל Participants ו-Constraints
/// במבני הדומיין שלו.
///
/// הקלטים של המחלקה הם assignmentId ו-managerId, ובנוסף הנתונים שנשלפים
/// מהמסד. הפלט המרכזי הוא InitialPlacementInput. אם החלוקה אינה שייכת
/// למנהל או חסרים נתונים בסיסיים, המחלקה זורקת שגיאה מתאימה כדי שהבקר
/// יוכל להחזיר תשובת API ברורה.
/// </remarks>
// שירות שטוען חלוקה מהמסד וממיר לקלט אלגוריתם
public sealed class AssignmentPlacementLoader
{
    private readonly ApplicationDbContext _db;
    // הגדרות אלגוריתם כמו חישוב סטייה מקסימלית
    private readonly AlgorithmSettings _algorithmSettings;

    // בונה את השירות עם תלויות מוזרקות
    public AssignmentPlacementLoader(ApplicationDbContext db, AlgorithmSettings algorithmSettings)
    {

         _db = db ?? throw new ArgumentNullException(nameof(db));
        // בודקים שלא קיבלנו הגדרות ריקות
        _algorithmSettings = algorithmSettings ?? throw new ArgumentNullException(nameof(algorithmSettings));
    }

    /// <summary>
    /// רשימת חלוקות של מנהל — רק חלוקות בקבוצות ניהול ששייכות לו.
    /// </summary>
    // מחזיר רשימת סיכומי חלוקות של המנהל
    public async Task<IReadOnlyList<AssignmentSummaryDto>> ListAssignmentsAsync(
        int managerId,
        CancellationToken cancellationToken = default)
    {
        // שולפים רק חלוקות ששייכות לקבוצות ניהול של המנהל ומציגים סיכום לכל חלוקה
        return await _db.Assignments
            // מסננים לפי שיוך קבוצת הניהול למנהל
            .Where(assignment => _db.ManagementGroups
                .Any(group =>
                    group.ManagementGroupId == assignment.ManagementGroupId
                    && group.ManagerId == managerId))
            // מציגים חלוקות חדשות קודם
            .OrderByDescending(assignment => assignment.AssignmentId)
            // מיפוי ל-AssignmentSummaryDto 
            .Select(assignment => new AssignmentSummaryDto
            {
                // מזהה החלוקה
                AssignmentId = assignment.AssignmentId,
                // שם החלוקה לתצוגה
                AssignmentName = assignment.AssignmentName,
                // כמה משתתפים יש בחלוקה
                ParticipantCount = _db.ParticipantAssignments.Count(
                    participantAssignment => participantAssignment.AssignmentId == assignment.AssignmentId),
            })
            // מריצים את השאילתה באופן אסינכרוני
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// בודק האם חלוקה שייכת למנהל — בסיס לכל פעולת הרשאה.
    /// </summary>
    // בודקים אם החלוקה שייכת למנהל
    public async Task<bool> AssignmentBelongsToManagerAsync(
        int assignmentId,
        int managerId,
        CancellationToken cancellationToken = default)
    {
        // מחפשים חלוקה עם המזהה הנתון בקבוצת ניהול של המנהל
        return await _db.Assignments
            .AnyAsync(
                assignment => assignment.AssignmentId == assignmentId
                    && _db.ManagementGroups.Any(group =>
                        group.ManagementGroupId == assignment.ManagementGroupId
                        && group.ManagerId == managerId),
                cancellationToken);
    }

    /// <summary>
    /// בונה קלט אלגוריתם מלא מחלוקה במסד.
    /// </summary>
    /// <remarks>
    /// המתודה טוענת את כל רכיבי החלוקה הדרושים להרצה: משתתפים, שיוכי משתתפים
    /// לחלוקה, סיווגים, העדפות חברתיות, זוגות חובה, זוגות אסורים, אילוצי
    /// גודל ומספר קבוצות ואילוצי סיווג. לאחר הטעינה היא בונה אובייקטי עזר
    /// כמו ClassificationCatalog ו-ParticipantAssignmentContext, ואז מפעילה
    /// את מחלקות המיפוי. התוצאה היא InitialPlacementInput נקי, שאינו תלוי
    /// במבנה הטבלאות.
    /// </remarks>
    // טוענים את כל הנתונים ובונים קלט אלגוריתם
    //קלט אלגוריתם מורכב מנתוני ריצה משתתפים ואילוצים
    public async Task<InitialPlacementInput> LoadInputAsync(
        int assignmentId,
        int managerId,
        CancellationToken cancellationToken = default)
    {
        // קודם בודקים שהחלוקה שייכת למנהל
        var belongsToManager = await AssignmentBelongsToManagerAsync(
            assignmentId,
            managerId,
            cancellationToken);

        // אם החלוקה לא שייכת למנהל — זורקים שגיאה
        if (!belongsToManager)
        {
            // הבקר יתפוס ויחזיר 400 או 404
            throw new ArgumentException($"Assignment {assignmentId} was not found.");
        }

        //  שלב 1: טעינת משתתפים וקשרים 
        // שולפים את כל שיוכי המשתתפים לחלוקה
        var participantAssignments = await _db.ParticipantAssignments
            .Where(participantAssignment => participantAssignment.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        // אם אין משתתפים בחלוקה — לא ניתן להמשיך
        if (participantAssignments.Count == 0)
        {
            throw new ArgumentException($"Assignment {assignmentId} has no participants.");
        }

        // אוספים את מזהי המשתתפים הייחודיים
        var dbParticipantIds = participantAssignments
            .Select(participantAssignment => participantAssignment.ParticipantId)
            .Distinct()
            .ToList();

        // אוספים את מזהי השיוך משתתף-לחלוקה
        var participantAssignmentIds = participantAssignments
            .Select(participantAssignment => participantAssignment.ParticipantAssignmentId)
            .ToList();

        // טוענים את רשומות המשתתפים מהמסד
        var participants = await _db.Participants
            .Where(participant => dbParticipantIds.Contains(participant.ParticipantId))
            .ToListAsync(cancellationToken);


        //  שלב 2: טעינת סיווגים, העדפות, אילוצים 
        // טוענים סיווגי משתתפים לפי שיוכי החלוקה
        var participantClassifications = await _db.ParticipantClassifications
            .Where(classification => participantAssignmentIds.Contains(classification.ParticipantAssignmentId))
            .ToListAsync(cancellationToken);

        // טוענים העדפות חברתיות — רק לאלגוריתם
        var socialPreferences = await _db.SocialPreferences
            .Where(preference => preference.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        // טוענים זוגות שחייבים להיות באותה קבוצה
        var mandatoryPairs = await _db.MandatoryPairConstraints
            .Where(pair => pair.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        // טוענים זוגות שאסור שיהיו באותה קבוצה
        var forbiddenPairs = await _db.ForbiddenPairConstraints
            .Where(pair => pair.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        // טוענים אילוצי גודל קבוצה
        var groupSizeConstraints = await _db.GroupSizeConstraints
            .Where(constraint => constraint.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        // טוענים אילוצי מספר קבוצות
        var groupCountConstraints = await _db.GroupCountConstraints
            .Where(constraint => constraint.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        // טוענים אילוצי סיווג
        var classificationConstraintRows = await _db.AssignmentClassificationConstraints
            .Where(constraint => constraint.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        //  שלב 3: קטלוג סיווגים (מימדים + רמות) 
        // טוענים את כל מימדי הסיווג
        var dimensions = await _db.ClassificationDimensions.ToListAsync(cancellationToken);
        // טוענים את כל רמות הסיווג
        var levels = await _db.ClassificationLevels.ToListAsync(cancellationToken);
        // בונים קטלוג לחיפוש מימד ורמה לפי מזהה
        var catalog = new ClassificationCatalog(dimensions, levels);
        // בונים הקשר שיוך משתתף לחלוקה
        var context = new ParticipantAssignmentContext(participantAssignments);
        // בונים מילון תעודת זהות למזהה במסד
        var identityLookup = ParticipantMapper.CreateParticipantIdentityLookup(participants);

        //  שלב 4: מיפוי DB → Core.Participant 
        // ממירים כל משתתף לאובייקט דומיין
        var coreParticipants = participants
            .Select(participant => ParticipantMapper.MapToCoreParticipant(
                participant,
                participantClassifications,
                catalog,
                socialPreferences,
                context,
                identityLookup,
                assignmentId))
            .ToList();

        //  שלב 5 
        // אוסף אילוצים לאלגוריתם
        var constraints = new List<IConstraint>();

        // ממירים אילוץ מספר קבוצות — יכול להיות null
        var groupCount = ConstraintMapper.MapGroupCountConstraint(groupCountConstraints, assignmentId);
        // מוסיפים רק אם קיים אילוץ מספר קבוצות
        if (groupCount is not null)
        {
            constraints.Add(groupCount);
        }

        // מוסיפים אילוצי גודל קבוצה
        constraints.AddRange(ConstraintMapper.MapGroupSizeConstraints(groupSizeConstraints, assignmentId));
        // מוסיפים זוגות חובה
        constraints.AddRange(ConstraintMapper.MapMandatoryPairs(mandatoryPairs, context, identityLookup, assignmentId));
        // מוסיפים זוגות אסורים
        constraints.AddRange(ConstraintMapper.MapForbiddenPairs(forbiddenPairs, context, identityLookup, assignmentId));
        // מחשבים סטייה מקסימלית לפי מספר משתתפים
        var maxScaledDeviation = _algorithmSettings.ComputeMaxScaledDeviation(coreParticipants.Count);
        // מוסיפים אילוצי סיווג עם סטייה מותרת
        constraints.AddRange(ConstraintMapper.MapClassificationConstraints(
            classificationConstraintRows,
            catalog,
            participantClassifications,
            context,
            identityLookup,
            assignmentId,
            coreParticipants.Count,
            maxScaledDeviation));

        // חייב להיות לפחות אילוץ אחד
        if (constraints.Count == 0)
        {
            throw new ArgumentException($"Assignment {assignmentId} has no placement constraints.");
        }

        // בונים הקשר ריצה עם מזהה ו-seed
        var executionContext = new InitialPlacementExecutionContext(
            MyProject.Core.Domain.ValueObjects.AlgorithmRunId.New(),
            Environment.TickCount);

        // מחזירים קלט אלגוריתם מלא
        return new InitialPlacementInput(executionContext, coreParticipants, constraints);
    }
}