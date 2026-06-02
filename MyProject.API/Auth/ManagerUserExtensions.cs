using System.Security.Claims;

namespace MyProject.API.Auth;

/// <summary>
/// הרחבות ל-ClaimsPrincipal — שליפת מזהה מנהל מהטוקן JWT.
/// </summary>
public static class ManagerUserExtensions
{
    /// <summary>
    /// מחזיר את ManagerId מה-Claims שב-JWT.
    /// </summary>
    /// <remarks>
    /// this ClaimsPrincipal — extension method: נקרא כ-User.GetManagerId() בבקרים.
    /// FindFirstValue — מחפש claim לפי שם; מחזיר string או null.
    /// ?? — אם "managerId" חסר, מנסים ClaimTypes.NameIdentifier (גיבוי).
    /// </remarks>
    public static int GetManagerId(this ClaimsPrincipal user)
    {
        var managerIdClaim = user.FindFirstValue("managerId")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        // int.TryParse — בטוח יותר מ-int.Parse; out var managerId מכריז על משתנה ביציאה.
        if (managerIdClaim is null || !int.TryParse(managerIdClaim, out var managerId))
        {
            throw new UnauthorizedAccessException("Manager id claim is missing.");
        }

        return managerId;
    }
}
