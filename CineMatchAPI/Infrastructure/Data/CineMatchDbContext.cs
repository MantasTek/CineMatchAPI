using CineMatchAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CineMatchAPI.Infrastructure.Data;

public class CineMatchDbContext : DbContext
{
    public CineMatchDbContext(DbContextOptions<CineMatchDbContext> options) 
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Movie> Movies { get; set; } = null!;
    public DbSet<Swipe> Swipes { get; set; } = null!;
    public DbSet<Match> Matches { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255)
                .UseCollation("NOCASE"); // Case-insensitive comparisons & unique index on SQLite

            entity.Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            entity.Property(u => u.Location)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.Bio)
                .HasMaxLength(500);

            // FIXED: Preferences is now nullable (no .IsRequired(), no default value)
            entity.Property(u => u.Preferences);
        });

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Title)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(m => m.Genre)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(m => m.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(m => m.ImageUrl)
                .IsRequired()
                .HasMaxLength(500);

            entity.HasIndex(m => m.Genre);
            entity.HasIndex(m => m.Runtime);
        });

        modelBuilder.Entity<Swipe>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => new { s.UserId, s.MovieId }).IsUnique();

            entity.HasOne(s => s.User)
                .WithMany(u => u.Swipes)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // FIXED: Added m => m.Swipes to reference the Movie navigation property
            entity.HasOne(s => s.Movie)
                .WithMany(m => m.Swipes)
                .HasForeignKey(s => s.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => s.UserId);
            entity.HasIndex(s => s.MovieId);
            entity.HasIndex(s => new { s.UserId, s.Liked });
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => new { m.User1Id, m.User2Id, m.MovieId }).IsUnique();

            entity.HasOne(m => m.User1)
                .WithMany(u => u.MatchesAsUser1)
                .HasForeignKey(m => m.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.User2)
                .WithMany(u => u.MatchesAsUser2)
                .HasForeignKey(m => m.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Movie)
                .WithMany()
                .HasForeignKey(m => m.MovieId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(m => m.User1Id);
            entity.HasIndex(m => m.User2Id);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Text)
                .IsRequired()
                .HasMaxLength(2000);

            entity.HasOne(m => m.Match)
                .WithMany(match => match.Messages)
                .HasForeignKey(m => m.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(m => new { m.MatchId, m.SentAt });
            entity.HasIndex(m => new { m.MatchId, m.IsRead });
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=cinematch.db;Cache=Shared;Mode=ReadWriteCreate",
                options => options.CommandTimeout(60));
        }

        base.OnConfiguring(optionsBuilder);
    }
}