using Microsoft.AspNetCore.Mvc;

namespace MyProject.API.Controllers;

/// <summary>
/// נקודת בדיקה פשוטה לצד הלקוח (fetch) בלי לוגיקה עסקית.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() =>
        Ok(new { ok = true, message = "השרת פעיל", at = DateTime.UtcNow });
}
