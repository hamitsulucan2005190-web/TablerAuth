using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TablerAuth.Domain.Entities;
using TablerAuth.Domain.Identity;
using TablerAuth.Infrastructure.Auth;
using TablerAuth.Infrastructure.Data;
using TablerAuth.Infrastructure.Identity;

namespace TablerAuth.Tests;

internal static class IdentityTestHost
{
    public static ServiceProvider Create(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddHttpContextAccessor();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(options =>
        {
            options.Issuer = "TablerAuth.Test";
            options.Audience = "TablerAuth.Api.Test";
            options.AccessTokenMinutes = 15;
            options.RefreshTokenDays = 7;
            options.SigningKey = new string('k', 64);
        });

        services.AddScoped<TokenService>();
        services.AddScoped<AdminDirectoryService>();

        return services.BuildServiceProvider();
    }

    public static async Task EnsureRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var name in new[] { AppRoles.Admin, AppRoles.User })
        {
            if (!await roleManager.RoleExistsAsync(name))
            {
                await roleManager.CreateAsync(new IdentityRole(name));
            }
        }
    }

    public static async Task<ApplicationUser> CreateUserAsync(
        IServiceProvider services,
        string email,
        params string[] roles)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = email
        };

        var created = await userManager.CreateAsync(user, "Passw0rd");
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(error => error.Description)));

        foreach (var role in roles)
        {
            var added = await userManager.AddToRoleAsync(user, role);
            Assert.True(added.Succeeded, string.Join("; ", added.Errors.Select(error => error.Description)));
        }

        return user;
    }
}
