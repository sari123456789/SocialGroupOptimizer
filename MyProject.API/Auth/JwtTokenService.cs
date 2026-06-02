using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MyProject.API.Auth;

/// <summary>
/// יוצר JWT (JSON Web Token) למנהל מאומת.
/// </summary>
/// <remarks>
/// הטוקן נשלח ללקוח; בכל בקשה הבאה הלקוח מחזיר אותו בכותרת Authorization: Bearer ...
/// ASP.NET Core מאמת אותו דרך AddJwtBearer ב-Program.cs.
/// </remarks>
public sealed class JwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        // IOptions<JwtSettings> — DI מזריק הגדרות מ-appsettings (Jwt:Key, Issuer, Audience...).
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// בונה טוקן חתום עם Claims של המנהל.
    /// </summary>
    public string CreateToken(int managerId, string managerName)
    {
        // Claims — "תעודות זהות" בתוך הטוקן. השרת קורא אותן ב-User.GetManagerId().
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, managerId.ToString()),
            new Claim(ClaimTypes.Name, managerName),
            new Claim("managerId", managerId.ToString()), // claim מותאם — קל לשליפה
        };

        // SymmetricSecurityKey — מפתח סודי משותף; חייב להיות זהה ב-Program.cs (Validation).
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes);

        // JwtSecurityToken — מבנה הטוקן: issuer, audience, claims, תפוגה, חתימה.
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        // WriteToken — מחרוזת Base64 שניתן להעביר ללקוח.
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
