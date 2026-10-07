using ApiTestGen.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ApiTestGen.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Generation> Generations => Set<Generation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<Generation>(e =>
        {
            e.ToTable("generations");
            e.HasKey(g => g.Id);
            e.Property(g => g.Title).HasMaxLength(200).IsRequired();
            e.Property(g => g.InputType).HasConversion<string>().HasMaxLength(32);
            e.Property(g => g.Input).IsRequired();
            e.Property(g => g.BaseUrl).HasMaxLength(2048);
            e.Property(g => g.TestCasesJson).HasColumnType("jsonb").IsRequired();
            e.Property(g => g.PostmanCollectionJson).HasColumnType("jsonb").IsRequired();
            e.Property(g => g.PytestCode).IsRequired();
            e.Property(g => g.Model).HasMaxLength(100);
            e.HasOne(g => g.User)
                .WithMany(u => u.Generations)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(g => new { g.UserId, g.CreatedAt });
        });
    }
}

/// <summary>Used by `dotnet ef` so migrations can be created without real secrets configured.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=apitestgen;Username=postgres;Password=postgres")
            .Options);
}
