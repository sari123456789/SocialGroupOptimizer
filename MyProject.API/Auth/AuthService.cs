using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using MyProject.Data;
using MyProject.Data.Models;

namespace MyProject.API.Auth;

/// <summary>
/// שירות אימות מנהלים: בודק מייל וסיסמה מול המסד.
/// </summary>
public sealed class AuthService
{
    private readonly ApplicationDbContext _db;

    public AuthService(ApplicationDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<ManagerAuthResult?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeEmail(email, out var normalizedEmail) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var manager = await _db.Managers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                entry => entry.Email == normalizedEmail,
                cancellationToken);

        if (manager is null || string.IsNullOrWhiteSpace(manager.PasswordHash))
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(password, manager.PasswordHash))
        {
            return null;
        }

        return new ManagerAuthResult(manager.ManagerId, manager.Email, manager.ManagerName);
    }

    public async Task<ManagerRegistrationResult> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeEmail(email, out var normalizedEmail))
        {
            return ManagerRegistrationResult.Fail("כתובת מייל לא תקינה.");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            return ManagerRegistrationResult.Fail("סיסמה חייבת להכיל לפחות 6 תווים.");
        }

        var emailExists = await _db.Managers
            .AnyAsync(entry => entry.Email == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return ManagerRegistrationResult.Fail("כתובת המייל כבר רשומה במערכת.");
        }

        var manager = new Manager
        {
            Email = normalizedEmail,
            ManagerName = BuildDisplayNameFromEmail(normalizedEmail),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        };

        _db.Managers.Add(manager);
        await _db.SaveChangesAsync(cancellationToken);

        return ManagerRegistrationResult.Ok(
            new ManagerAuthResult(manager.ManagerId, manager.Email, manager.ManagerName));
    }

    private static bool TryNormalizeEmail(string? email, out string normalizedEmail)
    {
        normalizedEmail = string.Empty;

        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var trimmed = email.Trim();

        try
        {
            var parsed = new MailAddress(trimmed);
            if (!string.Equals(parsed.Address, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        catch (FormatException)
        {
            return false;
        }

        normalizedEmail = trimmed.ToLowerInvariant();
        return true;
    }

    private static string BuildDisplayNameFromEmail(string normalizedEmail)
    {
        var atIndex = normalizedEmail.IndexOf('@');
        return atIndex > 0
            ? normalizedEmail[..atIndex]
            : normalizedEmail;
    }
}

public sealed record ManagerAuthResult(int ManagerId, string Email, string ManagerName);

public sealed record ManagerRegistrationResult(
    bool Success,
    ManagerAuthResult? Manager,
    string? Error)
{
    public static ManagerRegistrationResult Ok(ManagerAuthResult manager) =>
        new(true, manager, null);

    public static ManagerRegistrationResult Fail(string error) =>
        new(false, null, error);
}
