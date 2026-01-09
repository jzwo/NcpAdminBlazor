using Microsoft.EntityFrameworkCore;
using NcpAdminBlazor.Domain.AggregatesModel.RoleAggregate;
using NcpAdminBlazor.Domain.AggregatesModel.UserAggregate;
using NcpAdminBlazor.Infrastructure.Utils;
using NcpAdminBlazor.Shared.Auth;

namespace NcpAdminBlazor.ApiService.Extensions;

public static class ApplicationDbContextSeed
{
    public static async Task SeedAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(ApplicationDbContextSeed));
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        try
        {
            if (!await context.Roles.AnyAsync(cancellationToken))
            {
                var adminRole = new Role("Admin", "system admin role", false);
                adminRole.UpdatePermissions(AppPermissions.GetAllPermissionKeys().ToArray());
                await context.Roles.AddAsync(adminRole, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }

            if (!await context.Users.AnyAsync(cancellationToken))
            {
                var adminRoleId = await context.Roles
                    .Where(r => r.Name == "Admin")
                    .Select(r => r.Id)
                    .FirstAsync(cancellationToken);

                var adminUser = new User(
                    username: "admin",
                    passwordHash: passwordHasher.HashPassword("admin123456"),
                    realName: "system administrator",
                    email: "admin@example.com",
                    phone: "13800000000",
                    assignedRoleIds: new List<RoleId> { adminRoleId });

                await context.Users.AddAsync(adminUser, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}