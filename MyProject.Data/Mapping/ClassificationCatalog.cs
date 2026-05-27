using System;
using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.ValueObjects;
using MyProject.Data.Models;

namespace MyProject.Data.Mapping;

/// <summary>
/// קטלוג מימדים ורמות מהמסד — מתרגם מזהי int לקודי ליבה (מחרוזות).
/// </summary>
/// <remarks>
/// <para>האלגוריתם ב־Core לא מכיר מפתחות מסד; המיפוי קורה כאן בלבד.</para>
/// <para>טוענים פעם אחת את כל ה־<c>ClassificationDimensions</c> וה־<c>ClassificationLevels</c> ומעבירים לבנאי.</para>
/// </remarks>
public sealed class ClassificationCatalog
{
    private readonly IReadOnlyDictionary<int, ClassificationDimension> _dimensionById;
    private readonly IReadOnlyDictionary<int, ClassificationLevel> _levelById;

    public ClassificationCatalog(
        IEnumerable<ClassificationDimension> dimensions,
        IEnumerable<ClassificationLevel> levels)
    {
        if (dimensions is null)
        {
            throw new ArgumentNullException(nameof(dimensions));
        }

        if (levels is null)
        {
            throw new ArgumentNullException(nameof(levels));
        }

        _dimensionById = dimensions.ToDictionary(d => d.ClassificationDimensionId);
        _levelById = levels.ToDictionary(l => l.ClassificationLevelId);

        // כל רמה חייבת להצביע על מימד קיים בקטלוג.
        foreach (var level in _levelById.Values)
        {
            if (!_dimensionById.ContainsKey(level.ClassificationDimensionId))
            {
                throw new InvalidOperationException(
                    $"ClassificationLevel {level.ClassificationLevelId} references missing dimension {level.ClassificationDimensionId}.");
            }
        }
    }

    /// <summary>מזהה מסד של מימד → <see cref="ClassificationDimensionCode"/> (שם העמודה / DimensionCode).</summary>
    public ClassificationDimensionCode GetDimensionCode(int classificationDimensionId)
    {
        if (!_dimensionById.TryGetValue(classificationDimensionId, out var dimension))
        {
            throw new InvalidOperationException($"Missing ClassificationDimension id {classificationDimensionId}.");
        }

        return new ClassificationDimensionCode(dimension.DimensionCode);
    }

    /// <summary>מזהה מסד של רמה → <see cref="ClassificationLevelCode"/> (תוכן התא / LevelCode).</summary>
    public ClassificationLevelCode GetLevelCode(int classificationLevelId)
    {
        if (!_levelById.TryGetValue(classificationLevelId, out var level))
        {
            throw new InvalidOperationException($"Missing ClassificationLevel id {classificationLevelId}.");
        }

        return new ClassificationLevelCode(level.LevelCode);
    }

    /// <summary>כל הרמות האפשריות במימד — משמש לבניית אילוץ סיווג (איזון או הפרדה).</summary>
    public IReadOnlyList<ClassificationLevelCode> GetLevelCodesForDimension(int classificationDimensionId)
    {
        if (!_dimensionById.ContainsKey(classificationDimensionId))
        {
            throw new InvalidOperationException($"Missing ClassificationDimension id {classificationDimensionId}.");
        }

        return _levelById.Values
            .Where(l => l.ClassificationDimensionId == classificationDimensionId)
            .Select(l => new ClassificationLevelCode(l.LevelCode))
            .ToList();
    }

    /// <summary>
    /// מתרגם שורת <see cref="ParticipantClassification"/> לזוג (מימד, רמה) בליבה.
    /// </summary>
    /// <remarks>מוודא שהרמה שייכת לאותו מימד שמופיע בשורה — מונע נתונים סותרים במסד.</remarks>
    public (ClassificationDimensionCode Dimension, ClassificationLevelCode Level) ResolveParticipantClassification(
        ParticipantClassification row)
    {
        if (!_levelById.TryGetValue(row.ClassificationLevelId, out var level))
        {
            throw new InvalidOperationException($"Missing ClassificationLevel id {row.ClassificationLevelId}.");
        }

        if (level.ClassificationDimensionId != row.ClassificationDimensionId)
        {
            throw new InvalidOperationException(
                $"ParticipantClassification level {row.ClassificationLevelId} does not belong to dimension {row.ClassificationDimensionId}.");
        }

        return (GetDimensionCode(row.ClassificationDimensionId), GetLevelCode(row.ClassificationLevelId));
    }
}
