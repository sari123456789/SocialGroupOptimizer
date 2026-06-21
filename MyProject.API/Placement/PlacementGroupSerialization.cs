using System.Text.Json;
using MyProject.Core.Domain.Entities;
using MyProject.Core.Domain.ValueObjects;

namespace MyProject.API.Placement;

/// <summary>
/// סריאליזציה של קבוצות חלוקה ל-JSON במסד.
/// </summary>
// עוזר לשמירה ושחזור קבוצות כ-JSON
internal static class PlacementGroupSerialization
{
    // ממיר חלוקה למחרוזת JSON לשמירה במסד
    public static string SerializeGroups(Assignment assignment)
    {
        // בודקים שלא קיבלנו חלוקה ריקה
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        // בונים רשימת קבוצות לסריאליזציה
        var groups = assignment.Groups
            // מסדרים לפי מזהה קבוצה לסדר יציב
            .OrderBy(group => group.Id.Value)
            // ממירים כל קבוצה למבנה לשמירה
            .Select(group => new StoredPlacementGroup
            {
                // מזהה הקבוצה
                GroupId = group.Id.Value,
                // רשימת מזהי משתתפים כמחרוזות
                ParticipantIds = group.ParticipantIds
                    .Select(participantId => participantId.Value)
                    .ToList(),
            })
            .ToList();

        // מחזירים JSON
        return JsonSerializer.Serialize(groups);
    }

    /// <summary>
    /// משחזר חלוקה (Core) מתוך JSON שמור — קריאה בלבד, ללא שינוי מצב.
    /// </summary>
    /// <param name="json">JSON של הקבוצות כפי שנשמר ב-<c>LastPlacementGroupsJson</c>.</param>
    /// <returns>חלוקת דומיין משוחזרת, או null אם אין נתונים תקפים.</returns>
    // משחזר חלוקה מ-JSON או מחזיר null
    public static Assignment? DeserializeToAssignment(string? json)
    {
        // אין תוכן — אין מה לשחזר
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        // רשימת קבוצות אחרי דסריאליזציה
        List<StoredPlacementGroup>? stored;
        // מנסים לפרסר את ה-JSON
        try
        {
            stored = JsonSerializer.Deserialize<List<StoredPlacementGroup>>(json);
        }
        catch (JsonException)
        {
            // JSON לא תקין — מחזירים null
            return null;
        }

        // אין קבוצות — מחזירים null
        if (stored is null || stored.Count == 0)
        {
            return null;
        }

        // בונים רשימת קבוצות בדומיין
        var groups = new List<Group>();
        // עוברים על כל קבוצה שנשמרה
        foreach (var entry in stored)
        {
            // מדלגים על קבוצה בלי משתתפים
            if (entry.ParticipantIds.Count == 0)
            {
                continue;
            }

            // ממירים מחרוזות למזהי משתתף
            var participantIds = entry.ParticipantIds
                .Select(value => new ParticipantId(value))
                .ToList();

            // מוסיפים קבוצה לרשימה
            groups.Add(new Group(new GroupId(entry.GroupId), participantIds));
        }

        // אחרי סינון לא נשארו קבוצות
        if (groups.Count == 0)
        {
            return null;
        }

        // בונים חלוקה מהקבוצות
        return new Assignment(groups);
    }

    // מבנה פנימי לייצוג קבוצה ב-JSON
    private sealed class StoredPlacementGroup
    {
        // מזהה הקבוצה
        public int GroupId { get; set; }

        // משתתפים כמחרוזות
        public List<string> ParticipantIds { get; set; } = new();
    }
}