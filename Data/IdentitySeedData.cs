using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace smartscheduler.Data;

public static class IdentitySeedData
{
    public const string AdminEmail = "admin@smartscheduler.local";
    public const string ManagerEmail = "manager@smartscheduler.local";
    public const string SchedulerEmail = "scheduler@smartscheduler.local";
    private const string DemoPassword = "SmartScheduler!2026";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { AppRoles.Admin, AppRoles.Manager, AppRoles.Scheduler, AppRoles.Employee })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        await EnsureUserAsync(userManager, AdminEmail, AppRoles.Admin);
        await EnsureUserAsync(userManager, ManagerEmail, AppRoles.Manager);
        await EnsureUserAsync(userManager, SchedulerEmail, AppRoles.Scheduler);
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string role)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(item => item.Email == email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            var created = await userManager.CreateAsync(user, DemoPassword);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", created.Errors.Select(error => error.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }
}
