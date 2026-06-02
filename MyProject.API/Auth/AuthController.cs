using Microsoft.AspNetCore.Mvc;

namespace MyProject.API.Auth;

/// <summary>
/// בקר התחברות — נקודת כניסה ציבורית (ללא [Authorize]).
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(AuthService authService, JwtTokenService jwtTokenService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
    }

    /// <summary>
    /// POST /api/auth/login — מקבל שם מנהל וסיסמה, מחזיר JWT.
    /// </summary>
    /// <remarks>
    /// [FromBody] — ASP.NET Core מפרסר JSON מהגוף ל-LoginRequestDto.
    /// ProducesResponseType — תיעוד ל-Swagger על קודי תשובה אפשריים.
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequestDto? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Unauthorized();
        }

        // שלב 1: אימות מול המסד (BCrypt).
        var authResult = await _authService.AuthenticateAsync(
            request.ManagerName,
            request.Password,
            cancellationToken);

        if (authResult is null)
        {
            // 401 + הודעה בעברית — לא מפרטים אם השם או הסיסמה שגויים.
            return Unauthorized(new { message = "שם מנהל או סיסמה שגויים." });
        }

        // שלב 2: הנפקת JWT.
        var token = _jwtTokenService.CreateToken(authResult.ManagerId, authResult.ManagerName);

        // Ok(...) — 200 + גוף JSON (LoginResponseDto).
        return Ok(new LoginResponseDto
        {
            Token = token,
            ManagerId = authResult.ManagerId,
            ManagerName = authResult.ManagerName,
        });
    }
}
