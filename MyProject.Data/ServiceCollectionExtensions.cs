using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MyProject.Data;

/// <summary>
/// רישום שירותי גישה לנתונים.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// מוסיף את <see cref="ApplicationDbContext"/> עם ספק SQL Server.
    /// </summary>
    /// <param name="services">אוסף השירותים.</param>
    /// <param name="connectionString">מחרוזת חיבור ל־SQL Server.</param>
    public static IServiceCollection AddApplicationPersistence(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }
}
