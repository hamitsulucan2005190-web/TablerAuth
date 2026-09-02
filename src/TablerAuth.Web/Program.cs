using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using TablerAuth.Domain.Entities;
using TablerAuth.Infrastructure;
using TablerAuth.Infrastructure.Data;
using TablerAuth.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment() && OperatingSystem.IsMacOS())
{
    // Safari on macOS often fails Kestrel's HTTP/2 TLS handshake
    // ("can't establish a secure connection") while Chrome still works.
    builder.WebHost.ConfigureKestrel(options =>
        options.ConfigureEndpointDefaults(listenOptions =>
            listenOptions.Protocols = HttpProtocols.Http1));
}

builder.Services.AddControllersWithViews();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    await DataSeeder.SeedAsync(
        scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(),
        scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
        scope.ServiceProvider.GetRequiredService<IConfiguration>(),
        app.Logger);

    await IdentityProviderSeeder.SeedAsync(
        db,
        scope.ServiceProvider.GetRequiredService<IConfiguration>(),
        app.Logger);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
