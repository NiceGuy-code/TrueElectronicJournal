using Journal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Journal.Data;

// Роли, админ и тестовые данные создаются при старте, миграцией не создаются
public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        foreach (var role in new[] { "Admin", "Teacher", "Student" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminEmail = "admin@test.ru";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Администратор"
            };
            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
            else
                logger.LogError("Не удалось создать админа: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        // Тестовые данные (восстановление после сброса БД)
        foreach (var name in new[] { "ИС-21", "ИС-22" })
            if (!await db.Groups.AnyAsync(g => g.Name == name))
                db.Groups.Add(new Group { Name = name });
        await db.SaveChangesAsync();

        foreach (var name in new[] { "Математика", "Русский язык", "Физика" })
            if (!await db.Subjects.AnyAsync(s => s.Name == name))
                db.Subjects.Add(new Subject { Name = name });
        await db.SaveChangesAsync();

        await CreateUserAsync(userManager, logger, "teacher1@test.ru", "Teacher123!",
            "Иванов Иван Иванович", "Teacher", null);

        var is22 = await db.Groups.FirstOrDefaultAsync(g => g.Name == "ИС-22");
        await CreateUserAsync(userManager, logger, "student1@test.ru", "Student123!",
            "Петров Пётр Петрович", "Student", is22?.Id);
    }

    private static async Task CreateUserAsync(UserManager<ApplicationUser> userManager,
        ILogger logger, string email, string password, string fullName, string role, int? groupId)
    {
        if (await userManager.FindByEmailAsync(email) != null)
            return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            GroupId = role == "Student" ? groupId : null
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, role);
        else
            logger.LogError("Не удалось создать {Email}: {Errors}", email,
                string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
