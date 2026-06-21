using Microsoft.AspNetCore.Mvc;

namespace MyProject.API.Auth;

/// <summary>
/// בקר התחברות — נקודת כניסה ציבורית (ללא [Authorize]).
/// </summary>
/// <remarks>
/// מחלקה זו היא שער הכניסה הציבורי למערכת. בניגוד לרוב בקרי ה-API,
/// היא אינה מוגנת ב-[Authorize], משום שמשתמש שעדיין לא התחבר חייב להיות
/// מסוגל לשלוח אליה שם מנהל וסיסמה. לאחר התחברות או רישום מוצלח, הבקר
/// מחזיר JWT ללקוח. הלקוח שומר את הטוקן ושולח אותו בבקשות הבאות בכותרת
/// Authorization. הבקר אינו מבצע בעצמו Hash לסיסמה ואינו ניגש ישירות
/// לכללי יצירת הטוקן; הוא מתאם בין AuthService, שמטפל באימות מול המסד,
/// לבין JwtTokenService, שמייצר את הטוקן החתום.
/// </remarks>
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
            request.Email,
            request.Password,
            cancellationToken);

        if (authResult is null)
        {
            return Unauthorized(new { message = "כתובת מייל או סיסמה שגויים." });
        }

        var token = _jwtTokenService.CreateToken(
            authResult.ManagerId,
            authResult.Email,
            authResult.ManagerName);

        return Ok(new LoginResponseDto
        {
            Token = token,
            ManagerId = authResult.ManagerId,
            Email = authResult.Email,
            ManagerName = authResult.ManagerName,
        });
    }

    /// <summary>
    /// POST /api/auth/register - יוצר מנהל חדש ומחזיר JWT להתחברות מיידית.
    /// </summary>
    /// <remarks>
    /// הקלט מגיע מהלקוח כ-JSON ומכיל שם מנהל וסיסמה. הפעולה מעבירה את
    /// הקלט ל-AuthService, שם מתבצעות בדיקות תקינות: שם מנהל לא ריק,
    /// סיסמה באורך מינימלי ושם מנהל שאינו קיים כבר במסד. אם הרישום מצליח,
    /// נוצר Manager חדש עם PasswordHash מוצפן, ואז נוצר טוקן בדיוק כמו
    /// במסלול ההתחברות. המשמעות מבחינת המשתמש היא שלאחר רישום מוצלח אין
    /// צורך להתחבר שוב ידנית.
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponseDto>> Register(
        [FromBody] RegisterRequestDto? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "בקשת רישום לא תקינה." });
        }

        var registrationResult = await _authService.RegisterAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (!registrationResult.Success || registrationResult.Manager is null)
        {
            return BadRequest(new { message = registrationResult.Error ?? "הרישום נכשל." });
        }

        var token = _jwtTokenService.CreateToken(
            registrationResult.Manager.ManagerId,
            registrationResult.Manager.Email,
            registrationResult.Manager.ManagerName);

        return Ok(new LoginResponseDto
        {
            Token = token,
            ManagerId = registrationResult.Manager.ManagerId,
            Email = registrationResult.Manager.Email,
            ManagerName = registrationResult.Manager.ManagerName,
        });
    }
}
