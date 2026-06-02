// =============================================================================
// InitialPlacementController — הרצת אלגוריתם חלוקה ראשונית.
// =============================================================================
// שלושה מסלולים:
// 1) POST initial — JSON גולמי (ללא [Authorize] — demo/פיתוח)
// 2) POST initial/assignment/{id} — מחלוקה במסד (מוגן)
// 3) POST initial/excel — קובץ Excel (ללא [Authorize])
// =============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.API.Auth;
using MyProject.API.Placement;
using MyProject.API.Placement.Excel;
using MyProject.BL.Algorithm.InitialPlacement.Orchestration;

namespace MyProject.API.Controllers;

[ApiController]
[Route("api/placement")]
public sealed class InitialPlacementController : ControllerBase
{
    private readonly InitialPlacementOrchestrator _orchestrator;
    private readonly AssignmentPlacementLoader _loader;
    private readonly AssignmentInitialPlacementValidator _placementValidator;
    private readonly AssignmentDetailLoader _detailLoader;
    private readonly ParticipantsExcelWorkbookReader _excelReader;
    private readonly ParticipantsExcelValidator _excelValidator;
    private readonly ParticipantsExcelToInitialPlacementMapper _excelMapper;

    public InitialPlacementController(
        InitialPlacementOrchestrator orchestrator,
        AssignmentPlacementLoader loader,
        AssignmentInitialPlacementValidator placementValidator,
        AssignmentDetailLoader detailLoader,
        ParticipantsExcelWorkbookReader excelReader,
        ParticipantsExcelValidator excelValidator,
        ParticipantsExcelToInitialPlacementMapper excelMapper)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _placementValidator = placementValidator ?? throw new ArgumentNullException(nameof(placementValidator));
        _detailLoader = detailLoader ?? throw new ArgumentNullException(nameof(detailLoader));
        _excelReader = excelReader ?? throw new ArgumentNullException(nameof(excelReader));
        _excelValidator = excelValidator ?? throw new ArgumentNullException(nameof(excelValidator));
        _excelMapper = excelMapper ?? throw new ArgumentNullException(nameof(excelMapper));
    }

    /// <summary>
    /// מסלול 1: קלט JSON ישיר → InitialPlacementOrchestrator.
    /// </summary>
    /// <remarks>ללא [Authorize] — נגיש ללא טוקן (שימושי לבדיקות).</remarks>
    [HttpPost("initial")]
    [ProducesResponseType(typeof(InitialPlacementResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(InitialPlacementErrorResponseDto), StatusCodes.Status400BadRequest)]
    public ActionResult<InitialPlacementResponseDto> RunInitial([FromBody] InitialPlacementRequestDto? request)
    {
        if (request is null)
        {
            return BadRequest(new InitialPlacementErrorResponseDto
            {
                Errors = new List<string> { "Request body is required." },
            });
        }

        try
        {
            var input = InitialPlacementDtoMapper.ToInput(request);
            var result = _orchestrator.Run(input);
            return Ok(InitialPlacementResponseDto.FromResult(result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new InitialPlacementErrorResponseDto
            {
                Errors = new List<string> { ex.Message },
            });
        }
    }

    /// <summary>
    /// מסלול 2: אימות חלוקה שמורה במסד — דורש JWT.
    /// </summary>
    [Authorize]
    [HttpPost("initial/assignment/{assignmentId:int}")]
    [ProducesResponseType(typeof(InitialPlacementResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(InitialPlacementErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InitialPlacementResponseDto>> RunInitialFromAssignment(
        int assignmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var managerId = User.GetManagerId();
            await _placementValidator.ValidateAsync(assignmentId, managerId, cancellationToken);
            var detail = await _detailLoader.GetDetailAsync(assignmentId, managerId, cancellationToken);

            if (detail is null)
            {
                return NotFound();
            }

            return Ok(new InitialPlacementResponseDto
            {
                Status = detail.LastPlacementStatus ?? detail.Status,
                Errors = detail.ValidationErrors,
                Groups = detail.PlacementGroups,
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new InitialPlacementErrorResponseDto
            {
                Errors = new List<string> { ex.Message },
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new InitialPlacementErrorResponseDto
            {
                Errors = new List<string> { ex.Message },
            });
        }
    }

    [HttpGet("initial/excel/template")]
    public IActionResult DownloadExcelTemplate()
    {
        var bytes = ParticipantsExcelTemplateGenerator.CreateTemplateBytes();
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ParticipantsExcelTemplateGenerator.TemplateFileName);
    }

    /// <summary>
    /// מסלול 3: Excel → קריאה, ולידציה, מיפוי, Orchestrator.
    /// </summary>
    /// <remarks>[Consumes("multipart/form-data")] — קובץ + פרמטרים בטופס.</remarks>
    [HttpPost("initial/excel")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(InitialPlacementResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(InitialPlacementErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InitialPlacementResponseDto>> RunInitialFromExcel(
        IFormFile? file,
        [FromForm] int groupCount,
        [FromForm] int minGroupSize,
        [FromForm] int maxGroupSize,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new InitialPlacementErrorResponseDto
            {
                Errors = new List<string> { "יש לצרף קובץ Excel." },
            });
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new InitialPlacementErrorResponseDto
            {
                Errors = new List<string> { "ניתן להעלות קובץ .xlsx בלבד." },
            });
        }

        if (groupCount < 1)
        {
            return BadRequest(new InitialPlacementErrorResponseDto
            {
                Errors = new List<string> { "groupCount חייב להיות לפחות 1." },
            });
        }

        if (minGroupSize < 1)
        {
            return BadRequest(new InitialPlacementErrorResponseDto
            {
                Errors = new List<string> { "minGroupSize חייב להיות לפחות 1." },
            });
        }

        if (maxGroupSize < minGroupSize)
        {
            return BadRequest(new InitialPlacementErrorResponseDto
            {
                Errors = new List<string> { "maxGroupSize לא יכול להיות קטן מ-minGroupSize." },
            });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var parseResult = _excelReader.Read(stream);

            var validationResult = _excelValidator.Validate(
                parseResult,
                groupCount,
                minGroupSize,
                maxGroupSize);

            if (!validationResult.IsValid)
            {
                return BadRequest(new InitialPlacementErrorResponseDto
                {
                    Errors = validationResult.Errors.ToList(),
                });
            }

            var mappingResult = _excelMapper.Map(
                parseResult,
                groupCount,
                minGroupSize,
                maxGroupSize);

            if (!mappingResult.Success || mappingResult.Input is null)
            {
                return BadRequest(new InitialPlacementErrorResponseDto
                {
                    Errors = mappingResult.Errors.ToList(),
                });
            }

            var result = _orchestrator.Run(mappingResult.Input);
            return Ok(InitialPlacementResponseDto.FromResult(result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new InitialPlacementErrorResponseDto
            {
                Errors = new List<string> { ex.Message },
            });
        }
    }
}

public sealed class InitialPlacementErrorResponseDto
{
    public List<string> Errors { get; set; } = new();
}
