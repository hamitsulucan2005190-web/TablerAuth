using System.Text;
using AspNet.Security.OAuth.GitHub;
using AspNet.Security.OAuth.LinkedIn;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TablerAuth.Application.Admin;
using TablerAuth.Application.Auth;
using TablerAuth.Application.Email;
using TablerAuth.Application.IdentityProviders;
using TablerAuth.Application.Tokens;
using TablerAuth.Domain.Entities;
using TablerAuth.Domain.Identity;
using TablerAuth.Infrastructure.Auth;
using TablerAuth.Infrastructure.Auth.Dynamic;
using TablerAuth.Infrastructure.Data;
using TablerAuth.Infrastructure.Email;
using TablerAuth.Infrastructure.Identity;

namespace TablerAuth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found. " +
                "Set it with User Secrets (not appsettings.json): " +
                "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Server=localhost,1433;Database=TablerAuth;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;Encrypt=True;\" --project src/TablerAuth.Web");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddErrorDescriber<TurkishIdentityErrorDescriber>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.Events.OnRedirectToLogin = context =>
            {
                if (IsApiRequest(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                if (IsApiRequest(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });

        AddJwtBearer(services, configuration);
        AddAuthorizationPolicies(services);
        AddDynamicExternalProviders(services);

        services.Configure<SmtpEmailOptions>(configuration.GetSection(SmtpEmailOptions.SectionName));
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminDirectoryService, AdminDirectoryService>();
        services.AddScoped<IIdentityProviderService, IdentityProviderService>();
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }

    private static void AddJwtBearer(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey)
                && Encoding.UTF8.GetByteCount(options.SigningKey) >= 32,
                "Jwt:SigningKey must be set via User Secrets (at least 32 bytes). " +
                "Generate one with: python3 -c \"import secrets,base64; print(base64.b64encode(secrets.token_bytes(64)).decode())\" " +
                "then: dotnet user-secrets set \"Jwt:SigningKey\" \"YOUR_KEY\" --project src/TablerAuth.Web")
            .Validate(options => options.AccessTokenMinutes > 0 && options.RefreshTokenDays > 0,
                "Jwt:AccessTokenMinutes and Jwt:RefreshTokenDays must be positive.")
            .ValidateOnStart();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt section is missing from configuration.");

        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey was not found or is too short. Set it with User Secrets (not appsettings.json): " +
                "dotnet user-secrets set \"Jwt:SigningKey\" \"YOUR_KEY\" --project src/TablerAuth.Web");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));

        services.AddAuthentication()
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = !IsDevelopment(configuration);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = System.Security.Claims.ClaimTypes.Name,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            });
    }

    /// <summary>
    /// Dış sağlayıcılar startup'ta sabitlenmiyor. Handler tipleri ve options
    /// kurulumları kaydediliyor; hangi scheme'in gerçekten var olacağına
    /// <see cref="DynamicAuthenticationSchemeProvider"/> çalışma zamanında,
    /// veritabanındaki kayıtlara bakarak karar veriyor.
    /// </summary>
    private static void AddDynamicExternalProviders(IServiceCollection services)
    {
        services.AddSingleton<DynamicSchemeStore>();

        // Yeni bir sağlayıcı türü eklemek = paketi referanslamak + bir binding sınıfı,
        // bir options setup sınıfı ve buraya tek satır.
        services.AddExternalProvider<GoogleProviderBinding, GoogleOptions, GoogleHandler, DynamicGoogleOptionsSetup>();
        services.AddExternalProvider<GitHubProviderBinding, GitHubAuthenticationOptions, GitHubAuthenticationHandler, DynamicGitHubOptionsSetup>();
        services.AddExternalProvider<LinkedInProviderBinding, LinkedInAuthenticationOptions, LinkedInAuthenticationHandler, DynamicLinkedInOptionsSetup>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IPostConfigureOptions<GitHubAuthenticationOptions>,
            GitHubPostConfigureOptions>());

        // OIDC, OAuthOptions ailesinde değil; ayrı handler + options kurulumu.
        AddOpenIdConnectProvider(services);

        // AddIdentity/AddAuthentication varsayılan sağlayıcıyı zaten kaydetti; onun yerine bizimki geçiyor.
        services.RemoveAll<IAuthenticationSchemeProvider>();
        services.AddSingleton<IAuthenticationSchemeProvider, DynamicAuthenticationSchemeProvider>();
    }

    /// <summary>
    /// Bir sağlayıcı türünün handler'ını, options kurulumunu ve önbellek
    /// temizleyicisini kaydeder. Scheme'i bilinçli olarak kaydetmez: buna
    /// çalışma zamanında veritabanına bakarak karar veriliyor.
    /// </summary>
    private static IServiceCollection AddExternalProvider<TBinding, TOptions, THandler, TSetup>(
        this IServiceCollection services)
        where TBinding : class, IExternalProviderBinding
        where TOptions : OAuthOptions, new()
        where THandler : OAuthHandler<TOptions>
        where TSetup : class, IConfigureOptions<TOptions>
    {
        services.AddSingleton<IExternalProviderBinding, TBinding>();
        services.AddTransient<THandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IPostConfigureOptions<TOptions>,
            OAuthPostConfigureOptions<TOptions, THandler>>());
        services.AddSingleton<IConfigureOptions<TOptions>, TSetup>();
        services.AddSingleton<IDynamicSchemeOptionsInvalidator, DynamicSchemeOptionsInvalidator<TOptions>>();

        return services;
    }

    /// <summary>
    /// Generic OpenID Connect, OAuth helper'ın TOptions : OAuthOptions kısıtına uymaz.
    /// Handler ve options kurulumu kaydedilir; scheme yine çalışma anında tablodan gelir.
    /// </summary>
    private static void AddOpenIdConnectProvider(IServiceCollection services)
    {
        services.AddSingleton<IExternalProviderBinding, OidcProviderBinding>();
        services.AddTransient<OpenIdConnectHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IPostConfigureOptions<OpenIdConnectOptions>,
            OpenIdConnectPostConfigureOptions>());
        services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, DynamicOidcOptionsSetup>();
        services.AddSingleton<IDynamicSchemeOptionsInvalidator, DynamicSchemeOptionsInvalidator<OpenIdConnectOptions>>();
    }

    private static void AddAuthorizationPolicies(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AppPolicies.ManageUsers,
                policy => policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.UsersManage));
            options.AddPolicy(
                AppPolicies.ViewRoles,
                policy => policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.RolesView));
            options.AddPolicy(
                AppPolicies.ManageProviders,
                policy => policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.ProvidersManage));
        });
    }

    private static bool IsApiRequest(HttpRequest request) =>
        request.Path.StartsWithSegments("/api");

    private static bool IsDevelopment(IConfiguration configuration)
    {
        var environment = configuration["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
    }
}
