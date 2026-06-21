namespace MyProject.API.Auth;

/// <summary>
/// DTO לקבלת פרטי רישום של מנהל חדש מהלקוח.
/// </summary>
public sealed class RegisterRequestDto
{
    /// <summary>
    /// כתובת המייל. חייבת להיות ייחודית במערכת.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// סיסמה גולמית שהוזנה על ידי המשתמש. תשמש ליצירת Hash בלבד.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
