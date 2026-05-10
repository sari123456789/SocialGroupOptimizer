namespace MyProject.Data.Models;

/// <summary>
/// משתתף בשכבת הנתונים. מזהה המשתתף הפנימי הוא מספר סידורי במסד (לא מזהה ליבה).
/// </summary>
public class Participant
{
    /// <summary>מפתח ראשי — מזהה פנימי במסד.</summary>
    public int ParticipantId { get; set; }

    /// <summary>מספר זהות ישראלי (תשע ספרות); משמש לבניית <see cref="MyProject.Core.Domain.ValueObjects.ParticipantId"/> בשכבת הליבה.</summary>
    public string IsraeliIdentityNumber { get; set; } = string.Empty;

    public string? ParticipantName { get; set; }
}
