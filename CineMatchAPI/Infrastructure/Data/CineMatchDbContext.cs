using CineMatchAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CineMatchAPI.Infrastructure.Data;

/// <summary>
/// The database context for CineMatch application.
/// This class represents a session with the database and provides DbSet properties
/// for each entity type we want to query or save.
/// </summary>
public class CineMatchDbContext : DbContext
{
    // Constructor that accepts configuration options
    // These options are provided by dependency injection and contain
    // connection string, database provider settings, etc.
    public CineMatchDbContext(DbContextOptions<CineMatchDbContext> options) 
        : base(options)
    {
    }

    // DbSet properties represent tables in our database
    // Each DbSet<T> gives us a way to query and modify entities of type T
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Movie> Movies { get; set; } = null!;
    public DbSet<Swipe> Swipes { get; set; } = null!;
    public DbSet<Match> Matches { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;

    /// <summary>
    /// This method is called when EF Core is building the model from our entities.
    /// Here we configure relationships, constraints, indexes, and other database details
    /// that cannot be expressed through simple properties and attributes.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure the User entity
        modelBuilder.Entity<User>(entity =>
        {
            // Set the primary key explicitly
            entity.HasKey(u => u.Id);

            // Email must be unique across all users
            // This creates a unique index in the database
            entity.HasIndex(u => u.Email).IsUnique();

            // Email is required and has a maximum length
            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            // Name is required and has a reasonable maximum length
            entity.Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(100);

            // Password hash must never be null
            entity.Property(u => u.PasswordHash)
                .IsRequired();

            // Location is required for matching purposes
            entity.Property(u => u.Location)
                .IsRequired()
                .HasMaxLength(100);

            // Bio is optional but has a maximum length when provided
            entity.Property(u => u.Bio)
                .HasMaxLength(500);

            // Preferences stored as JSON string
            entity.Property(u => u.Preferences)
                .IsRequired()
                .HasDefaultValue("[]");
        });

        // Configure the Movie entity
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

            // Index on genre for faster filtering
            // When users filter by genre, this index makes queries much faster
            entity.HasIndex(m => m.Genre);

            // Index on runtime for length-based filtering
            entity.HasIndex(m => m.Runtime);
        });

        // Configure the Swipe entity
        modelBuilder.Entity<Swipe>(entity =>
        {
            entity.HasKey(s => s.Id);

            // A user can only swipe on each movie once
            // This composite unique index prevents duplicate swipes
            entity.HasIndex(s => new { s.UserId, s.MovieId }).IsUnique();

            // Define the relationship between Swipe and User
            // One user has many swipes
            // When a user is deleted, their swipes should be deleted too (Cascade)
            entity.HasOne(s => s.User)
                .WithMany(u => u.Swipes)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Define the relationship between Swipe and Movie
            // One movie can be swiped by many users
            // When a movie is deleted, swipes on it should be deleted too
            entity.HasOne(s => s.Movie)
                .WithMany(m => m.Swipes)
                .HasForeignKey(s => s.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for finding all swipes by a user quickly
            entity.HasIndex(s => s.UserId);

            // Index for finding all swipes on a movie
            entity.HasIndex(s => s.MovieId);

            // Index for finding starred movies (Liked = true)
            entity.HasIndex(s => new { s.UserId, s.Liked });
        });

        // Configure the Match entity
        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasKey(m => m.Id);

            // Prevent duplicate matches between the same users for the same movie
            entity.HasIndex(m => new { m.User1Id, m.User2Id, m.MovieId }).IsUnique();

            // Define relationship with User1
            // The Restrict delete behavior means we cannot delete a user who has matches
            // This prevents orphaned matches and maintains data integrity
            entity.HasOne(m => m.User1)
                .WithMany(u => u.MatchesAsUser1)
                .HasForeignKey(m => m.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Define relationship with User2
            // We use Restrict here too to maintain referential integrity
            entity.HasOne(m => m.User2)
                .WithMany(u => u.MatchesAsUser2)
                .HasForeignKey(m => m.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Define relationship with Movie
            entity.HasOne(m => m.Movie)
                .WithMany()
                .HasForeignKey(m => m.MovieId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for quickly finding matches by user
            entity.HasIndex(m => m.User1Id);
            entity.HasIndex(m => m.User2Id);
        });

        // Configure the Message entity
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Text)
                .IsRequired()
                .HasMaxLength(2000);

            // Define relationship with Match
            // One match has many messages
            entity.HasOne(m => m.Match)
                .WithMany(match => match.Messages)
                .HasForeignKey(m => m.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            // Define relationship with Sender (User)
            entity.HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for fetching messages in a match chronologically
            entity.HasIndex(m => new { m.MatchId, m.SentAt });

            // Index for finding unread messages
            entity.HasIndex(m => new { m.MatchId, m.IsRead });
        });
    }
}