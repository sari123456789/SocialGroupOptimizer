using Microsoft.EntityFrameworkCore;
using MyProject.Data.Models;

namespace MyProject.Data.Import;

public static class ClassificationCatalogHelper
{
    public static async Task<Dictionary<string, ClassificationDimension>> LoadDimensionLookupAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        var dimensions = await db.ClassificationDimensions.ToListAsync(cancellationToken);
        return BuildDimensionLookup(dimensions);
    }

    public static Dictionary<string, ClassificationDimension> BuildDimensionLookup(
        IEnumerable<ClassificationDimension> dimensions) =>
        dimensions.ToDictionary(
            dimension => dimension.DimensionCode,
            dimension => dimension,
            StringComparer.OrdinalIgnoreCase);

    public static async Task<ClassificationDimension> GetOrCreateDimensionAsync(
        ApplicationDbContext db,
        IDictionary<string, ClassificationDimension> dimensionByCode,
        string dimensionCode,
        CancellationToken cancellationToken = default)
    {
        var trimmedCode = dimensionCode.Trim();

        if (dimensionByCode.TryGetValue(trimmedCode, out var existing))
        {
            return existing;
        }

        var dimension = new ClassificationDimension { DimensionCode = trimmedCode };
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
        db.ClassificationLevels.Add(level);
        await db.SaveChangesAsync(cancellationToken);
        levelIdByDimensionAndCode[levelKey] = level.ClassificationLevelId;
        return level.ClassificationLevelId;
    }
}
