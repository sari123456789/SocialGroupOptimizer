using Microsoft.EntityFrameworkCore;
using MyProject.BL.Algorithm.InitialPlacement;
using MyProject.BL.Logic.Configuration;
using MyProject.Core.Domain.Constraints;
using MyProject.Data;
using MyProject.Data.Mapping;
using InitialPlacementExecutionContext = MyProject.BL.Algorithm.InitialPlacement.Runtime.ExecutionContext;

namespace MyProject.API.Placement;

/// <summary>
/// טוען נתוני חלוקה מהמסד וממירם ל-<see cref="InitialPlacementInput"/> לשימוש האלגוריתם.
/// </summary>
/// <remarks>
/// <para>שכבת API זו היא "גשר" בין Data (EF models) ל-BL (Core domain):</para>
/// <list type="bullet">
///   <item><description>ParticipantMapper — DB → Core.Participant</description></item>
///   <item><description>ConstraintMapper — DB → Core IConstraint</description></item>
/// </list>
/// </remarks>
public sealed class AssignmentPlacementLoader
{
    private readonly ApplicationDbContext _db;
    private readonly AlgorithmSettings _algorithmSettings;

    public AssignmentPlacementLoader(ApplicationDbContext db, AlgorithmSettings algorithmSettings)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _algorithmSettings = algorithmSettings ?? throw new ArgumentNullException(nameof(algorithmSettings));
    }

    /// <summary>
    /// רשימת חלוקות של מנהל — רק חלוקות בקבוצות ניהול ששייכות לו.
    /// </summary>
    public async Task<IReadOnlyList<AssignmentSummaryDto>> ListAssignmentsAsync(
        int managerId,
        CancellationToken cancellationToken = default)
    {
        // Where + Any — שאילתה: חלוקות ש-ManagementGroup.ManagerId == managerId.
        // Select anonymous → AssignmentSummaryDto — EF מתרגם ל-SQL.
        return await _db.Assignments
            .Where(assignment => _db.ManagementGroups
                .Any(group =>
                    group.ManagementGroupId == assignment.ManagementGroupId
                    && group.ManagerId == managerId))
            .OrderByDescending(assignment => assignment.AssignmentId)
            .Select(assignment => new AssignmentSummaryDto
            {
                AssignmentId = assignment.AssignmentId,
                AssignmentName = assignment.AssignmentName,
                ParticipantCount = _db.ParticipantAssignments.Count(
                    participantAssignment => participantAssignment.AssignmentId == assignment.AssignmentId),
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// בודק האם חלוקה שייכת למנהל — בסיס לכל פעולת הרשאה.
    /// </summary>
    public async Task<bool> AssignmentBelongsToManagerAsync(
        int assignmentId,
        int managerId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Assignments
            .AnyAsync(
                assignment => assignment.AssignmentId == assignmentId
                    && _db.ManagementGroups.Any(group =>
                        group.ManagementGroupId == assignment.ManagementGroupId
                        && group.ManagerId == managerId),
                cancellationToken);
    }

    /// <summary>
    /// בונה קלט אלגוריתם מלא מחלוקה במסד.
    /// </summary>
    public async Task<InitialPlacementInput> LoadInputAsync(
        int assignmentId,
        int managerId,
        CancellationToken cancellationToken = default)
    {
        var belongsToManager = await AssignmentBelongsToManagerAsync(
            assignmentId,
            managerId,
            cancellationToken);

        if (!belongsToManager)
        {
            // ArgumentException — הבקר יתפוס ויחזיר 400/404.
            throw new ArgumentException($"Assignment {assignmentId} was not found.");
        }

        // ===== שלב 1: טעינת משתתפים וקשרים =====
        var participantAssignments = await _db.ParticipantAssignments
            .Where(participantAssignment => participantAssignment.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        if (participantAssignments.Count == 0)
        {
            throw new ArgumentException($"Assignment {assignmentId} has no participants.");
        }

        var dbParticipantIds = participantAssignments
            .Select(participantAssignment => participantAssignment.ParticipantId)
            .Distinct()
            .ToList();

        var participantAssignmentIds = participantAssignments
            .Select(participantAssignment => participantAssignment.ParticipantAssignmentId)
            .ToList();

        var participants = await _db.Participants
            .Where(participant => dbParticipantIds.Contains(participant.ParticipantId))
            .ToListAsync(cancellationToken);

        if (participants.Count != dbParticipantIds.Count)
        {
            throw new InvalidOperationException("Some participants referenced by the assignment are missing in the database.");
        }

        // ===== שלב 2: טעינת סיווגים, העדפות, אילוצים =====
        var participantClassifications = await _db.ParticipantClassifications
            .Where(classification => participantAssignmentIds.Contains(classification.ParticipantAssignmentId))
            .ToListAsync(cancellationToken);

        // SocialPreferences — נטענים לצורך האלגוריתם; לא נשלחים ל-UI.
        var socialPreferences = await _db.SocialPreferences
            .Where(preference => preference.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        var mandatoryPairs = await _db.MandatoryPairConstraints
            .Where(pair => pair.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        var forbiddenPairs = await _db.ForbiddenPairConstraints
            .Where(pair => pair.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        var groupSizeConstraints = await _db.GroupSizeConstraints
            .Where(constraint => constraint.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        var groupCountConstraints = await _db.GroupCountConstraints
            .Where(constraint => constraint.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        var classificationConstraintRows = await _db.AssignmentClassificationConstraints
            .Where(constraint => constraint.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        // ===== שלב 3: קטalog סיווגים (מימדים + רמות) =====
        var dimensions = await _db.ClassificationDimensions.ToListAsync(cancellationToken);
        var levels = await _db.ClassificationLevels.ToListAsync(cancellationToken);
        var catalog = new ClassificationCatalog(dimensions, levels);
        var context = new ParticipantAssignmentContext(participantAssignments);
        var identityLookup = ParticipantMapper.CreateParticipantIdentityLookup(participants);

        // ===== שלב 4: מיפוי DB → Core.Participant =====
        var coreParticipants = participants
            .Select(participant => ParticipantMapper.MapToCoreParticipant(
                participant,
                participantClassifications,
                catalog,
                socialPreferences,
                context,
                identityLookup,
                assignmentId))
            .ToList();

        // ===== שלב 5: מיפוי DB → Core IConstraint =====
        var constraints = new List<IConstraint>();

        var groupCount = ConstraintMapper.MapGroupCountConstraint(groupCountConstraints, assignmentId);
        if (groupCount is not null)
        {
            constraints.Add(groupCount);
        }

        constraints.AddRange(ConstraintMapper.MapGroupSizeConstraints(groupSizeConstraints, assignmentId));
        constraints.AddRange(ConstraintMapper.MapMandatoryPairs(mandatoryPairs, context, identityLookup, assignmentId));
        constraints.AddRange(ConstraintMapper.MapForbiddenPairs(forbiddenPairs, context, identityLookup, assignmentId));
        var maxScaledDeviation = _algorithmSettings.ComputeMaxScaledDeviation(coreParticipants.Count);
        constraints.AddRange(ConstraintMapper.MapClassificationConstraints(
            classificationConstraintRows,
            catalog,
            participantClassifications,
            context,
            identityLookup,
            assignmentId,
            coreParticipants.Count,
            maxScaledDeviation));

        if (constraints.Count == 0)
        {
            throw new ArgumentException($"Assignment {assignmentId} has no placement constraints.");
        }

        // ExecutionContext — מזהה ריצה + seed לרפרודוסibilità (אם האלגוריתם משתמש באקראיות).
        var executionContext = new InitialPlacementExecutionContext(
            MyProject.Core.Domain.ValueObjects.AlgorithmRunId.New(),
            Environment.TickCount);

        return new InitialPlacementInput(executionContext, coreParticipants, constraints);
    }
}
