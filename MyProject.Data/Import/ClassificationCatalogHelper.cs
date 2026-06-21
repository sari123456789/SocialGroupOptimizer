using Microsoft.EntityFrameworkCore;
using MyProject.Data.Models;

namespace MyProject.Data.Import;


//תפקיד המחלקה הסטטית: לטעון, למצוא, או ליצור במסד הנתונים ממדי סיווג ו־רמות סיווג בזמן ייבוא נתונים.
public static class ClassificationCatalogHelper
{
    // <summary>
    //טעינת כל ממדי הסיווג מה־DB
    // </summary>
    public static async Task<Dictionary<string, ClassificationDimension>> LoadDimensionLookupAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        var dimensions = await db.ClassificationDimensions.ToListAsync(cancellationToken);
        return BuildDimensionLookup(dimensions);
    }

    //הפונקציה הזאת מקבלת רשימת ממדים, ובונה ממנה Dictionary
    //שממפה את קוד הממד לאובייקט ממד הסיווג עצמו.
    public static Dictionary<string, ClassificationDimension> BuildDimensionLookup(
        IEnumerable<ClassificationDimension> dimensions) =>
        dimensions.ToDictionary(
            //המפתח יהיה קוד הממד, והערך יהיה האובייקט עצמו
            dimension => dimension.DimensionCode,
            dimension => dimension,
            StringComparer.OrdinalIgnoreCase);//החיפוש במילון לא יהיה רגיש לאותיות גדולות/קטנות.

    /// <summary>
    /// מציאת מימד קיים או יצירת מימד חדש
    /// </summary>
    public static async Task<ClassificationDimension> GetOrCreateDimensionAsync(
        ApplicationDbContext db,
        IDictionary<string, ClassificationDimension> dimensionByCode,
        string dimensionCode,
        CancellationToken cancellationToken = default)
    {
        var trimmedCode = dimensionCode.Trim();
        //בדיקה אם המימד כבר קיים במילון לפי קוד המימד
        if (dimensionByCode.TryGetValue(trimmedCode, out var existing))
        {
            return existing;
        }

        //יצירת מימד חדש
        var dimension = new ClassificationDimension { DimensionCode = trimmedCode };
        //הוספת מימד חדש למסד הנתונים
        db.ClassificationDimensions.Add(dimension);
        await db.SaveChangesAsync(cancellationToken);
        dimensionByCode[dimension.DimensionCode] = dimension;
        return dimension;
    }

    public static async Task<int> GetOrCreateLevelIdAsync(
        ApplicationDbContext db,
        IDictionary<(int ClassificationDimensionId, string LevelCode), int> levelIdByDimensionAndCode,
        ClassificationDimension dimension,
        string levelCode,
        CancellationToken cancellationToken = default)
    {
        var trimmedLevel = levelCode.Trim();
        var levelKey = (dimension.ClassificationDimensionId, trimmedLevel);

        if (levelIdByDimensionAndCode.TryGetValue(levelKey, out var existingLevelId))
        {
            return existingLevelId;
        }

        var level = new ClassificationLevel
        {
            ClassificationDimensionId = dimension.ClassificationDimensionId,
            LevelCode = trimmedLevel,
        };
        //הוספת רמה חדשה למסד הנתונים
        db.ClassificationLevels.Add(level);
        await db.SaveChangesAsync(cancellationToken);
        levelIdByDimensionAndCode[levelKey] = level.ClassificationLevelId;
        return level.ClassificationLevelId;
    }
}
