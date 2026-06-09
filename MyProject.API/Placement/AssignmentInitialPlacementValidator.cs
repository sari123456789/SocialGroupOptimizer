using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
///   <item><description>הרצת AssignmentPlacementRunner.RunAndImprove</description></item>
///   <item><description>שמירת סטטוס, שגיאות וקבוצות ב-Assignment</description></item>
/// </list>
/// </remarks>
public sealed class AssignmentInitialPlacementValidator
{
    private readonly ApplicationDbContext _db;
    private readonly AssignmentPlacementLoader _loader;
    private readonly AssignmentPlacementRunner _placementRunner;

    public AssignmentInitialPlacementValidator(
        ApplicationDbContext db,
        AssignmentPlacementLoader loader,
        AssignmentPlacementRunner placementRunner)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _placementRunner = placementRunner ?? throw new ArgumentNullException(nameof(placementRunner));
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
            var runResult = _placementRunner.RunAndImprove(input);
            ApplyResult(assignment, runResult);
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

    private static void ApplyResult(Data.Models.Assignment assignment, AssignmentPlacementRunResult runResult)
    {
        var result = runResult.PlacementResult;

        assignment.LastValidatedAtUtc = DateTime.UtcNow;
        assignment.LastPlacementStatus = result.Status.ToString();

        var displayErrors = InitialPlacementUserMessages.ForDisplay(result).ToList();
        if (!string.IsNullOrWhiteSpace(runResult.Warning))
        {
            displayErrors.Add(runResult.Warning);
        }

        assignment.LastValidationErrors = SerializeErrors(displayErrors);

        if (result.Status is InitialPlacementStatus.Success or InitialPlacementStatus.SuccessViaSolver
            && runResult.FinalAssignment is not null)
        {
            assignment.ValidationStatus = "Validated";
            assignment.LastPlacementGroupsJson = PlacementGroupSerialization.SerializeGroups(runResult.FinalAssignment);
            return;
        }

        assignment.ValidationStatus = "ValidationFailed";
        assignment.LastPlacementGroupsJson = null;
    }

    private static void ApplyFailure(Data.Models.Assignment assignment, string error)
    {
        assignment.LastValidatedAtUtc = DateTime.UtcNow;
        assignment.ValidationStatus = "ValidationFailed";
        assignment.LastPlacementStatus = null;
        assignment.LastValidationErrors = SerializeErrors(new[] { error });
        assignment.LastPlacementGroupsJson = null;
    }

    private static void ApplyPending(Data.Models.Assignment assignment)
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

}
