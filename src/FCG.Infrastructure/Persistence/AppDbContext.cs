using FCG.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace FCG.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(builder =>
        {
            builder.ToTable("Users");
            builder.HasKey(user => user.Id);
            builder.Property(user => user.Name).IsRequired().HasMaxLength(200);
            builder.Property(user => user.Email).IsRequired().HasMaxLength(320);
            builder.Property(user => user.NormalizedEmail).IsRequired().HasMaxLength(320);
            builder.HasIndex(user => user.NormalizedEmail).IsUnique();
            builder.Property(user => user.PasswordHash).IsRequired().HasMaxLength(512);
            builder.Property(user => user.Role).IsRequired().HasConversion<string>().HasMaxLength(64);
            builder.Property(user => user.CreatedAtUtc).IsRequired();
        });
    }
}
