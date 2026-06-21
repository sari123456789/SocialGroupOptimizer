using Microsoft.AspNetCore.Mvc;

// לוגיקה: כל הבקרים ב-API יושבים תחת מרחב השמות הזה.
namespace MyProject.API.Controllers;

/// <summary>
/// נקודת בדיקה פשוטה לצד הלקוח (fetch) בלי לוגיקה עסקית.
/// </summary>
// סינטקס: [ApiController] — מפעיל ולידציית מודל אוטומטית ומיפוי שגיאות 400.
[ApiController]
// סינטקס: [Route("api/[controller]")] — [controller] מוחלף בשם המחלקה בלי הסיומת Controller → api/Health.
[Route("api/[controller]")]
// סינטקס: public class — בקר ציבורי; יורש מ-ControllerBase (לא Controller מלא עם Views).
public class HealthController : ControllerBase
{
    // סינטקס: [HttpGet] — ממפה GET לנתיב של הבקר (api/Health).
    // לוגיקה: לקוח בודק שהשרת חי בלי JWT וללא מסד.
    [HttpGet]
    // סינטקס: IActionResult — תוצאת HTTP גמישה; => expression body מחזיר Ok(...) ישירות.
    public IActionResult Get() =>
        // סינטקס: Ok(object) — 200 עם JSON; new { } = אנונימי טיפוס עם ok, message, at.
        // לוגיקה: at = חותמת UTC לדיבוג/ניטור.
        Ok(new { ok = true, message = "השרת פעיל", at = DateTime.UtcNow });
}