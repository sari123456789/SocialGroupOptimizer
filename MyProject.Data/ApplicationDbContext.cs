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
    public DbSet<Classification> Classifications => Set<Classification>();
    public DbSet<ClassificationAttribute> ClassificationAttributes => Set<ClassificationAttribute>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<ParticipantAssignment> ParticipantAssignments => Set<ParticipantAssignment>();
    public DbSet<ParticipantClassification> ParticipantClassifications => Set<ParticipantClassification>();
    public DbSet<SocialPreference> SocialPreferences => Set<SocialPreference>();
    public DbSet<GroupSizeConstraint> GroupSizeConstraints => Set<GroupSizeConstraint>();
    public DbSet<GroupCountConstraint> GroupCountConstraints => Set<GroupCountConstraint>();
    public DbSet<MandatoryConstraint> MandatoryConstraints => Set<MandatoryConstraint>();
    public DbSet<MandatoryConstraintAssignment> MandatoryConstraintAssignments => Set<MandatoryConstraintAssignment>();
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

        modelBuilder.Entity<Classification>(e =>
        {
            e.HasKey(c => c.ClassificationId);
            e.Property(c => c.ClassificationName).HasMaxLength(256);
        });

        modelBuilder.Entity<ClassificationAttribute>(e =>
        {
            e.HasKey(a => a.ClassificationAttributeId);
            e.Property(a => a.AttributeName).HasMaxLength(128);
            e.HasIndex(a => new { a.ClassificationId, a.AttributeName }).IsUnique();
            e.HasOne<Classification>()
                .WithMany()
                .HasForeignKey(a => a.ClassificationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Participant>(e =>
        {
            e.HasKey(p => p.ParticipantId);
            e.Property(p => p.ParticipantId).ValueGeneratedOnAdd();
            e.Property(p => p.IsraeliIdentityNumber).HasMaxLength(9).IsRequired();
            e.Property(p => p.ParticipantName).HasMaxLength(256);
            e.HasIndex(p => p.IsraeliIdentityNumber).IsUnique();
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

        modelBuilder.Entity<ParticipantClassification>(e =>
        {
            e.HasKey(pc => new { pc.ParticipantId, pc.ClassificationAttributeId });
            e.HasOne<Participant>()
                .WithMany()
                .HasForeignKey(pc => pc.ParticipantId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ClassificationAttribute>()
                .WithMany()
                .HasForeignKey(pc => pc.ClassificationAttributeId)
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

        modelBuilder.Entity<MandatoryConstraint>(e =>
        {
            e.HasKey(m => m.MandatoryConstraintId);
            e.Property(m => m.MandatoryConstraintId).ValueGeneratedOnAdd();
            e.Property(m => m.ConstraintName).HasMaxLength(256);
            e.HasOne<Assignment>()
                .WithMany()
                .HasForeignKey(m => m.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MandatoryConstraintAssignment>(e =>
        {
            e.HasKey(x => new { x.MandatoryConstraintId, x.ParticipantAssignmentId });
            e.HasOne<MandatoryConstraint>()
                .WithMany()
                .HasForeignKey(x => x.MandatoryConstraintId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ParticipantAssignment>()
                .WithMany()
                .HasForeignKey(x => x.ParticipantAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssignmentClassificationConstraint>(e =>
        {
            e.HasKey(x => new { x.AssignmentId, x.ClassificationId });
            e.HasOne<Assignment>()
                .WithMany()
                .HasForeignKey(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Classification>()
                .WithMany()
                .HasForeignKey(x => x.ClassificationId)
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
