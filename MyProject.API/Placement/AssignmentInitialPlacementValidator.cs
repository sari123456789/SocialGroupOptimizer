using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyProject.BL.Algorithm.InitialPlacement.Orchestration;
using MyProject.BL.Algorithm.InitialPlacement.Results;
using MyProject.Data;
using MyProject.Data.Models;

namespace MyProject.API.Placement;

/// <summary>
/// מריץ אימות חלוקה ראשונית על חלוקה שמורה במסד, ושומר את התוצאה.
/// </summary>
/// <remarks>
/// <para>זרימה:</para>
/// <list type="number">
///   <item><description>בדיקה שהחלוקה שייכת למנהל</description></item>
///   <item><description>מריץ אימות גם בלי אילוצי סיווג (קבוצות + זוגות מספיקים)</description></item>
///   <item><description>טעינת קלט דרך AssignmentPlacementLoader</description></item>
///   <item><description>הרצת InitialPlacementOrchestrator.Run</description></item>
///   <item><description>שמירת סטטוס, שגיאות וקבוצות ב-Assignment</description></item>
/// </list>
/// </remarks>
public sealed class AssignmentInitialPlacementValidator
{
    private readonly ApplicationDbContext _db;
    private readonly AssignmentPlacementLoader _loader;
    private readonly InitialPlacementOrchestrator _orchestrator;

    public AssignmentInitialPlacementValidator(
        ApplicationDbContext db,
        AssignmentPlacementLoader loader,
        InitialPlacementOrchestrator orchestrator)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public async Task ValidateAsync(
        int assignmentId,
        int managerId,
        CancellationToken cancellationToken = default)
    {
        // ===== שלב 1: הרשאה — רק מנהל שבבעלותו החלוקה =====
        var belongsToManager = await _loader.AssignmentBelongsToManagerAsync(
            assignmentId,
            managerId,
            cancellationToken);

        if (!belongsToManager)
        {
            // שקט — לא זורקים; הבקר יחזיר 404 אם צריך.
            return;
        }

        // FirstOrDefaultAsync — טוען ישות Assignment לעדכון (עם tracking).
        var assignment = await _db.Assignments
            .FirstOrDefaultAsync(entry => entry.AssignmentId == assignmentId, cancellationToken);

        if (assignment is null)
        {
            return;
        }

        // ===== שלב 2: הרצת האלגוריתם =====
        // אין חובה על אילוצי סיווג — מספיקים אילוצי קבוצות וזוגות.
        try
        {
            // LoadInputAsync — DB → InitialPlacementInput (Core + BL).
            var input = await _loader.LoadInputAsync(assignmentId, managerId, cancellationToken);
            var result = _orchestrator.Run(input);
            ApplyResult(assignment, result);
        }
        catch (ArgumentException ex)
        {
            ApplyFailure(assignment, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            ApplyFailure(assignment, ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyResult(Assignment assignment, InitialPlacementResult result)
    {
        assignment.LastValidatedAtUtc = DateTime.UtcNow;
        assignment.LastPlacementStatus = result.Status.ToString();
        // ForDisplay — מתרגם שגיאות טכnicas להודעות בעברית למשתמש.
        assignment.LastValidationErrors = SerializeErrors(InitialPlacementUserMessages.ForDisplay(result));

        // pattern matching: is Success or SuccessViaSolver — C# 9+.
        if (result.Status is InitialPlacementStatus.Success or InitialPlacementStatus.SuccessViaSolver
            && result.Assignment is not null)
        {
            assignment.ValidationStatus = "Validated";
            assignment.LastPlacementGroupsJson = SerializeGroups(result);
            return;
        }

        assignment.ValidationStatus = "ValidationFailed";
        assignment.LastPlacementGroupsJson = null;
    }

    private static void ApplyFailure(Assignment assignment, string error)
    {
        assignment.LastValidatedAtUtc = DateTime.UtcNow;
        assignment.ValidationStatus = "ValidationFailed";
        assignment.LastPlacementStatus = null;
        assignment.LastValidationErrors = SerializeErrors(new[] { error });
        assignment.LastPlacementGroupsJson = null;
    }

    private static void ApplyPending(Assignment assignment)
    {
        assignment.ValidationStatus = "PendingValidation";
        assignment.LastPlacementStatus = null;
        assignment.LastValidationErrors = null;
        assignment.LastPlacementGroupsJson = null;
    }

    private static string? SerializeErrors(IReadOnlyList<string> errors)
    {
        if (errors.Count == 0)
        {
            return null;
        }

        // JsonSerializer — שומר רשימת מחרוזות כ-JSON בעמודה LastValidationErrors.
        return JsonSerializer.Serialize(errors);
    }

    private static string SerializeGroups(InitialPlacementResult result)
    {
        var groups = result.Assignment!.Groups
            .OrderBy(group => group.Id.Value)
            .Select(group => new StoredPlacementGroup
            {
                GroupId = group.Id.Value,
                ParticipantIds = group.ParticipantIds
                    .Select(participantId => participantId.Value)
                    .ToList(),
            })
            .ToList();

        return JsonSerializer.Serialize(groups);
    }

    // מחלקה פנימית — מבנה JSON לשמירה במסד (לא DTO ללקוח).
    private sealed class StoredPlacementGroup
    {
        public int GroupId { get; set; }

        public List<string> ParticipantIds { get; set; } = new();
    }
}
