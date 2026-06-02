using Microsoft.EntityFrameworkCore;
using MyProject.Data;

namespace MyProject.API.Auth;

/// <summary>
/// שירות אימות מנהלים: בודק שם וסיסמה מול המסד.
/// </summary>
/// <remarks>
/// לא מנפיק JWT — רק מחזיר ManagerAuthResult אם האימות הצליח.
/// JwtTokenService נקרא ב-AuthController לאחר הצלחה.
/// </remarks>
public sealed class AuthService
{
    private readonly ApplicationDbContext _db;

    public AuthService(ApplicationDbContext db)
    {
        // ?? throw — אם db הוא null, עוצרים מיד (Dependency Injection תקין לא אמור להעביר null).
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// מאמת מנהל לפי שם וסיסמה.
    /// </summary>
    /// <returns>ManagerAuthResult אם הצליח; null אם נכשל (לא זורק חריגה — כדי לא לחשוף אם השם קיים).</returns>
    public async Task<ManagerAuthResult?> AuthenticateAsync(
        string managerName,
        string password,
        CancellationToken cancellationToken = default)
    {
        // בדיקה מוקדמת — קלט ריק לא שווה "משתמש לא קיים", אבל חוסך שאילתה למסד.
        if (string.IsNullOrWhiteSpace(managerName) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        // AsNoTracking — קריאה בלבד, בלי מעקב EF (מהיר יותר, לא משנה ישות).
        // FirstOrDefaultAsync — מחזיר את הרשומה הראשונה או null.
        var manager = await _db.Managers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                entry => entry.ManagerName == managerName.Trim(),
                cancellationToken);

        if (manager is null || string.IsNullOrWhiteSpace(manager.PasswordHash))
        {
            // null — אותה תשובה כמו סיסמה שגויה (אבטחה: לא מגלים אם השם קיים).
            return null;
        }

        // BCrypt.Verify — משווה סיסמה גולמית ל-hash שמור. לא שומרים סיסמה בטקסט גלוי.
        if (!BCrypt.Net.BCrypt.Verify(password, manager.PasswordHash))
        {
            return null;
        }

        // record — טיפוס immutable קצר; ManagerId + ManagerName מספיקים ל-JWT.
        return new ManagerAuthResult(manager.ManagerId, manager.ManagerName);
    }
}

public sealed record ManagerAuthResult(int ManagerId, string ManagerName);
