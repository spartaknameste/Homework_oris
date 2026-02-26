using AliasGame.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AliasGame.Shared.ORM;

public class AliasDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Word> Words { get; set; } = null!;
    public DbSet<GameHistory> GameHistories { get; set; } = null!;
    public DbSet<GameSettingsPreset> GameSettingsPresets { get; set; } = null!;

    private readonly string _connectionString;

    public AliasDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public AliasDbContext(DbContextOptions<AliasDbContext> options) : base(options)
    {
        _connectionString = string.Empty;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(_connectionString))
        {
            optionsBuilder.UseNpgsql(_connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

                modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

                modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

                modelBuilder.Entity<Word>(entity =>
        {
            entity.HasIndex(e => new { e.WordText, e.CategoryId }).IsUnique();
            
            entity.HasOne(w => w.Category)
                .WithMany(c => c.Words)
                .HasForeignKey(w => w.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

                modelBuilder.Entity<GameHistory>(entity =>
        {
            entity.HasOne(g => g.User)
                .WithMany(u => u.GameHistories)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.GameDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

                modelBuilder.Entity<GameSettingsPreset>(entity =>
        {
            entity.HasIndex(e => e.Name);
        });
    }
}
