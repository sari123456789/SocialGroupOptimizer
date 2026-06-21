using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.API.Auth;
using MyProject.API.Placement;
using MyProject.API.Placement.Excel;
using MyProject.Data.Import;

namespace MyProject.API.Controllers;

// [ApiController] — התנהגות API (ולידציה, binding)
[ApiController]
// [Authorize] ברמת מחלקה — כל המתודות דורשות JWT
[Authorize]
// נתיב בסיס לכל endpoints של חלוקות
[Route("api/assignments")]
/// <summary>
/// בקר מרכזי לניהול חלוקות שמורות של מנהל מחובר.
/// </summary>

public sealed class AssignmentsController : ControllerBase
{
    //  טוען רשימת חלוקות למנהל
    private readonly AssignmentPlacementLoader _loader;
    //   טוען משתתפים בחלוקה
    private readonly AssignmentParticipantsLoader _participantsLoader;
    //   טוען פרטי חלוקה מלאים
    private readonly AssignmentDetailLoader _detailLoader;
    //   עורך חלוקות (CRUD, אילוצים)
    private readonly AssignmentEditorService _editor;
    // ייבוא Excel גיליון יחיד
    private readonly AssignmentSingleSheetImporter _singleSheetImporter;
    // אימות והרצת שיבוץ ראשוני
    private readonly AssignmentInitialPlacementValidator _placementValidator;
    // שדה טוען הסברי שיבוץ 
    private readonly AssignmentExplanationLoader _explanationLoader;
    //  תצוגה מקדימה ויישום הזזות ידניות
    private readonly AssignmentManualMoveService _manualMoveService;

    // בנאי — Dependency Injection מזריק את כל השירותים
    public AssignmentsController(
        AssignmentPlacementLoader loader,
        AssignmentParticipantsLoader participantsLoader,
        AssignmentDetailLoader detailLoader,
        AssignmentEditorService editor,
        AssignmentSingleSheetImporter singleSheetImporter,
        AssignmentInitialPlacementValidator placementValidator,
        AssignmentExplanationLoader explanationLoader,
        AssignmentManualMoveService manualMoveService)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _participantsLoader = participantsLoader ?? throw new ArgumentNullException(nameof(participantsLoader));
        _detailLoader = detailLoader ?? throw new ArgumentNullException(nameof(detailLoader));
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _singleSheetImporter = singleSheetImporter ?? throw new ArgumentNullException(nameof(singleSheetImporter));
        _placementValidator = placementValidator ?? throw new ArgumentNullException(nameof(placementValidator));
        _explanationLoader = explanationLoader ?? throw new ArgumentNullException(nameof(explanationLoader));
        _manualMoveService = manualMoveService ?? throw new ArgumentNullException(nameof(manualMoveService));
    }

   

    // GET api/assignments — רשימת חלוקות
    [HttpGet]
    // תיעוד OpenAPI — רשימת סיכומי חלוקות
    [ProducesResponseType(typeof(IReadOnlyList<AssignmentSummaryDto>), StatusCodes.Status200OK)]
    // async Task<ActionResult<T>> — פעולה אסינכרונית
    public async Task<ActionResult<IReadOnlyList<AssignmentSummaryDto>>> List(CancellationToken cancellationToken)
    {
        // שליפת מזהה מנהל מה-JWT
        var managerId = User.GetManagerId();
        // await — טעינת חלוקות מהמסד לפי מנהל
        var assignments = await _loader.ListAssignmentsAsync(managerId, cancellationToken);
        // HTTP 200 עם הרשימה
        return Ok(assignments);
    }

 
    [HttpGet("{assignmentId:int}")]
    // תיעוד — פרטי חלוקה
    [ProducesResponseType(typeof(AssignmentDetailDto), StatusCodes.Status200OK)]
    // תיעוד — לא נמצא
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    // async — טעינת פרטי חלוקה בודדת
    public async Task<ActionResult<AssignmentDetailDto>> GetDetail(
        int assignmentId,
        CancellationToken cancellationToken)
    {
        // מזהה מנהל מהטוקן
        var managerId = User.GetManagerId();
        // await — שאילתת פרטים עם בידוד לפי מנהל
        var result = await _detailLoader.GetDetailAsync(assignmentId, managerId, cancellationToken);

        // null — חלוקה לא קיימת או לא שייכת למנהל
        if (result is null)
        {
            // HTTP 404
            return NotFound();
        }

        // HTTP 200 עם DTO מלא
        return Ok(result);
    }

    // PUT — עדכון מטא-דאטה של חלוקה
    [HttpPut("{assignmentId:int}")]
    [ProducesResponseType(typeof(AssignmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AssignmentEditResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentDetailDto>> Update(
        int assignmentId,
        // [FromBody] — דסריאליזציית JSON ל-DTO
        [FromBody] UpdateAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        var result = await _editor.UpdateAssignmentAsync(
            assignmentId,
            managerId,
            request,
            cancellationToken);

        // בדיקת דגל הצלחה בתוצאה
        if (!result.Success)
        {
            // HTTP 400 עם פרטי כישלון
            return BadRequest(result);
        }

        // HTTP 200 עם פרטי חלוקה מעודכנים
        return Ok(result.Detail);
    }

    // GET — רשימת משתתפים בחלוקה
    [HttpGet("{assignmentId:int}/participants")]
    [ProducesResponseType(typeof(AssignmentParticipantsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentParticipantsDto>> GetParticipants(
        int assignmentId,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        // await — טעינת משתתפים
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


    // GET — הסבר שיבוץ למשתתף בודד
    [HttpGet("{assignmentId:int}/participants/{participantId}/explanation")]
    [ProducesResponseType(typeof(AssignmentExplanationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentExplanationDto>> GetParticipantExplanation(
        int assignmentId,
        // string בנתיב — מספר זהות (ParticipantId)
        string participantId,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();

        // try — תופס שגיאות ולידציה
        try
        {
            // await — חישוב/טעינת הסבר (ללא שינוי מצב)
            var explanation = await _explanationLoader.GetExplanationAsync(
                assignmentId,
                participantId,
                managerId,
                cancellationToken);

            if (explanation is null)
            {
                return NotFound();
            }

            return Ok(explanation);
        }
        catch (ArgumentException ex)
        {
            // BadRequest — DTO ייבוא משמש גם להודעות שגיאה
            return BadRequest(new AssignmentImportResponseDto { Errors = new List<string> { ex.Message } });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new AssignmentImportResponseDto { Errors = new List<string> { ex.Message } });
        }
    }

    //   שינוי ידני של מנהל (Swap/Transfer) עם אישור חריגה  
    // Preview = Read Only. Apply שומר רק לפי הכללים (חוקי / override מאושר).

    // POST — תצוגה מקדימה להזזה ידנית (ללא שמירה)
    [HttpPost("{assignmentId:int}/manual-move/preview")]
    [ProducesResponseType(typeof(ManualMovePreviewResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManualMovePreviewResult>> ManualMovePreview(
        int assignmentId,
        [FromBody] ManualMoveRequestDto request,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        // await — הערכת ההזזה בלבד
        var preview = await _manualMoveService.PreviewAsync(
            assignmentId,
            managerId,
            request,
            cancellationToken);

        if (preview is null)
        {
            return NotFound();
        }

        return Ok(preview);
    }

    // POST  יישום הזזה ידנית 
    [HttpPost("{assignmentId:int}/manual-move/apply")]
    [ProducesResponseType(typeof(AssignmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ManualMovePreviewResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ManualMovePreviewResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    // IActionResult — מחזיר סוגי תגובה שונים לפי תוצאה
    public async Task<IActionResult> ManualMoveApply(
        int assignmentId,
        [FromBody] ManualMoveApplyRequestDto request,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        // await — ניסיון שמירת ההזזה
        var outcome = await _manualMoveService.ApplyAsync(
            assignmentId,
            managerId,
            request,
            cancellationToken);

        // switch expression  — בוחר ActionResult לפי ResultKind
        return outcome.Status switch
        {
            // דפוס התאמה — לא נמצא
            ManualMoveApplyOutcome.ResultKind.NotFound => NotFound(),
            // חסום — שגיאת קלט/אילוצים
            ManualMoveApplyOutcome.ResultKind.Blocked => BadRequest(outcome.Preview),
            // דורש אישור חריגה — קונפליקט 409
            ManualMoveApplyOutcome.ResultKind.NeedsOverride => Conflict(outcome.Preview),
            // הוחל בהצלחה
            ManualMoveApplyOutcome.ResultKind.Applied => Ok(outcome.Detail),
            // discard pattern _ — כל ערך אחר: שגיאת שרת
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    //   CRUD משתתפים  
    // [FromBody] — JSON → DTO. identity בנתיב = מספר זהות ישראלי (ParticipantId ב-Core).

    // POST — הוספת משתתף לחלוקה
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

    // PUT — עדכון משתתף לפי identity בנתיב
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

    // DELETE — מחיקת משתתף
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

    //  אילוצים: זוגות חובה / איסור / סיווג 
    // AssignmentEditorService מעדכן DB ומריץ אימות מחדש אם יש אילוצי סיווג.

    // POST — הוספת זוג חובה
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

    // DELETE — מחיקת זוג חובה לפי constraintId
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

    // POST — הוספת זוג אסור
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

    // DELETE — מחיקת זוג אסור
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

    // POST — הוספת אילוץ סיווג
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

    // DELETE — מחיקת אילוץ סיווג לפי dimensionCode
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

    //  תבניות Excel (הורדה) 

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

    //  ייבוא 
    // IFormFile — קובץ multipart מה-upload; ייבוא דרך גיליון Participants יחיד.

    // POST — יצירת חלוקה מרשימת משתתפים ב-JSON
    [HttpPost("create-from-participants")]
    [ProducesResponseType(typeof(AssignmentImportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AssignmentImportResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentImportResponseDto>> CreateFromParticipants(
        [FromBody] CreateAssignmentFromParticipantsRequest request,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        // await — יצירה דרך ייבוא גיליון יחיד (ללא קובץ)
        var result = await _singleSheetImporter.CreateFromParticipantsAsync(
            request,
            managerId,
            cancellationToken);

        // המרת תוצאה ל-DTO תגובה
        var response = AssignmentImportResponseDto.FromResult(result);

        if (!result.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    // POST multipart — ייבוא מקובץ Excel
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
        // ולידציה — קובץ חובה
        if (file is null || file.Length == 0)
        {
            return BadRequest(new AssignmentImportResponseDto
            {
                Errors = new List<string> { "יש לצרף קובץ Excel." },
            });
        }

        // בדיקת סיומת .xlsx
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
            // await — העתקת תוכן הקובץ לזיכרון
            await file.CopyToAsync(stream, cancellationToken);
            // איפוס מיקום לקריאה מההתחלה
            stream.Position = 0;

            var result = await _singleSheetImporter.ImportAsync(
                stream,
                managerId,
                assignmentName ?? "חלוקה חדשה",
                groupCount ?? 2,
                minGroupSize ?? 2,
                maxGroupSize ?? 2,
                cancellationToken);

            var response = AssignmentImportResponseDto.FromResult(result);

            if (!result.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        // catch כללי — כל שגיאה בייבוא
        catch (Exception ex)
        {
            // הודעה בעברית עם פרטי חריגה
            return BadRequest(new AssignmentImportResponseDto
            {
                Errors = new List<string> { $"שגיאה בייבוא: {ex.Message}" },
            });
        }
    }

    //  אימות חלוקה ראשונית 
    // AssignmentInitialPlacementValidator — DB → Orchestrator → שמירת תוצאה.

    // POST — הרצת אימות/שיבוץ על חלוקה שמורה
    [HttpPost("{assignmentId:int}/validate")]
    [ProducesResponseType(typeof(AssignmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentDetailDto>> ValidatePlacement(
        int assignmentId,
        CancellationToken cancellationToken)
    {
        var managerId = User.GetManagerId();
        // await — טוען מהמסד, מריץ אלגוריתם, שומר תוצאה
        await _placementValidator.ValidateAsync(assignmentId, managerId, cancellationToken);

        // טעינת מצב מעודכן לאחר האימות
        var detail = await _detailLoader.GetDetailAsync(assignmentId, managerId, cancellationToken);
        if (detail is null)
        {
            return NotFound();
        }

        return Ok(detail);
    }
}