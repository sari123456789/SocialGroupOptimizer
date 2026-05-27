using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyProject.Data;

/// <summary>
/// מפעל ל־design-time (מיגרציות) בלי תלות בפרויקט האתחול.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=MyProjectDb_Dev;Trusted_Connection=True;TrustServerCertificate=True;");
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
