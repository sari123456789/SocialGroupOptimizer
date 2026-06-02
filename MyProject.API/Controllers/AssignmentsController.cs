// =============================================================================
// AssignmentsController — REST API לניהול חלוקות (Assignments).
// =============================================================================
// [Authorize] על כל הבקר — כל endpoint דורש JWT תקף.
// User.GetManagerId() — extension ב-ManagerUserExtensions; שולף managerId מהטוקן.
// כל פעולה מעבירה managerId לשירותי Placement — בידוד נתונים לפי מנהל.
// =============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.API.Auth;
using MyProject.API.Placement;
using MyProject.API.Placement.Excel;
using MyProject.Data.Import;

namespace MyProject.API.Controllers;

[ApiController]
[Authorize]
[Route("api/assignments")]
public sealed class AssignmentsController : ControllerBase
{
    private readonly AssignmentPlacementLoader _loader;
    private readonly AssignmentParticipantsLoader _participantsLoader;
    private readonly AssignmentDetailLoader _detailLoader;
    private readonly AssignmentEditorService _editor;
    private readonly ParticipantExcelImporter _importer;
    private readonly AssignmentSingleSheetImporter _singleSheetImporter;
    private readonly AssignmentInitialPlacementValidator _placementValidator;

    public AssignmentsController(
        AssignmentPlacementLoader loader,
        AssignmentParticipantsLoader participantsLoader,
        AssignmentDetailLoader detailLoader,
        AssignmentEditorService editor,
        ParticipantExcelImporter importer,
        AssignmentSingleSheetImporter singleSheetImporter,
        AssignmentInitialPlacementValidator placementValidator)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _participantsLoader = participantsLoader ?? throw new ArgumentNullException(nameof(participantsLoader));
        _detailLoader = detailLoader ?? throw new ArgumentNullException(nameof(detailLoader));
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _importer = importer ?? throw new ArgumentNullException(nameof(importer));
        _singleSheetImporter = singleSheetImporter ?? throw new ArgumentNullException(nameof(singleSheetImporter));
        _placementValidator = placementValidator ?? throw new ArgumentNullException(nameof(placementValidator));
    }

    // ===== קריאה: רשימה ופרטים =====

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AssignmentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AssignmentSummaryDto>>> List(CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        var assignments = await _loader.ListAssignmentsAsync(managerId, cancellationToken);
        return Ok(assignments);
    }

    [HttpGet("{assignmentId:int}")]
    [ProducesResponseType(typeof(AssignmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentDetailDto>> GetDetail(
        int assignmentId,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        var result = await _detailLoader.GetDetailAsync(assignmentId, managerId, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPut("{assignmentId:int}")]
    [ProducesResponseType(typeof(AssignmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AssignmentEditResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentDetailDto>> Update(
        int assignmentId,
        [FromBody] UpdateAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        var result = await _editor.UpdateAssignmentAsync(
            assignmentId,
            managerId,
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result.Detail);
    }

    [HttpGet("{assignmentId:int}/participants")]
    [ProducesResponseType(typeof(AssignmentParticipantsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentParticipantsDto>> GetParticipants(
        int assignmentId,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        var result = await _participantsLoader.GetParticipantsAsync(
            assignmentId,
            managerId,
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    // ===== CRUD משתתפים =====
    // [FromBody] — JSON → DTO. identity בנתיב = מספר זהות ישראלי (ParticipantId ב-Core).

    [HttpPost("{assignmentId:int}/participants")]
    [ProducesResponseType(typeof(AssignmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AssignmentEditResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentDetailDto>> AddParticipant(
        int assignmentId,
        [FromBody] AddParticipantRequest request,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        var result = await _editor.AddParticipantAsync(
            assignmentId,
            managerId,
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result.Detail);
    }

    [HttpPut("{assignmentId:int}/participants/{identity}")]
    [ProducesResponseType(typeof(AssignmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AssignmentEditResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentDetailDto>> UpdateParticipant(
        int assignmentId,
        string identity,
        [FromBody] UpdateParticipantRequest request,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        var result = await _editor.UpdateParticipantAsync(
            assignmentId,
            managerId,
            identity,
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result.Detail);
    }

    [HttpDelete("{assignmentId:int}/participants/{identity}")]
    [ProducesResponseType(typeof(AssignmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AssignmentEditResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentDetailDto>> DeleteParticipant(
        int assignmentId,
        string identity,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        var result = await _editor.DeleteParticipantAsync(
            assignmentId,
            managerId,
            identity,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result.Detail);
    }

    // ===== אילוצים: זוגות חובה / איסור / סיווג =====
    // AssignmentEditorService מעדכן DB ומריץ אימות מחדש אם יש אילוצי סיווג.

    [HttpPost("{assignmentId:int}/constraints/mandatory-pairs")]
    [ProducesResponseType(typeof(AssignmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AssignmentEditResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentDetailDto>> AddMandatoryPair(
        int assignmentId,
        [FromBody] AddPairConstraintRequest request,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        var result = await _editor.AddMandatoryPairAsync(
            assignmentId,
            managerId,
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result.Detail);
    }

    [HttpDelete("{assignmentId:int}/constraints/mandatory-pairs/{constraintId:int}")]
    [ProducesResponseType(typeof(AssignmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AssignmentEditResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentDetailDto>> DeleteMandatoryPair(
        int assignmentId,
        int constraintId,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        var result = await _editor.DeleteMandatoryPairAsync(
            assignmentId,
            managerId,
            constraintId,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result.Detail);
    }

    [HttpPost("{assignmentId:int}/constraints/forbidden-pairs")]
    [ProducesResponseType(typeof(AssignmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AssignmentEditResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentDetailDto>> AddForbiddenPair(
        int assignmentId,
        [FromBody] AddPairConstraintRequest request,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        var result = await _editor.AddForbiddenPairAsync(
            assignmentId,
            managerId,
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result.Detail);
    }

    [HttpDelete("{assignmentId:int}/constraints/forbidden-pairs/{constraintId:int}")]
    [ProducesResponseType(typeof(AssignmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AssignmentEditResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentDetailDto>> DeleteForbiddenPair(
        int assignmentId,
        int constraintId,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        var result = await _editor.DeleteForbiddenPairAsync(
            assignmentId,
            managerId,
            constraintId,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result.Detail);
    }

    [HttpPost("{assignmentId:int}/constraints/classification")]
    [ProducesResponseType(typeof(AssignmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AssignmentEditResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentDetailDto>> AddClassificationConstraint(
        int assignmentId,
        [FromBody] AddClassificationConstraintRequest request,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        var result = await _editor.AddClassificationConstraintAsync(
            assignmentId,
            managerId,
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result.Detail);
    }

    [HttpDelete("{assignmentId:int}/constraints/classification/{dimensionCode}")]
    [ProducesResponseType(typeof(AssignmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AssignmentEditResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentDetailDto>> DeleteClassificationConstraint(
        int assignmentId,
        string dimensionCode,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        var result = await _editor.DeleteClassificationConstraintAsync(
            assignmentId,
            managerId,
            dimensionCode,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result.Detail);
    }

    // ===== תבניות Excel (הורדה) =====
    // File(...) — מחזיר קובץ בינארי עם Content-Type של xlsx.

    [HttpGet("template/stress-test-20")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public IActionResult DownloadStressTestTemplate()
    {
        var bytes = StressTestParticipantsWorkbookGenerator.CreateWorkbookBytes();
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            StressTestParticipantsWorkbookGenerator.FileName);
    }

    [HttpGet("template")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public IActionResult DownloadTemplate()
    {
        var bytes = ParticipantsExcelTemplateGenerator.CreateTemplateBytes();
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ParticipantsExcelTemplateGenerator.TemplateFileName);
    }

    [HttpGet("template/advanced")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public IActionResult DownloadAdvancedTemplate()
    {
        var bytes = ExcelTemplateGenerator.CreateTemplateBytes();
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ExcelTemplateDefinitions.TemplateFileName);
    }

    // ===== ייבוא =====
    // IFormFile — קובץ multipart מה-upload. AssignmentWorkbookFormatDetector בוחר מסלול ייבוא.

    [HttpPost("create-from-participants")]
    [ProducesResponseType(typeof(AssignmentImportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AssignmentImportResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentImportResponseDto>> CreateFromParticipants(
        [FromBody] CreateAssignmentFromParticipantsRequest request,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        var result = await _singleSheetImporter.CreateFromParticipantsAsync(
            request,
            managerId,
            cancellationToken);

        var response = AssignmentImportResponseDto.FromResult(result);

        if (!result.Success)
        {
            return BadRequest(response);
        }

        if (result.AssignmentId.HasValue)
        {
            await _placementValidator.ValidateAsync(
                result.AssignmentId.Value,
                managerId,
                cancellationToken);
        }

        return Ok(response);
    }

    [HttpPost("import")]
    [ProducesResponseType(typeof(AssignmentImportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AssignmentImportResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentImportResponseDto>> Import(
        IFormFile? file,
        [FromForm] string? assignmentName,
        [FromForm] int? groupCount,
        [FromForm] int? minGroupSize,
        [FromForm] int? maxGroupSize,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new AssignmentImportResponseDto
            {
                Errors = new List<string> { "יש לצרף קובץ Excel." },
            });
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new AssignmentImportResponseDto
            {
                Errors = new List<string> { "ניתן להעלות קובץ .xlsx בלבד." },
            });
        }

        var managerId = User.GetManagerId();

        try
        {
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            stream.Position = 0;

            ExcelImportResult result;
            if (AssignmentWorkbookFormatDetector.IsSingleSheetParticipantsFormat(stream))
            {
                stream.Position = 0;
                result = await _singleSheetImporter.ImportAsync(
                    stream,
                    managerId,
                    assignmentName ?? "חלוקה חדשה",
                    groupCount ?? 2,
                    minGroupSize ?? 2,
                    maxGroupSize ?? 2,
                    cancellationToken);
            }
            else
            {
                stream.Position = 0;
                result = await _importer.ImportAsync(stream, managerId, cancellationToken);
            }

            var response = AssignmentImportResponseDto.FromResult(result);

            if (!result.Success)
            {
                return BadRequest(response);
            }

            if (result.AssignmentId.HasValue)
            {
                await _placementValidator.ValidateAsync(
                    result.AssignmentId.Value,
                    managerId,
                    cancellationToken);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new AssignmentImportResponseDto
            {
                Errors = new List<string> { $"שגיאה בייבוא: {ex.Message}" },
            });
        }
    }

    // ===== אימות חלוקה ראשונית =====
    // AssignmentInitialPlacementValidator — DB → Orchestrator → שמירת תוצאה.

    [HttpPost("{assignmentId:int}/validate")]
    [ProducesResponseType(typeof(AssignmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentDetailDto>> ValidatePlacement(
        int assignmentId,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        await _placementValidator.ValidateAsync(assignmentId, managerId, cancellationToken);

        var detail = await _detailLoader.GetDetailAsync(assignmentId, managerId, cancellationToken);
        if (detail is null)
        {
            return NotFound();
        }

        return Ok(detail);
    }
}
