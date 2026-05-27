using Microsoft.EntityFrameworkCore;
using MyProject.Data.Models;

namespace MyProject.Data;

/// <summary>
/// הקשר למסד הנתונים של האפליקציה (EF Core).
/// </summary>
public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Manager> Managers => Set<Manager>();
    public DbSet<ManagementGroup> ManagementGroups => Set<ManagementGroup>();
    public DbSet<ClassificationDimension> ClassificationDimensions => Set<ClassificationDimension>();
    public DbSet<ClassificationLevel> ClassificationLevels => Set<ClassificationLevel>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<ParticipantAssignment> ParticipantAssignments => Set<ParticipantAssignment>();
    public DbSet<ParticipantClassification> ParticipantClassifications => Set<ParticipantClassification>();
    public DbSet<SocialPreference> SocialPreferences => Set<SocialPreference>();
    public DbSet<GroupSizeConstraint> GroupSizeConstraints => Set<GroupSizeConstraint>();
    public DbSet<GroupCountConstraint> GroupCountConstraints => Set<GroupCountConstraint>();
    public DbSet<MandatoryPairConstraint> MandatoryPairConstraints => Set<MandatoryPairConstraint>();
    public DbSet<ForbiddenPairConstraint> ForbiddenPairConstraints => Set<ForbiddenPairConstraint>();
    public DbSet<AssignmentClassificationConstraint> AssignmentClassificationConstraints => Set<AssignmentClassificationConstraint>();
    public DbSet<BestSolutionArchive> BestSolutionArchives => Set<BestSolutionArchive>();
    public DbSet<AssignmentScoreBreakdown> AssignmentScoreBreakdowns => Set<AssignmentScoreBreakdown>();
    public DbSet<AssignmentConflict> AssignmentConflicts => Set<AssignmentConflict>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Manager>(e =>
        {
            e.HasKey(m => m.ManagerId);
            e.Property(m => m.ManagerName).HasMaxLength(256);
        });

        modelBuilder.Entity<ManagementGroup>(e =>
        {
            e.HasKey(g => g.ManagementGroupId);
            e.Property(g => g.ManagementGroupName).HasMaxLength(256);
            e.HasOne<Manager>()
                .WithMany()
                .HasForeignKey(g => g.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // קטלוג סיווגים: מימד (עמודה) → רמות (ערכי תא). גלובלי — לא פר שיבוץ.
        modelBuilder.Entity<ClassificationDimension>(e =>
        {
            e.HasKey(c => c.ClassificationDimensionId);
            e.Property(c => c.DimensionCode).HasMaxLength(256);
            e.HasIndex(c => c.DimensionCode).IsUnique();
        });

        modelBuilder.Entity<ClassificationLevel>(e =>
        {
            e.HasKey(a => a.ClassificationLevelId);
            e.Property(a => a.LevelCode).HasMaxLength(128);
            e.HasIndex(a => new { a.ClassificationDimensionId, a.LevelCode }).IsUnique();
            e.HasOne<ClassificationDimension>()
                .WithMany()
                .HasForeignKey(a => a.ClassificationDimensionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Participant>(e =>
        {
            e.HasKey(p => p.ParticipantId);//מפתח ראשי — מזהה פנימי במסד.
            e.Property(p => p.ParticipantId).ValueGeneratedOnAdd();//מזהה פנימי במסד מוגדר כמספר סידורי במסד (לא מזהה ליבה).
            e.Property(p => p.IsraeliIdentityNumber).HasMaxLength(9).IsRequired();//מספר זהות ישראלי (תשע ספרות)
            e.Property(p => p.ParticipantName).HasMaxLength(256);
            e.HasIndex(p => p.IsraeliIdentityNumber).IsUnique(); //מונע כפילויות במספר זהות
        });

        modelBuilder.Entity<Assignment>(e =>
        {
            e.HasKey(a => a.AssignmentId);
            e.Property(a => a.AssignmentId).ValueGeneratedOnAdd();
            e.Property(a => a.AssignmentName).HasMaxLength(256);
            e.HasOne<ManagementGroup>()
                .WithMany()
                .HasForeignKey(a => a.ManagementGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ParticipantAssignment>(e =>
        {
            e.HasKey(pa => pa.ParticipantAssignmentId);
            e.Property(pa => pa.ParticipantAssignmentId).ValueGeneratedOnAdd();
            e.HasOne<Participant>()
                .WithMany()
                .HasForeignKey(pa => pa.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Assignment>()
                .WithMany()
                .HasForeignKey(pa => pa.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Manager>()
                .WithMany()
                .HasForeignKey(pa => pa.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // סיווג משתתף בריצה: ערך אחד למימד; מפתח (ParticipantAssignmentId, ClassificationDimensionId).
        modelBuilder.Entity<ParticipantClassification>(e =>
        {
            e.HasKey(pc => new { pc.ParticipantAssignmentId, pc.ClassificationDimensionId });
            e.HasOne<ParticipantAssignment>()
                .WithMany()
                .HasForeignKey(pc => pc.ParticipantAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ClassificationDimension>()
                .WithMany()
                .HasForeignKey(pc => pc.ClassificationDimensionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ClassificationLevel>()
                .WithMany()
                .HasForeignKey(pc => pc.ClassificationLevelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SocialPreference>(e =>
        {
            e.HasKey(sp => new { sp.AssignmentId, sp.FromParticipantAssignmentId, sp.ToParticipantAssignmentId });
            e.HasOne(sp => sp.Assignment)
                .WithMany()
                .HasForeignKey(sp => sp.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(sp => sp.FromParticipantAssignment)
                .WithMany()
                .HasForeignKey(sp => sp.FromParticipantAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(sp => sp.ToParticipantAssignment)
                .WithMany()
                .HasForeignKey(sp => sp.ToParticipantAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GroupSizeConstraint>(e =>
        {
            e.HasKey(x => new { x.AssignmentId, x.GroupId });
            e.HasOne<Assignment>()
                .WithMany()
                .HasForeignKey(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GroupCountConstraint>(e =>
        {
            e.HasKey(x => x.AssignmentId);
            e.HasOne<Assignment>()
                .WithOne()
                .HasForeignKey<GroupCountConstraint>(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MandatoryPairConstraint>(e =>
        {
            e.HasKey(m => m.MandatoryPairConstraintId);
            e.Property(m => m.MandatoryPairConstraintId).ValueGeneratedOnAdd();
            e.HasOne(m => m.Assignment)
                .WithMany()
                .HasForeignKey(m => m.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.FirstParticipantAssignment)
                .WithMany()
                .HasForeignKey(m => m.FirstParticipantAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.SecondParticipantAssignment)
                .WithMany()
                .HasForeignKey(m => m.SecondParticipantAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ForbiddenPairConstraint>(e =>
        {
            e.HasKey(f => f.ForbiddenPairConstraintId);
            e.Property(f => f.ForbiddenPairConstraintId).ValueGeneratedOnAdd();
            e.HasOne(f => f.Assignment)
                .WithMany()
                .HasForeignKey(f => f.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.FirstParticipantAssignment)
                .WithMany()
                .HasForeignKey(f => f.FirstParticipantAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(f => f.SecondParticipantAssignment)
                .WithMany()
                .HasForeignKey(f => f.SecondParticipantAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // אילוץ סיווג לשיבוץ: על איזה מימד חל האילוץ (איזון או הפרדה).
        modelBuilder.Entity<AssignmentClassificationConstraint>(e =>
        {
            e.HasKey(x => new { x.AssignmentId, x.ClassificationDimensionId });
            e.HasOne<Assignment>()
                .WithMany()
                .HasForeignKey(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ClassificationDimension>()
                .WithMany()
                .HasForeignKey(x => x.ClassificationDimensionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BestSolutionArchive>(e =>
        {
            e.HasKey(b => b.RunId);
            e.Property(b => b.SolutionStateBlob).HasMaxLength(8000);
        });

        modelBuilder.Entity<AssignmentScoreBreakdown>(e =>
        {
            e.HasKey(x => x.AssignmentId);
            e.Property(x => x.ScoreExplanation).HasMaxLength(4000);
            e.HasOne<Assignment>()
                .WithOne()
                .HasForeignKey<AssignmentScoreBreakdown>(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssignmentConflict>(e =>
        {
            e.HasKey(c => c.AssignmentConflictId);
            e.Property(c => c.AssignmentConflictId).ValueGeneratedOnAdd();
            e.Property(c => c.ConflictType).HasMaxLength(128);
            e.Property(c => c.Description).HasMaxLength(2000);
            e.Property(c => c.Severity).HasMaxLength(64);
            e.Property(c => c.SystemRecommendation).HasMaxLength(2000);
            e.HasOne<Assignment>()
                .WithMany()
                .HasForeignKey(c => c.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
