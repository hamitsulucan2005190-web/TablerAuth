using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TablerAuth.Domain.Entities;

namespace TablerAuth.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<IdentityProvider> IdentityProviders => Set<IdentityProvider>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(100);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.UserId).HasMaxLength(450).IsRequired();
            entity.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(token => token.ReplacedByTokenHash).HasMaxLength(64);
            entity.Property(token => token.CreatedByIp).HasMaxLength(45);
            entity.Property(token => token.RevokedByIp).HasMaxLength(45);
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => token.UserId);
            entity.HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<IdentityProvider>(entity =>
        {
            entity.ToTable("IdentityProviders");
            entity.HasKey(provider => provider.Id);
            entity.Property(provider => provider.Scheme).HasMaxLength(64).IsRequired();
            entity.Property(provider => provider.DisplayName).HasMaxLength(64).IsRequired();
            entity.Property(provider => provider.Kind).HasConversion<int>();
            entity.Property(provider => provider.ClientId).HasMaxLength(256).IsRequired();
            entity.Property(provider => provider.ClientSecretKey).HasMaxLength(256).IsRequired();
            entity.Property(provider => provider.Authority).HasMaxLength(256);
            entity.Property(provider => provider.Scopes).HasMaxLength(512);
            entity.Property(provider => provider.CallbackPath).HasMaxLength(128);
            entity.HasIndex(provider => provider.Scheme).IsUnique();
        });
    }
}
