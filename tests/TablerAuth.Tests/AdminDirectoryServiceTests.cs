using Microsoft.Extensions.DependencyInjection;
using TablerAuth.Domain.Identity;
using TablerAuth.Infrastructure.Identity;

namespace TablerAuth.Tests;

public class AdminDirectoryServiceTests
{
    [Fact]
    public async Task Last_admin_cannot_be_removed()
    {
        await using var provider = IdentityTestHost.Create(nameof(Last_admin_cannot_be_removed));
        await IdentityTestHost.EnsureRolesAsync(provider);
        var onlyAdmin = await IdentityTestHost.CreateUserAsync(
            provider,
            "only-admin@test.local",
            AppRoles.Admin,
            AppRoles.User);

        var result = await provider.GetRequiredService<AdminDirectoryService>().SetAdminRoleAsync(
            onlyAdmin.Id,
            isAdmin: false,
            actingUserId: "someone-else");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("En az bir Admin", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Admin_can_be_removed_when_another_admin_remains()
    {
        await using var provider = IdentityTestHost.Create(nameof(Admin_can_be_removed_when_another_admin_remains));
        await IdentityTestHost.EnsureRolesAsync(provider);
        var first = await IdentityTestHost.CreateUserAsync(provider, "first-admin@test.local", AppRoles.Admin, AppRoles.User);
        var second = await IdentityTestHost.CreateUserAsync(provider, "second-admin@test.local", AppRoles.Admin, AppRoles.User);

        var result = await provider.GetRequiredService<AdminDirectoryService>().SetAdminRoleAsync(
            second.Id,
            isAdmin: false,
            actingUserId: first.Id);

        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
    }
}
