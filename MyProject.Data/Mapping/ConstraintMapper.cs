using System;
using System.Collections.Generic;
using System.Linq;
using MyProject.Core.Domain.Constraints;
using MyProject.Core.Domain.ValueObjects;
using MyProject.Data.Models;
using CoreForbiddenPairConstraint = MyProject.Core.Domain.Constraints.ForbiddenPairConstraint;
using CoreGroupCountConstraint = MyProject.Core.Domain.Constraints.GroupCountConstraint;
using CoreGroupSizeConstraint = MyProject.Core.Domain.Constraints.GroupSizeConstraint;
using CoreMandatoryPairConstraint = MyProject.Core.Domain.Constraints.MandatoryPairConstraint;
using DataForbiddenPairConstraint = MyProject.Data.Models.ForbiddenPairConstraint;
using DataGroupCountConstraint = MyProject.Data.Models.GroupCountConstraint;
using DataGroupSizeConstraint = MyProject.Data.Models.GroupSizeConstraint;
using DataMandatoryPairConstraint = MyProject.Data.Models.MandatoryPairConstraint;

namespace MyProject.Data.Mapping;

/// <summary>
/// מיפוי מפורש משורות נתונים לאילוצי ליבה.
/// </summary>
public static class ConstraintMapper
{
    public static IReadOnlyList<CoreMandatoryPairConstraint> MapMandatoryPairs(
        IEnumerable<DataMandatoryPairConstraint> rows,
        ParticipantAssignmentContext context,
        IReadOnlyDictionary<int, ParticipantId> participantIdentityByDbParticipantId,
        int assignmentDbId)
    {
        ValidatePairMappingInputs(rows, context, participantIdentityByDbParticipantId);

        return rows
            .Where(row => row.AssignmentId == assignmentDbId)
            .Select(row => MapMandatoryPair(row, context, participantIdentityByDbParticipantId))
            .ToList();
    }

    public static IReadOnlyList<CoreForbiddenPairConstraint> MapForbiddenPairs(
        IEnumerable<DataForbiddenPairConstraint> rows,
        ParticipantAssignmentContext context,
        IReadOnlyDictionary<int, ParticipantId> participantIdentityByDbParticipantId,
        int assignmentDbId)
    {
        ValidatePairMappingInputs(rows, context, participantIdentityByDbParticipantId);

        return rows
            .Where(row => row.AssignmentId == assignmentDbId)
            .Select(row => MapForbiddenPair(row, context, participantIdentityByDbParticipantId))
            .ToList();
    }

    public static IReadOnlyList<CoreGroupSizeConstraint> MapGroupSizeConstraints(
        IEnumerable<DataGroupSizeConstraint> rows,
        int assignmentDbId)
    {
        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        return rows
            .Where(row => row.AssignmentId == assignmentDbId)
            .Select(row => new CoreGroupSizeConstraint(
                new GroupId(row.GroupId),
                row.MinGroupSize,
                new GroupCapacity(row.MaxGroupSize)))
            .ToList();
    }

    public static CoreGroupCountConstraint? MapGroupCountConstraint(
        IEnumerable<DataGroupCountConstraint> rows,
        int assignmentDbId)
    {
        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        var row = rows.FirstOrDefault(r => r.AssignmentId == assignmentDbId);
        return row is null
            ? null
            : new CoreGroupCountConstraint(row.MinGroups, row.MaxGroups);
    }

    /// <summary>
    /// ממפה אילוצי סיווג של שיבוץ לאילוצי ליבה (איזון יחסי או הפרדה הומוגנית).
    /// </summary>
    /// <remarks>
    /// <list type="number">
    /// <item>בונה מפה: כל משתתף (זהות) → מילון מימד→רמה — <see cref="BuildParticipantClassificationsMap"/>.</item>
    /// <item>לכל שורה ב־<c>AssignmentClassificationConstraint</c> של השיבוץ: מימד מטרה + כל הרמות במימד.</item>
    /// <item><c>IsBalanceOrSeparation</c> true = ProportionalBalance; false = HomogeneousGroup.</item>
    /// </list>
    /// </remarks>
    public static IReadOnlyList<IConstraint> MapClassificationConstraints(
        IEnumerable<AssignmentClassificationConstraint> rows,
        ClassificationCatalog catalog,
        IEnumerable<ParticipantClassification> participantClassifications,
        ParticipantAssignmentContext context,
        IReadOnlyDictionary<int, ParticipantId> participantIdentityByDbParticipantId,
        int assignmentDbId)
    {
        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        if (catalog is null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        if (participantClassifications is null)
        {
            throw new ArgumentNullException(nameof(participantClassifications));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (participantIdentityByDbParticipantId is null)
        {
            throw new ArgumentNullException(nameof(participantIdentityByDbParticipantId));
        }

        var participantClassificationMap = BuildParticipantClassificationsMap(
            participantClassifications,
            catalog,
            context,
            participantIdentityByDbParticipantId,
            assignmentDbId);

        var constraints = new List<IConstraint>();

        foreach (var row in rows.Where(r => r.AssignmentId == assignmentDbId))
        {
            // שורה אחת = אילוץ על מימד אחד (למשל רק "רמה", לא "מגדר").
            var targetDimension = catalog.GetDimensionCode(row.ClassificationDimensionId);
            var dimensionLevels = catalog.GetLevelCodesForDimension(row.ClassificationDimensionId);

            if (dimensionLevels.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Missing classification levels for ClassificationDimensionId {row.ClassificationDimensionId}.");
            }

            constraints.Add(row.IsBalanceOrSeparation
                ? new ClassificationProportionalBalanceConstraint(targetDimension, dimensionLevels, participantClassificationMap)
                : new ClassificationHomogeneousGroupConstraint(targetDimension, dimensionLevels, participantClassificationMap));
        }

        return constraints;
    }

    private static void ValidatePairMappingInputs(
        object rows,
        ParticipantAssignmentContext context,
        IReadOnlyDictionary<int, ParticipantId> participantIdentityByDbParticipantId)
    {
        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (participantIdentityByDbParticipantId is null)
        {
            throw new ArgumentNullException(nameof(participantIdentityByDbParticipantId));
        }
    }

    private static CoreMandatoryPairConstraint MapMandatoryPair(
        DataMandatoryPairConstraint row,
        ParticipantAssignmentContext context,
        IReadOnlyDictionary<int, ParticipantId> participantIdentityByDbParticipantId)
    {
        var participantA = ResolveParticipantId(row.FirstParticipantAssignmentId, context, participantIdentityByDbParticipantId);
        var participantB = ResolveParticipantId(row.SecondParticipantAssignmentId, context, participantIdentityByDbParticipantId);
        return new CoreMandatoryPairConstraint(participantA, participantB);
    }

    private static CoreForbiddenPairConstraint MapForbiddenPair(
        DataForbiddenPairConstraint row,
        ParticipantAssignmentContext context,
        IReadOnlyDictionary<int, ParticipantId> participantIdentityByDbParticipantId)
    {
        var participantA = ResolveParticipantId(row.FirstParticipantAssignmentId, context, participantIdentityByDbParticipantId);
        var participantB = ResolveParticipantId(row.SecondParticipantAssignmentId, context, participantIdentityByDbParticipantId);
        return new CoreForbiddenPairConstraint(participantA, participantB);
    }

    private static ParticipantId ResolveParticipantId(
        int participantAssignmentId,
        ParticipantAssignmentContext context,
        IReadOnlyDictionary<int, ParticipantId> participantIdentityByDbParticipantId)
    {
        var dbParticipantId = context.GetDbParticipantId(participantAssignmentId);
        if (!participantIdentityByDbParticipantId.TryGetValue(dbParticipantId, out var participantId))
        {
            throw new InvalidOperationException($"Missing domain participant identity for DbParticipantId {dbParticipantId}.");
        }

        return participantId;
    }

    /// <summary>
    /// מפה מרכזת לכל משתתפי השיבוץ: זהות → (מימד → רמה). משמש את אילוצי הסיווג בליבה.
    /// </summary>
    private static IReadOnlyDictionary<ParticipantId, IReadOnlyDictionary<ClassificationDimensionCode, ClassificationLevelCode>> BuildParticipantClassificationsMap(
        IEnumerable<ParticipantClassification> participantClassifications,
        ClassificationCatalog catalog,
        ParticipantAssignmentContext context,
        IReadOnlyDictionary<int, ParticipantId> participantIdentityByDbParticipantId,
        int assignmentDbId)
    {
        var classificationsByParticipant = new Dictionary<ParticipantId, Dictionary<ClassificationDimensionCode, ClassificationLevelCode>>();

        foreach (var (dbParticipantId, participantId) in participantIdentityByDbParticipantId)
        {
            var participantAssignmentId = context.GetParticipantAssignmentId(dbParticipantId, assignmentDbId);
            var map = new Dictionary<ClassificationDimensionCode, ClassificationLevelCode>();

            // רק שורות הסיווג של אותו משתתף באותה ריצת שיבוץ.
            foreach (var row in participantClassifications.Where(r => r.ParticipantAssignmentId == participantAssignmentId))
            {
                var (dimension, level) = catalog.ResolveParticipantClassification(row);
                if (!map.TryAdd(dimension, level))
                {
                    throw new InvalidOperationException(
                        $"Duplicate classification dimension '{dimension}' for participant {participantId}.");
                }
            }

            if (map.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Participant with DbParticipantId {dbParticipantId} must have at least one mapped classification.");
            }

            classificationsByParticipant[participantId] = map;
        }

        return classificationsByParticipant.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlyDictionary<ClassificationDimensionCode, ClassificationLevelCode>)entry.Value);
    }
}
