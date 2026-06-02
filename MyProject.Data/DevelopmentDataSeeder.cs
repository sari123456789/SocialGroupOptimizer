using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using MyProject.Data.Models;

namespace MyProject.Data;

/// <summary>
/// נתוני דמו לפיתוח — נטען רק כשאין שיבוצים במסד.
/// </summary>
public static class DevelopmentDataSeeder
{
    public const string DemoManagerName = "מנהל דמו";

    public const string DemoManagerPassword = "demo1234";

    private static readonly string[] DemoIdentityNumbers =
    [
        "200000001",
        "200000002",
        "200000003",
        "200000004",
    ];

    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        await EnsureDemoManagerPasswordAsync(db, cancellationToken);

        if (await db.Assignments.AnyAsync(cancellationToken))
        {
            return;
        }

        var manager = new Manager
        {
            ManagerName = DemoManagerName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoManagerPassword),
        };
        db.Managers.Add(manager);
        await db.SaveChangesAsync(cancellationToken);

        var managementGroup = new ManagementGroup
        {
            ManagerId = manager.ManagerId,
            ManagementGroupName = "קבוצת ניהול דמו",
        };
        db.ManagementGroups.Add(managementGroup);
        await db.SaveChangesAsync(cancellationToken);

        var dimension = new ClassificationDimension { DimensionCode = "level" };
        db.ClassificationDimensions.Add(dimension);
        await db.SaveChangesAsync(cancellationToken);

        var level = new ClassificationLevel
        {
            ClassificationDimensionId = dimension.ClassificationDimensionId,
            LevelCode = "default",
        };
        db.ClassificationLevels.Add(level);
        await db.SaveChangesAsync(cancellationToken);

        var participants = DemoIdentityNumbers
            .Select(identityNumber => new Participant { IsraeliIdentityNumber = identityNumber })
            .ToList();
        db.Participants.AddRange(participants);
        await db.SaveChangesAsync(cancellationToken);

        var assignment = new Assignment
        {
            AssignmentName = "שיבוץ דמו",
            ManagementGroupId = managementGroup.ManagementGroupId,
        };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);

        var participantAssignments = participants
            .Select(participant => new ParticipantAssignment
            {
                ParticipantId = participant.ParticipantId,
                AssignmentId = assignment.AssignmentId,
                ManagerId = manager.ManagerId,
            })
            .ToList();
        db.ParticipantAssignments.AddRange(participantAssignments);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var participantAssignment in participantAssignments)
        {
            db.ParticipantClassifications.Add(new ParticipantClassification
            {
                ParticipantAssignmentId = participantAssignment.ParticipantAssignmentId,
                ClassificationDimensionId = dimension.ClassificationDimensionId,
                ClassificationLevelId = level.ClassificationLevelId,
            });
        }

        db.GroupCountConstraints.Add(new GroupCountConstraint
        {
            AssignmentId = assignment.AssignmentId,
            MinGroups = 2,
            MaxGroups = 2,
        });

        db.GroupSizeConstraints.AddRange(
            new GroupSizeConstraint
            {
                AssignmentId = assignment.AssignmentId,
                GroupId = 1,
                MinGroupSize = 2,
                MaxGroupSize = 2,
            },
            new GroupSizeConstraint
            {
                AssignmentId = assignment.AssignmentId,
                GroupId = 2,
                MinGroupSize = 2,
                MaxGroupSize = 2,
            });

        var assignmentByIdentity = participantAssignments
            .Join(
                participants,
                participantAssignment => participantAssignment.ParticipantId,
                participant => participant.ParticipantId,
                (participantAssignment, participant) => new
                {
                    participant.IsraeliIdentityNumber,
                    participantAssignment.ParticipantAssignmentId,
                })
            .ToDictionary(entry => entry.IsraeliIdentityNumber, entry => entry.ParticipantAssignmentId);

        db.MandatoryPairConstraints.Add(new MandatoryPairConstraint
        {
            AssignmentId = assignment.AssignmentId,
            FirstParticipantAssignmentId = assignmentByIdentity["200000001"],
            SecondParticipantAssignmentId = assignmentByIdentity["200000002"],
        });

        db.ForbiddenPairConstraints.Add(new ForbiddenPairConstraint
        {
            AssignmentId = assignment.AssignmentId,
            FirstParticipantAssignmentId = assignmentByIdentity["200000001"],
            SecondParticipantAssignmentId = assignmentByIdentity["200000003"],
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task EnsureDemoManagerPasswordAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        var demoManager = await db.Managers
            .FirstOrDefaultAsync(
                manager => manager.ManagerName == DemoManagerName,
                cancellationToken);

        if (demoManager is null || !string.IsNullOrWhiteSpace(demoManager.PasswordHash))
        {
            return;
        }

        demoManager.PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoManagerPassword);
        await db.SaveChangesAsync(cancellationToken);
    }
}
