using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyProject.BL.Algorithm.InitialPlacement.Results;
using MyProject.Data;
using MyProject.Data.Models;

namespace MyProject.API.Placement;

/// <summary>
/// מריץ אימות חלוקה ראשונית על חלוקה שמורה במסד, ושומר את התוצאה.
/// </summary>
/// <remarks>
/// <para>זרימה:</para>
/// <list type="number">
///   <item><description>בדיקה שהחלוקה שייכת למנהל</description></item>
///   <item><description>מריץ אימות גם בלי אילוצי סיווג (קבוצות + זוגות מספיקים)</description></item>
///   <item><description>טעינת קלט דרך AssignmentPlacementLoader</description></item>
///   <item><description>הרצת AssignmentPlacementRunner.RunAndImprove</description></item>
///   <item><description>שמירת סטטוס, שגיאות וקבוצות ב-Assignment</description></item>
/// </list>
/// </remarks>
/// <remarks>
/// למרות השם Validator, המחלקה אינה רק בודקת תקינות סטטית. היא מפעילה את
/// תהליך החלוקה על בסיס הנתונים השמורים, ולאחר מכן מעדכנת את רשומת Assignment
/// עם סטטוס, הודעות שגיאה, קבוצות שנוצרו וציוני חלוקה. לכן זו יחידת תיאום
/// חשובה בין המסד לבין האלגוריתם.
///
/// הקלט הוא assignmentId ו-managerId. הפלט אינו מוחזר ישירות כערך מתודה,
/// אלא נשמר בשדות של Assignment כגון ValidationStatus, LastPlacementStatus,
/// LastPlacementGroupsJson, LastInitialPlacementScore ו-LastPlacementScore.
/// לאחר מכן AssignmentDetailLoader קורא את הערכים הללו ומחזיר אותם ללקוח.
/// </remarks>
// מתזמר הרצת חלוקה ושמירת התוצאה במסד
public sealed class AssignmentInitialPlacementValidator
{
    // חיבור למסד לעדכון רשומת החלוקה
    private readonly ApplicationDbContext _db;
    // טוען קלט אלגוריתם מהמסד
    private readonly AssignmentPlacementLoader _loader;
    // מריץ חלוקה ושיפור
    private readonly AssignmentPlacementRunner _placementRunner;

    // בונה את השירות עם שלוש תלויות
    public AssignmentInitialPlacementValidator(
        ApplicationDbContext db,
        AssignmentPlacementLoader loader,
        AssignmentPlacementRunner placementRunner)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _placementRunner = placementRunner ?? throw new ArgumentNullException(nameof(placementRunner));
    }

    /// <summary>
    /// מריץ אימות וחלוקה עבור חלוקה שמורה ומעדכן את תוצאת ההרצה במסד.
    /// </summary>
    /// <remarks>
    /// המתודה מבצעת תחילה בדיקת בעלות כדי למנוע ממנהל להריץ חלוקה שאינה שלו.
    ///  אם ההרצה מצליחה, נשמרות הקבוצות
    /// האחרונות והציונים האחרונים, כדי שהמסך יוכל להציג את התוצאה בלי להריץ
    /// מחדש את האלגוריתם בכל טעינה.
    /// </remarks>
    // מריץ חלוקה ושומר את התוצאה — בלי להחזיר ערך
    public async Task ValidateAsync(
        int assignmentId,
        int managerId,
        CancellationToken cancellationToken = default)
    {
        //  שלב 1: הרשאה — רק מנהל שבבעלותו החלוקה 
        // בודקים שהחלוקה שייכת למנהל
        var belongsToManager = await _loader.AssignmentBelongsToManagerAsync(
            assignmentId,
            managerId,
            cancellationToken);

        // אין הרשאה — יוצאים 
        if (!belongsToManager)
        {
            return;
        }

        // טוענים את רשומת החלוקה לעדכון
        var assignment = await _db.Assignments
            .FirstOrDefaultAsync(entry => entry.AssignmentId == assignmentId, cancellationToken);

        // החלוקה לא קיימת — יוצאים
        if (assignment is null)
        {
            return;
        }

        //  שלב 2: הרצת האלגוריתם 
        try
        {
            // טוענים קלט מהמסד
            var input = await _loader.LoadInputAsync(assignmentId, managerId, cancellationToken);
            // מריצים חלוקה ושיפור
            var runResult = _placementRunner.RunAndImprove(input);
            // מעדכנים את רשומת החלוקה לפי התוצאה
            ApplyResult(assignment, runResult);
        }
        // תופסים שגיאת קלט או נתונים
        catch (ArgumentException ex)
        {
            ApplyFailure(assignment, ex.Message);
        }
        // תופסים שגיאת מצב לא תקין
        catch (InvalidOperationException ex)
        {
            ApplyFailure(assignment, ex.Message);
        }

        // שומרים את כל השינויים למסד
        await _db.SaveChangesAsync(cancellationToken);
    }

    // מעדכן את רשומת החלוקה לפי תוצאת הרצה מוצלחת
    private static void ApplyResult(Data.Models.Assignment assignment, AssignmentPlacementRunResult runResult)
    {
        // תוצאת שלב החלוקה הראשוני
        var result = runResult.PlacementResult;

        // שומרים מתי בוצע אימות אחרון
        assignment.LastValidatedAtUtc = DateTime.UtcNow;
        // שומרים את סטטוס ההרצה
        assignment.LastPlacementStatus = result.Status.ToString();

        // בונים הודעות שגיאה מותאמות למשתמש
        var displayErrors = InitialPlacementUserMessages.ForDisplay(result).ToList();
        // מוסיפים אזהרה אם יש (למשל חזרה לחלוקה ראשונית)
        if (!string.IsNullOrWhiteSpace(runResult.Warning))
        {
            displayErrors.Add(runResult.Warning);
        }

        // שומרים שגיאות כ-JSON
        assignment.LastValidationErrors = SerializeErrors(displayErrors);

        // אם הצליח ויש חלוקה סופית — שומרים קבוצות וציונים
        if (result.Status is InitialPlacementStatus.Success or InitialPlacementStatus.SuccessViaSolver
            && runResult.FinalAssignment is not null)
        {
            assignment.ValidationStatus = "Validated";
            assignment.LastPlacementGroupsJson = PlacementGroupSerialization.SerializeGroups(runResult.FinalAssignment);
            ApplyPlacementScores(assignment, runResult); // שומר ציוני חלוקה ראשונית ושיפור
            return;
        }

        // כשלון — מנקים קבוצות וציונים
        assignment.ValidationStatus = "ValidationFailed";
        assignment.LastPlacementGroupsJson = null;
        ClearPlacementScores(assignment);
    }

    // שומר ציוני חלוקה ראשונית ושיפור
    private static void ApplyPlacementScores(
        Data.Models.Assignment assignment,
        AssignmentPlacementRunResult runResult)
    {
        // אם יש שני הציונים — שומרים אותם
        if (runResult.ImprovementResult?.InitialScore is { } initialScore
            && runResult.ImprovementResult.FinalScore is { } finalScore)
        {
            assignment.LastInitialPlacementScore = initialScore.Value;
            assignment.LastPlacementScore = finalScore.Value;
            return;
        }

        // אין ציוני שיפור — מנקים
        ClearPlacementScores(assignment);
    }

    // מאפס את הציונים השמורים
    private static void ClearPlacementScores(Data.Models.Assignment assignment)
    {
        assignment.LastInitialPlacementScore = null;
        assignment.LastPlacementScore = null;
    }

    // מעדכן את הרשומה במקרה כשלון
    private static void ApplyFailure(Data.Models.Assignment assignment, string error)
    {
        assignment.LastValidatedAtUtc = DateTime.UtcNow;
        assignment.ValidationStatus = "ValidationFailed";
        assignment.LastPlacementStatus = null;
        assignment.LastValidationErrors = SerializeErrors(new[] { error });
        assignment.LastPlacementGroupsJson = null;
        ClearPlacementScores(assignment);
    }

    // ממיר רשימת שגיאות למחרוזת JSON
    private static string? SerializeErrors(IReadOnlyList<string> errors)
    {
        // אין שגיאות — שומרים null
        if (errors.Count == 0)
        {
            return null;
        }

        // ממירים ל-JSON
        return JsonSerializer.Serialize(errors);
    }

}