using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyProject.Data;
using MyProject.Data.Import;
using MyProject.Data.Models;

namespace MyProject.API.Placement;

/// <summary>
/// שירות עריכת חלוקה — עדכון הגדרות, CRUD משתתפים ואילוצים, עם אימות מחדש אחרי כל שינוי.
/// </summary>
/// <remarks>
/// נקרא מ-: <see cref="AssignmentsController"/> (כל endpoint של PUT/POST/DELETE על חלוקה, משתתפים ואילוצים).
/// </remarks>
public sealed class AssignmentEditorService
{
    private readonly ApplicationDbContext _db;
    private readonly AssignmentPlacementLoader _assignmentLoader;
    private readonly AssignmentDetailLoader _detailLoader;
    private readonly AssignmentInitialPlacementValidator _validator;

    /// <summary>
    /// מאתחל עם DbContext, טועני עזר ומאמת חלוקה.
    /// </summary>
    /// <param name="db">DbContext לכתיבה וקריאה מ-EF.</param>
    /// <param name="assignmentLoader">בדיקת שיוך חלוקה למנהל.</param>
    /// <param name="detailLoader">טעינת DTO מלא אחרי עריכה מוצלחת.</param>
    /// <param name="validator">אימות מחדש של החלוקה לאחר שינוי.</param>
    public AssignmentEditorService(
        ApplicationDbContext db,
        AssignmentPlacementLoader assignmentLoader,
        AssignmentDetailLoader detailLoader,
        AssignmentInitialPlacementValidator validator)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _assignmentLoader = assignmentLoader ?? throw new ArgumentNullException(nameof(assignmentLoader));
        _detailLoader = detailLoader ?? throw new ArgumentNullException(nameof(detailLoader));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <summary>
    /// מעדכן שם חלוקה ו/או הגדרות קבוצות (מספר + גודל).
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="managerId">מזהה המנהל — בדיקת הרשאה.</param>
    /// <param name="request">שם חדש (אופציונלי) והגדרות קבוצות (אופציונלי).</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <returns>AssignmentEditResult עם Detail מעודכן, או רשימת שגיאות.</returns>
    /// <remarks>
    /// נקרא מ-: <see cref="AssignmentsController.Update"/>.
    /// </remarks>
    public async Task<AssignmentEditResult> UpdateAssignmentAsync(
        int assignmentId,
        int managerId,
        UpdateAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        // ===== שלב 1: אימות הגדרות =====
        // ValidateSettings — בדיקות סטטיות על Min/Max קבוצות וגודל; ללא גישה ל-DB.
        var errors = ValidateSettings(request.Settings);
        if (errors.Count > 0)
        {
            return AssignmentEditResult.Fail(errors);
        }

        // ===== שלב 2: הרשאה =====
        // AssignmentBelongsToManagerAsync — JOIN Assignment↔Manager; false = חלוקה לא נמצאה.
        if (!await _assignmentLoader.AssignmentBelongsToManagerAsync(assignmentId, managerId, cancellationToken))
        {
            return AssignmentEditResult.Fail("החלוקה לא נמצאה.");
        }

        // ===== שלב 3: טעינת רשומת החלוקה =====
        var assignment = await _db.Assignments
            .FirstOrDefaultAsync(entry => entry.AssignmentId == assignmentId, cancellationToken);

        if (assignment is null)
        {
            return AssignmentEditResult.Fail("החלוקה לא נמצאה.");
        }

        // ===== שלב 4: עדכון שם (אופציונלי) =====
        if (!string.IsNullOrWhiteSpace(request.AssignmentName))
        {
            assignment.AssignmentName = request.AssignmentName.Trim();
        }

        // ===== שלב 5: עדכון הגדרות קבוצות (אופציונלי) =====
        if (request.Settings is not null)
        {
            // ספירת משתתפים — נדרשת לבדיקת קיבולת לפני שינוי Min/Max.
            var participantCount = await _db.ParticipantAssignments
                .CountAsync(entry => entry.AssignmentId == assignmentId, cancellationToken);

            var capacityError = ValidateCapacity(request.Settings, participantCount);
            if (capacityError is not null)
            {
                return AssignmentEditResult.Fail(capacityError);
            }

            await UpdateGroupCountAsync(assignmentId, request.Settings, cancellationToken);
            await UpdateGroupSizesAsync(assignmentId, request.Settings, cancellationToken);
        }

        // ===== שלב 6: שמירה + החזרת פירוט =====
        await _db.SaveChangesAsync(cancellationToken);

        var detail = await FinishWithValidationAsync(assignmentId, managerId, cancellationToken);
        return detail is null
            ? AssignmentEditResult.Fail("החלוקה לא נמצאה.")
            : AssignmentEditResult.Ok(detail);
    }

    /// <summary>
    /// מוסיף משתתף לחלוקה — יוצר/מעדכן רשומת Participant ומחיל סיווגים.
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="managerId">מזהה המנהל — בדיקת הרשאה.</param>
    /// <param name="request">ת.ז., שם תצוגה ומילון סיווגים (מימד→רמה).</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <returns>AssignmentEditResult עם Detail מעודכן, או הודעת שגיאה.</returns>
    /// <remarks>
    /// נקרא מ-: <see cref="AssignmentsController.AddParticipant"/>.
    /// </remarks>
    public async Task<AssignmentEditResult> AddParticipantAsync(
        int assignmentId,
        int managerId,
        AddParticipantRequest request,
        CancellationToken cancellationToken = default)
    {
        // ===== שלב 1: נרמול ואימות ת.ז. =====
        if (!TryNormalizeIdentity(request.ParticipantId, out var identity, out var identityError))
        {
            return AssignmentEditResult.Fail($"תעודת זהות {identityError}.");
        }

        if (request.Classifications.Count == 0)
        {
            return AssignmentEditResult.Fail("יש להגדיר לפחות סיווג אחד.");
        }

        // ===== שלב 2: הרשאה =====
        if (!await _assignmentLoader.AssignmentBelongsToManagerAsync(assignmentId, managerId, cancellationToken))
        {
            return AssignmentEditResult.Fail("החלוקה לא נמצאה.");
        }

        // ===== שלב 3: בדיקת כפילות בחלוקה =====
        // JOIN דרך subquery — מוודא שאין כבר משתתף עם אותה ת.ז. בחלוקה זו.
        var alreadyInAssignment = await _db.ParticipantAssignments
            .AnyAsync(
                entry => entry.AssignmentId == assignmentId
                    && _db.Participants.Any(participant =>
                        participant.ParticipantId == entry.ParticipantId
                        && participant.IsraeliIdentityNumber == identity),
                cancellationToken);

        if (alreadyInAssignment)
        {
            return AssignmentEditResult.Fail("משתתף עם תעודת זהות זו כבר קיים בחלוקה.");
        }

        // ===== שלב 4: מציאה או יצירת Participant גלובלי =====
        var participant = await _db.Participants
            .FirstOrDefaultAsync(entry => entry.IsraeliIdentityNumber == identity, cancellationToken);

        if (participant is null)
        {
            // משתתף חדש במערכת — IsraeliIdentityNumber = ת.ז. (לא ParticipantId מספרי).
            participant = new Participant
            {
                IsraeliIdentityNumber = identity,
                ParticipantName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim(),
            };
            _db.Participants.Add(participant);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            // משתתף קיים — מעדכנים שם תצוגה אם סופק.
            participant.ParticipantName = request.DisplayName.Trim();
        }

        // ===== שלב 5: שיוך לחלוקה =====
        var participantAssignment = new ParticipantAssignment
        {
            ParticipantId = participant.ParticipantId,
            AssignmentId = assignmentId,
            ManagerId = managerId,
        };

        _db.ParticipantAssignments.Add(participantAssignment);
        await _db.SaveChangesAsync(cancellationToken);

        // ===== שלב 6: החלת סיווגים =====
        var classificationError = await ApplyClassificationsAsync(
            participantAssignment.ParticipantAssignmentId,
            request.Classifications,
            cancellationToken);

        if (classificationError is not null)
        {
            return AssignmentEditResult.Fail(classificationError);
        }

        await _db.SaveChangesAsync(cancellationToken);

        // ===== שלב 7: אימות + החזרת פירוט =====
        var detail = await FinishWithValidationAsync(assignmentId, managerId, cancellationToken);
        return detail is null
            ? AssignmentEditResult.Fail("החלוקה לא נמצאה.")
            : AssignmentEditResult.Ok(detail);
    }

    /// <summary>
    /// מעדכן שם תצוגה ו/או סיווגים של משתתף קיים בחלוקה.
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="managerId">מזהה המנהל — בדיקת הרשאה.</param>
    /// <param name="rawIdentity">ת.ז. מהנתיב (לפני נרמול).</param>
    /// <param name="request">שם חדש (null = ללא שינוי) ו/או סיווגים חדשים.</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <returns>AssignmentEditResult עם Detail מעודכן, או הודעת שגיאה.</returns>
    /// <remarks>
    /// נקרא מ-: <see cref="AssignmentsController.UpdateParticipant"/>.
    /// </remarks>
    public async Task<AssignmentEditResult> UpdateParticipantAsync(
        int assignmentId,
        int managerId,
        string rawIdentity,
        UpdateParticipantRequest request,
        CancellationToken cancellationToken = default)
    {
        // ===== שלב 1: נרמול ת.ז. =====
        if (!TryNormalizeIdentity(rawIdentity, out var identity, out var identityError))
        {
            return AssignmentEditResult.Fail($"תעודת זהות {identityError}.");
        }

        // ===== שלב 2: מציאת שיוך משתתף↔חלוקה =====
        var participantAssignment = await FindParticipantAssignmentAsync(
            assignmentId,
            managerId,
            identity,
            cancellationToken);

        if (participantAssignment is null)
        {
            return AssignmentEditResult.Fail("המשתתף לא נמצא בחלוקה.");
        }

        // ===== שלב 3: עדכון שם תצוגה (אופציונלי) =====
        if (request.DisplayName is not null)
        {
            var participant = await _db.Participants
                .FirstAsync(entry => entry.ParticipantId == participantAssignment.ParticipantId, cancellationToken);

            // מחרוזת ריקה → null ב-DB (אין שם תצוגה).
            participant.ParticipantName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? null
                : request.DisplayName.Trim();
        }

        // ===== שלב 4: החלפת סיווגים (אופציונלי) =====
        if (request.Classifications is not null)
        {
            if (request.Classifications.Count == 0)
            {
                return AssignmentEditResult.Fail("יש להגדיר לפחות סיווג אחד.");
            }

            // מוחקים את כל הסיווגים הקיימים — מחליפים במלואם (לא merge חלקי).
            var existing = await _db.ParticipantClassifications
                .Where(entry => entry.ParticipantAssignmentId == participantAssignment.ParticipantAssignmentId)
                .ToListAsync(cancellationToken);

            _db.ParticipantClassifications.RemoveRange(existing);

            var classificationError = await ApplyClassificationsAsync(
                participantAssignment.ParticipantAssignmentId,
                request.Classifications,
                cancellationToken);

            if (classificationError is not null)
            {
                return AssignmentEditResult.Fail(classificationError);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var detail = await FinishWithValidationAsync(assignmentId, managerId, cancellationToken);
        return detail is null
            ? AssignmentEditResult.Fail("החלוקה לא נמצאה.")
            : AssignmentEditResult.Ok(detail);
    }

    /// <summary>
    /// מוחק משתתף מהחלוקה — כולל ניקוי אילוצי זוגות והעדפות חברתיות קשורים.
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="managerId">מזהה המנהל — בדיקת הרשאה.</param>
    /// <param name="rawIdentity">ת.ז. מהנתיב (לפני נרמול).</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <returns>AssignmentEditResult עם Detail מעודכן, או הודעת שגיאה.</returns>
    /// <remarks>
    /// נקרא מ-: <see cref="AssignmentsController.DeleteParticipant"/>.
    /// </remarks>
    public async Task<AssignmentEditResult> DeleteParticipantAsync(
        int assignmentId,
        int managerId,
        string rawIdentity,
        CancellationToken cancellationToken = default)
    {
        // ===== שלב 1: נרמול ת.ז. =====
        if (!TryNormalizeIdentity(rawIdentity, out var identity, out var identityError))
        {
            return AssignmentEditResult.Fail($"תעודת זהות {identityError}.");
        }

        // ===== שלב 2: מציאת שיוך =====
        var participantAssignment = await FindParticipantAssignmentAsync(
            assignmentId,
            managerId,
            identity,
            cancellationToken);

        if (participantAssignment is null)
        {
            return AssignmentEditResult.Fail("המשתתף לא נמצא בחלוקה.");
        }

        var participantAssignmentId = participantAssignment.ParticipantAssignmentId;

        // ===== שלב 3: ניקוי תלויות =====
        // אילוצי זוג ו-SocialPreferences מצביעים על ParticipantAssignmentId — חייבים למחוק לפני השיוך.
        await RemovePairConstraintsForParticipantAsync(assignmentId, participantAssignmentId, cancellationToken);
        await RemoveSocialPreferencesForParticipantAsync(assignmentId, participantAssignmentId, cancellationToken);

        // ===== שלב 4: מחיקת השיוך =====
        _db.ParticipantAssignments.Remove(participantAssignment);
        await _db.SaveChangesAsync(cancellationToken);

        var detail = await FinishWithValidationAsync(assignmentId, managerId, cancellationToken);
        return detail is null
            ? AssignmentEditResult.Fail("החלוקה לא נמצאה.")
            : AssignmentEditResult.Ok(detail);
    }

    /// <summary>
    /// מוסיף אילוץ זוג חובה — שני משתתפים חייבים להיות באותה קבוצה.
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="managerId">מזהה המנהל — בדיקת הרשאה.</param>
    /// <param name="request">ת.ז. של שני המשתתפים.</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <returns>AssignmentEditResult עם Detail מעודכן, או הודעת שגיאה.</returns>
    /// <remarks>
    /// נקרא מ-: <see cref="AssignmentsController.AddMandatoryPair"/>.
    /// </remarks>
    public async Task<AssignmentEditResult> AddMandatoryPairAsync(
        int assignmentId,
        int managerId,
        AddPairConstraintRequest request,
        CancellationToken cancellationToken = default)
    {
        return await AddPairAsync(
            assignmentId,
            managerId,
            request,
            isMandatory: true,
            cancellationToken);
    }

    /// <summary>
    /// מוסיף אילוץ זוג איסור — שני משתתפים לא יכולים להיות באותה קבוצה.
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="managerId">מזהה המנהל — בדיקת הרשאה.</param>
    /// <param name="request">ת.ז. של שני המשתתפים.</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <returns>AssignmentEditResult עם Detail מעודכן, או הודעת שגיאה.</returns>
    /// <remarks>
    /// נקרא מ-: <see cref="AssignmentsController.AddForbiddenPair"/>.
    /// </remarks>
    public async Task<AssignmentEditResult> AddForbiddenPairAsync(
        int assignmentId,
        int managerId,
        AddPairConstraintRequest request,
        CancellationToken cancellationToken = default)
    {
        return await AddPairAsync(
            assignmentId,
            managerId,
            request,
            isMandatory: false,
            cancellationToken);
    }

    /// <summary>
    /// מוחק אילוץ זוג חובה לפי מזהה האילוץ.
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="managerId">מזהה המנהל — בדיקת הרשאה.</param>
    /// <param name="constraintId">MandatoryPairConstraintId.</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <returns>AssignmentEditResult עם Detail מעודכן, או הודעת שגיאה.</returns>
    /// <remarks>
    /// נקרא מ-: <see cref="AssignmentsController.DeleteMandatoryPair"/>.
    /// </remarks>
    public async Task<AssignmentEditResult> DeleteMandatoryPairAsync(
        int assignmentId,
        int managerId,
        int constraintId,
        CancellationToken cancellationToken = default)
    {
        // ===== שלב 1: הרשאה =====
        if (!await _assignmentLoader.AssignmentBelongsToManagerAsync(assignmentId, managerId, cancellationToken))
        {
            return AssignmentEditResult.Fail("החלוקה לא נמצאה.");
        }

        // ===== שלב 2: מציאת האילוץ =====
        var row = await _db.MandatoryPairConstraints
            .FirstOrDefaultAsync(
                entry => entry.MandatoryPairConstraintId == constraintId
                    && entry.AssignmentId == assignmentId,
                cancellationToken);

        if (row is null)
        {
            return AssignmentEditResult.Fail("האילוץ לא נמצא.");
        }

        // ===== שלב 3: מחיקה + החזרת פירוט =====
        _db.MandatoryPairConstraints.Remove(row);
        await _db.SaveChangesAsync(cancellationToken);

        var detail = await FinishWithValidationAsync(assignmentId, managerId, cancellationToken);
        return detail is null
            ? AssignmentEditResult.Fail("החלוקה לא נמצאה.")
            : AssignmentEditResult.Ok(detail);
    }

    /// <summary>
    /// מוחק אילוץ זוג איסור לפי מזהה האילוץ.
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="managerId">מזהה המנהל — בדיקת הרשאה.</param>
    /// <param name="constraintId">ForbiddenPairConstraintId.</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <returns>AssignmentEditResult עם Detail מעודכן, או הודעת שגיאה.</returns>
    /// <remarks>
    /// נקרא מ-: <see cref="AssignmentsController.DeleteForbiddenPair"/>.
    /// </remarks>
    public async Task<AssignmentEditResult> DeleteForbiddenPairAsync(
        int assignmentId,
        int managerId,
        int constraintId,
        CancellationToken cancellationToken = default)
    {
        // ===== שלב 1: הרשאה =====
        if (!await _assignmentLoader.AssignmentBelongsToManagerAsync(assignmentId, managerId, cancellationToken))
        {
            return AssignmentEditResult.Fail("החלוקה לא נמצאה.");
        }

        // ===== שלב 2: מציאת האילוץ =====
        var row = await _db.ForbiddenPairConstraints
            .FirstOrDefaultAsync(
                entry => entry.ForbiddenPairConstraintId == constraintId
                    && entry.AssignmentId == assignmentId,
                cancellationToken);

        if (row is null)
        {
            return AssignmentEditResult.Fail("האילוץ לא נמצא.");
        }

        // ===== שלב 3: מחיקה + החזרת פירוט =====
        _db.ForbiddenPairConstraints.Remove(row);
        await _db.SaveChangesAsync(cancellationToken);

        var detail = await FinishWithValidationAsync(assignmentId, managerId, cancellationToken);
        return detail is null
            ? AssignmentEditResult.Fail("החלוקה לא נמצאה.")
            : AssignmentEditResult.Ok(detail);
    }

    /// <summary>
    /// מוסיף אילוץ סיווג (איזון או הפרדה) על מימד בחלוקה.
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="managerId">מזהה המנהל — בדיקת הרשאה.</param>
    /// <param name="request">קוד מימד וסוג כלל (Balance/Separation).</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <returns>AssignmentEditResult עם Detail מעודכן, או הודעת שגיאה.</returns>
    /// <remarks>
    /// נקרא מ-: <see cref="AssignmentsController.AddClassificationConstraint"/>.
    /// </remarks>
    public async Task<AssignmentEditResult> AddClassificationConstraintAsync(
        int assignmentId,
        int managerId,
        AddClassificationConstraintRequest request,
        CancellationToken cancellationToken = default)
    {
        // ===== שלב 1: אימות קלט =====
        if (string.IsNullOrWhiteSpace(request.DimensionCode))
        {
            return AssignmentEditResult.Fail("יש לציין מימד.");
        }

        if (!TryParseRuleType(request.RuleType, out var isBalance))
        {
            return AssignmentEditResult.Fail("סוג אילוץ לא תקין. השתמשי ב-Balance או Separation.");
        }

        // ===== שלב 2: הרשאה =====
        if (!await _assignmentLoader.AssignmentBelongsToManagerAsync(assignmentId, managerId, cancellationToken))
        {
            return AssignmentEditResult.Fail("החלוקה לא נמצאה.");
        }

        // ===== שלב 3: מציאת מימד במאגר המימדים =====
        var trimmedDimensionCode = request.DimensionCode.Trim();
        var dimensions = await _db.ClassificationDimensions.ToListAsync(cancellationToken);
        var dimension = dimensions.FirstOrDefault(
            entry => string.Equals(entry.DimensionCode, trimmedDimensionCode, StringComparison.OrdinalIgnoreCase));

        if (dimension is null)
        {
            return AssignmentEditResult.Fail($"מימד '{request.DimensionCode}' לא קיים.");
        }

        // ===== שלב 4: בדיקת כפילות =====
        var exists = await _db.AssignmentClassificationConstraints
            .AnyAsync(
                entry => entry.AssignmentId == assignmentId
                    && entry.ClassificationDimensionId == dimension.ClassificationDimensionId,
                cancellationToken);

        if (exists)
        {
            return AssignmentEditResult.Fail("אילוץ על מימד זה כבר קיים.");
        }

        // ===== שלב 5: הוספה + שמירה =====
        // IsBalanceOrSeparation=true → Balance; false → Separation.
        _db.AssignmentClassificationConstraints.Add(new AssignmentClassificationConstraint
        {
            AssignmentId = assignmentId,
            ClassificationDimensionId = dimension.ClassificationDimensionId,
            IsBalanceOrSeparation = isBalance,
        });

        await _db.SaveChangesAsync(cancellationToken);

        var detail = await FinishWithValidationAsync(assignmentId, managerId, cancellationToken);
        return detail is null
            ? AssignmentEditResult.Fail("החלוקה לא נמצאה.")
            : AssignmentEditResult.Ok(detail);
    }

    /// <summary>
    /// מוחק אילוץ סיווג לפי קוד מימד.
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="managerId">מזהה המנהל — בדיקת הרשאה.</param>
    /// <param name="dimensionCode">קוד מימד (case-insensitive).</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <returns>AssignmentEditResult עם Detail מעודכן, או הודעת שגיאה.</returns>
    /// <remarks>
    /// נקרא מ-: <see cref="AssignmentsController.DeleteClassificationConstraint"/>.
    /// </remarks>
    public async Task<AssignmentEditResult> DeleteClassificationConstraintAsync(
        int assignmentId,
        int managerId,
        string dimensionCode,
        CancellationToken cancellationToken = default)
    {
        // ===== שלב 1: הרשאה =====
        if (!await _assignmentLoader.AssignmentBelongsToManagerAsync(assignmentId, managerId, cancellationToken))
        {
            return AssignmentEditResult.Fail("החלוקה לא נמצאה.");
        }

        // ===== שלב 2: מציאת מימד =====
        var dimensions = await _db.ClassificationDimensions.ToListAsync(cancellationToken);
        var dimension = dimensions.FirstOrDefault(
            entry => string.Equals(entry.DimensionCode, dimensionCode, StringComparison.OrdinalIgnoreCase));

        if (dimension is null)
        {
            return AssignmentEditResult.Fail("האילוץ לא נמצא.");
        }

        // ===== שלב 3: מציאת שורת האילוץ =====
        var row = await _db.AssignmentClassificationConstraints
            .FirstOrDefaultAsync(
                entry => entry.AssignmentId == assignmentId
                    && entry.ClassificationDimensionId == dimension.ClassificationDimensionId,
                cancellationToken);

        if (row is null)
        {
            return AssignmentEditResult.Fail("האילוץ לא נמצא.");
        }

        // ===== שלב 4: מחיקה + החזרת פירוט =====
        _db.AssignmentClassificationConstraints.Remove(row);
        await _db.SaveChangesAsync(cancellationToken);

        var detail = await FinishWithValidationAsync(assignmentId, managerId, cancellationToken);
        return detail is null
            ? AssignmentEditResult.Fail("החלוקה לא נמצאה.")
            : AssignmentEditResult.Ok(detail);
    }

    /// <summary>
    /// לוגיקה משותפת להוספת אילוץ זוג — חובה או איסור.
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="managerId">מזהה המנהל — בדיקת הרשאה.</param>
    /// <param name="request">ת.ז. של שני המשתתפים.</param>
    /// <param name="isMandatory">true = זוג חובה; false = זוג איסור.</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <returns>AssignmentEditResult עם Detail מעודכן, או הודעת שגיאה.</returns>
    /// <remarks>
    /// נקרא מ-: <see cref="AddMandatoryPairAsync"/>, <see cref="AddForbiddenPairAsync"/>.
    /// </remarks>
    private async Task<AssignmentEditResult> AddPairAsync(
        int assignmentId,
        int managerId,
        AddPairConstraintRequest request,
        bool isMandatory,
        CancellationToken cancellationToken)
    {
        // ===== שלב 1: נרמול ת.ז. של שני המשתתפים =====
        if (!TryNormalizeIdentity(request.ParticipantA, out var identityA, out var errorA))
        {
            return AssignmentEditResult.Fail($"משתתף א: תעודת זהות {errorA}.");
        }

        if (!TryNormalizeIdentity(request.ParticipantB, out var identityB, out var errorB))
        {
            return AssignmentEditResult.Fail($"משתתף ב: תעודת זהות {errorB}.");
        }

        if (identityA == identityB)
        {
            return AssignmentEditResult.Fail("לא ניתן ליצור אילוץ בין משתתף לעצמו.");
        }

        // ===== שלב 2: הרשאה =====
        if (!await _assignmentLoader.AssignmentBelongsToManagerAsync(assignmentId, managerId, cancellationToken))
        {
            return AssignmentEditResult.Fail("החלוקה לא נמצאה.");
        }

        // ===== שלב 3: מציאת שיוכי המשתתפים =====
        var assignmentA = await FindParticipantAssignmentAsync(assignmentId, managerId, identityA, cancellationToken);
        var assignmentB = await FindParticipantAssignmentAsync(assignmentId, managerId, identityB, cancellationToken);

        if (assignmentA is null || assignmentB is null)
        {
            return AssignmentEditResult.Fail("שני המשתתפים חייבים להיות רשומים בחלוקה.");
        }

        var firstId = assignmentA.ParticipantAssignmentId;
        var secondId = assignmentB.ParticipantAssignmentId;

        // ===== שלב 4: הוספה לטבלה המתאימה =====
        if (isMandatory)
        {
            // בדיקת כפילות — האילוץ לא כיווני: (A,B) ≡ (B,A).
            var duplicate = await _db.MandatoryPairConstraints.AnyAsync(
                entry => entry.AssignmentId == assignmentId
                    && ((entry.FirstParticipantAssignmentId == firstId && entry.SecondParticipantAssignmentId == secondId)
                        || (entry.FirstParticipantAssignmentId == secondId && entry.SecondParticipantAssignmentId == firstId)),
                cancellationToken);

            if (duplicate)
            {
                return AssignmentEditResult.Fail("זוג חובה זה כבר קיים.");
            }

            _db.MandatoryPairConstraints.Add(new MandatoryPairConstraint
            {
                AssignmentId = assignmentId,
                FirstParticipantAssignmentId = firstId,
                SecondParticipantAssignmentId = secondId,
            });
        }
        else
        {
            var duplicate = await _db.ForbiddenPairConstraints.AnyAsync(
                entry => entry.AssignmentId == assignmentId
                    && ((entry.FirstParticipantAssignmentId == firstId && entry.SecondParticipantAssignmentId == secondId)
                        || (entry.FirstParticipantAssignmentId == secondId && entry.SecondParticipantAssignmentId == firstId)),
                cancellationToken);

            if (duplicate)
            {
                return AssignmentEditResult.Fail("זוג איסור זה כבר קיים.");
            }

            _db.ForbiddenPairConstraints.Add(new ForbiddenPairConstraint
            {
                AssignmentId = assignmentId,
                FirstParticipantAssignmentId = firstId,
                SecondParticipantAssignmentId = secondId,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        var detail = await FinishWithValidationAsync(assignmentId, managerId, cancellationToken);
        return detail is null
            ? AssignmentEditResult.Fail("החלוקה לא נמצאה.")
            : AssignmentEditResult.Ok(detail);
    }

    /// <summary>
    /// מוצא שיוך משתתף↔חלוקה לפי ת.ז., אחרי בדיקת הרשאה.
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="managerId">מזהה המנהל — בדיקת הרשאה.</param>
    /// <param name="identity">ת.ז. מנורמלת (9 ספרות).</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <returns>ParticipantAssignment או null אם אין הרשאה / לא נמצא.</returns>
    /// <remarks>
    /// נקרא מ-: <see cref="UpdateParticipantAsync"/>, <see cref="DeleteParticipantAsync"/>, <see cref="AddPairAsync"/>.
    /// </remarks>
    private async Task<ParticipantAssignment?> FindParticipantAssignmentAsync(
        int assignmentId,
        int managerId,
        string identity,
        CancellationToken cancellationToken)
    {
        if (!await _assignmentLoader.AssignmentBelongsToManagerAsync(assignmentId, managerId, cancellationToken))
        {
            return null;
        }

        // LINQ join — ParticipantAssignment.ParticipantId → Participants.IsraeliIdentityNumber.
        return await (
            from participantAssignment in _db.ParticipantAssignments
            join participant in _db.Participants on participantAssignment.ParticipantId equals participant.ParticipantId
            where participantAssignment.AssignmentId == assignmentId
                && participant.IsraeliIdentityNumber == identity
            select participantAssignment)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// מוחק כל אילוצי זוג (חובה + איסור) שבהם משתתף מעורב.
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="participantAssignmentId">מזהה שיוך משתתף↔חלוקה.</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <remarks>
    /// נקרא מ-: <see cref="DeleteParticipantAsync"/>.
    /// </remarks>
    private async Task RemovePairConstraintsForParticipantAsync(
        int assignmentId,
        int participantAssignmentId,
        CancellationToken cancellationToken)
    {
        // אילוץ רלוונטי אם המשתתף ב-First או ב-Second.
        var mandatory = await _db.MandatoryPairConstraints
            .Where(entry => entry.AssignmentId == assignmentId
                && (entry.FirstParticipantAssignmentId == participantAssignmentId
                    || entry.SecondParticipantAssignmentId == participantAssignmentId))
            .ToListAsync(cancellationToken);

        var forbidden = await _db.ForbiddenPairConstraints
            .Where(entry => entry.AssignmentId == assignmentId
                && (entry.FirstParticipantAssignmentId == participantAssignmentId
                    || entry.SecondParticipantAssignmentId == participantAssignmentId))
            .ToListAsync(cancellationToken);

        _db.MandatoryPairConstraints.RemoveRange(mandatory);
        _db.ForbiddenPairConstraints.RemoveRange(forbidden);
    }

    /// <summary>
    /// מוחק העדפות חברתיות (SocialPreferences) שבהן משתתף מעורב.
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="participantAssignmentId">מזהה שיוך משתתף↔חלוקה.</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <remarks>
    /// נקרא מ-: <see cref="DeleteParticipantAsync"/>.
    /// </remarks>
    private async Task RemoveSocialPreferencesForParticipantAsync(
        int assignmentId,
        int participantAssignmentId,
        CancellationToken cancellationToken)
    {
        var preferences = await _db.SocialPreferences
            .Where(entry => entry.AssignmentId == assignmentId
                && (entry.FromParticipantAssignmentId == participantAssignmentId
                    || entry.ToParticipantAssignmentId == participantAssignmentId))
            .ToListAsync(cancellationToken);

        _db.SocialPreferences.RemoveRange(preferences);
    }

    /// <summary>
    /// מעדכן או יוצר אילוץ מספר קבוצות (MinGroups/MaxGroups).
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="settings">הגדרות חדשות מה-DTO.</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <remarks>
    /// נקרא מ-: <see cref="UpdateAssignmentAsync"/>.
    /// </remarks>
    private async Task UpdateGroupCountAsync(
        int assignmentId,
        AssignmentSettingsDto settings,
        CancellationToken cancellationToken)
    {
        var groupCount = await _db.GroupCountConstraints
            .FirstOrDefaultAsync(entry => entry.AssignmentId == assignmentId, cancellationToken);

        if (groupCount is null)
        {
            // אין שורה קיימת — יוצרים חדשה.
            _db.GroupCountConstraints.Add(new GroupCountConstraint
            {
                AssignmentId = assignmentId,
                MinGroups = settings.MinGroups,
                MaxGroups = settings.MaxGroups,
            });
            return;
        }

        // עדכון in-place — EF יעקוב אחרי השינוי ב-SaveChanges.
        groupCount.MinGroups = settings.MinGroups;
        groupCount.MaxGroups = settings.MaxGroups;
    }

    /// <summary>
    /// מחליף את כל אילוצי גודל קבוצה — שורה אחת לכל GroupId מ-1 עד MaxGroups.
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="settings">הגדרות חדשות — Min/Max גודל זהה לכל הקבוצות.</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <remarks>
    /// נקרא מ-: <see cref="UpdateAssignmentAsync"/>.
    /// </remarks>
    private async Task UpdateGroupSizesAsync(
        int assignmentId,
        AssignmentSettingsDto settings,
        CancellationToken cancellationToken)
    {
        var existing = await _db.GroupSizeConstraints
            .Where(entry => entry.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        // מחיקה מלאה — בונה מחדש לפי MaxGroups הנוכחי.
        _db.GroupSizeConstraints.RemoveRange(existing);

        for (var groupId = 1; groupId <= settings.MaxGroups; groupId++)
        {
            _db.GroupSizeConstraints.Add(new GroupSizeConstraint
            {
                AssignmentId = assignmentId,
                GroupId = groupId,
                MinGroupSize = settings.MinGroupSize,
                MaxGroupSize = settings.MaxGroupSize,
            });
        }
    }

    /// <summary>
    /// מוסיף רשומות ParticipantClassification — יוצר מימד/רמה במאגר אם חסרים.
    /// </summary>
    /// <param name="participantAssignmentId">מזהה שיוך משתתף↔חלוקה.</param>
    /// <param name="classifications">מילון מימד→רמה.</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <returns>null בהצלחה; הודעת שגיאה בעברית אם הקלט לא תקין.</returns>
    /// <remarks>
    /// נקרא מ-: <see cref="AddParticipantAsync"/>, <see cref="UpdateParticipantAsync"/>.
    /// </remarks>
    private async Task<string?> ApplyClassificationsAsync(
        int participantAssignmentId,
        IReadOnlyDictionary<string, string> classifications,
        CancellationToken cancellationToken)
    {
        // טעינת מאגר מימדים — dimensionByCode מאפשר GetOrCreate בלי שאילתות כפולות.
        var dimensionByCode = await ClassificationCatalogHelper.LoadDimensionLookupAsync(_db, cancellationToken);
        var levels = await _db.ClassificationLevels.ToListAsync(cancellationToken);
        var levelIdByDimensionAndCode = levels.ToDictionary(
            level => (level.ClassificationDimensionId, level.LevelCode),
            level => level.ClassificationLevelId);

        foreach (var (dimensionCode, levelCode) in classifications)
        {
            if (string.IsNullOrWhiteSpace(dimensionCode) || string.IsNullOrWhiteSpace(levelCode))
            {
                return "סיווג לא תקין: מימד ורמה חייבים להיות מלאים.";
            }

            var dimension = await ClassificationCatalogHelper.GetOrCreateDimensionAsync(
                _db,
                dimensionByCode,
                dimensionCode,
                cancellationToken);

            var levelId = await ClassificationCatalogHelper.GetOrCreateLevelIdAsync(
                _db,
                levelIdByDimensionAndCode,
                dimension,
                levelCode,
                cancellationToken);

            _db.ParticipantClassifications.Add(new ParticipantClassification
            {
                ParticipantAssignmentId = participantAssignmentId,
                ClassificationDimensionId = dimension.ClassificationDimensionId,
                ClassificationLevelId = levelId,
            });
        }

        return null;
    }

    /// <summary>
    /// מאמת הגדרות קבוצות — בדיקות סטטיות ללא גישה ל-DB.
    /// </summary>
    /// <param name="settings">הגדרות מה-DTO; null = ללא הגדרות (תקין).</param>
    /// <returns>רשימת שגיאות בעברית; ריקה אם הכל תקין.</returns>
    /// <remarks>
    /// נקרא מ-: <see cref="UpdateAssignmentAsync"/>.
    /// </remarks>
    private static List<string> ValidateSettings(AssignmentSettingsDto? settings)
    {
        if (settings is null)
        {
            return new List<string>();
        }

        var errors = new List<string>();

        if (settings.MinGroups <= 0)
        {
            errors.Add("מספר קבוצות מינימום חייב להיות חיובי.");
        }

        if (settings.MaxGroups <= 0)
        {
            errors.Add("מספר קבוצות מקסימום חייב להיות חיובי.");
        }

        if (settings.MinGroups > settings.MaxGroups)
        {
            errors.Add("מספר קבוצות מינימום גדול מהמקסימום.");
        }

        if (settings.MinGroupSize <= 0)
        {
            errors.Add("גודל קבוצה מינימום חייב להיות חיובי.");
        }

        if (settings.MaxGroupSize <= 0)
        {
            errors.Add("גודל קבוצה מקסימום חייב להיות חיובי.");
        }

        if (settings.MinGroupSize > settings.MaxGroupSize)
        {
            errors.Add("גודל קבוצה מינימום גדול מהמקסימום.");
        }

        return errors;
    }

    /// <summary>
    /// בודק שמספר המשתתפים מתאים לקיבולת הכוללת של הקבוצות.
    /// </summary>
    /// <param name="settings">הגדרות קבוצות.</param>
    /// <param name="participantCount">מספר משתתפים נוכחי בחלוקה.</param>
    /// <returns>null אם תקין; הודעת שגיאה בעברית אחרת.</returns>
    /// <remarks>
    /// נקרא מ-: <see cref="UpdateAssignmentAsync"/>.
    /// </remarks>
    private static string? ValidateCapacity(AssignmentSettingsDto settings, int participantCount)
    {
        // קיבולת מינימום = MinGroups × MinGroupSize; מקסימום = MaxGroups × MaxGroupSize.
        var minCapacity = settings.MinGroups * settings.MinGroupSize;
        var maxCapacity = settings.MaxGroups * settings.MaxGroupSize;

        if (participantCount < minCapacity || participantCount > maxCapacity)
        {
            return $"מספר המשתתפים ({participantCount}) לא מתאים לקיבולת הקבוצות ({minCapacity}-{maxCapacity}).";
        }

        return null;
    }

    /// <summary>
    /// מפענח סוג אילוץ סיווג — Balance (איזון) או Separation (הפרדה).
    /// </summary>
    /// <param name="ruleType">מחרוזת מה-API — Balance/Separation או עברית.</param>
    /// <param name="isBalance">true = Balance; false = Separation.</param>
    /// <returns>true אם הסוג מוכר; false אחרת.</returns>
    /// <remarks>
    /// נקרא מ-: <see cref="AddClassificationConstraintAsync"/>.
    /// </remarks>
    private static bool TryParseRuleType(string ruleType, out bool isBalance)
    {
        isBalance = false;

        if (string.Equals(ruleType, "Balance", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ruleType, "איזון", StringComparison.Ordinal))
        {
            isBalance = true;
            return true;
        }

        if (string.Equals(ruleType, "Separation", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ruleType, "הפרדה", StringComparison.Ordinal))
        {
            isBalance = false;
            return true;
        }

        return false;
    }

    /// <summary>
    /// מריץ אימות מחדש וטוען DTO פירוט מעודכן — סיום סטנדרטי לכל פעולת עריכה.
    /// </summary>
    /// <param name="assignmentId">מזהה חלוקה ב-DB.</param>
    /// <param name="managerId">מזהה המנהל.</param>
    /// <param name="cancellationToken">ביטול אסינכרוני.</param>
    /// <returns>AssignmentDetailDto מלא; null אם אין גישה.</returns>
    /// <remarks>
    /// נקרא מ-: כל מתודות ה-public ב-class (אחרי SaveChanges).
    /// </remarks>
    private async Task<AssignmentDetailDto?> FinishWithValidationAsync(
        int assignmentId,
        int managerId,
        CancellationToken cancellationToken)
    {
        // ValidateAsync — מעדכן ValidationStatus ו-LastValidationErrors ב-Assignment.
        await _validator.ValidateAsync(assignmentId, managerId, cancellationToken);
        return await _detailLoader.GetDetailAsync(assignmentId, managerId, cancellationToken);
    }

    /// <summary>
    /// מנרמל ת.ז. ישראלית — 9 ספרות, ללא ספרה עשרונית מ-Excel.
    /// </summary>
    /// <param name="raw">קלט גולמי מה-API או מ-Excel.</param>
    /// <param name="normalized">ת.ז. מנורמלת (9 ספרות).</param>
    /// <param name="error">"חסרה" / "לא תקינה" — לשימוש בהודעת שגיאה.</param>
    /// <returns>true אם הת.ז. תקינה.</returns>
    /// <remarks>
    /// נקרא מ-: <see cref="AddParticipantAsync"/>, <see cref="UpdateParticipantAsync"/>,
    /// <see cref="DeleteParticipantAsync"/>, <see cref="AddPairAsync"/>.
    /// </remarks>
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

        // Excel לפעמים שומר ת.ז. כמספר עם ".0" בסוף — חותכים לפני הנקודה.
        var decimalSeparatorIndex = trimmed.IndexOf('.');
        if (decimalSeparatorIndex >= 0)
        {
            trimmed = trimmed[..decimalSeparatorIndex];
        }

        // ת.ז. ישראלית = בדיוק 9 ספרות.
        if (trimmed.Length != 9 || !trimmed.All(char.IsDigit))
        {
            return false;
        }

        normalized = trimmed;
        return true;
    }
}
