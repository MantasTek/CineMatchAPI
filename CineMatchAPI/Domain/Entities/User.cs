namespace CineMatchAPI.Domain.Entities;

/// <summary>
/// Represents a user in the CineMatch application.
/// This is a domain entity containing business identity and rules.
/// </summary>
public class User
{
    // Primary key - uniquely identifies each user
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    // Basic profile information
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Security - stored as hashed value, never plain text
    public string PasswordHash { get; set; } = string.Empty;
    
    // User location for matching purposes
    public string Location { get; set; } = string.Empty;
    
    // Optional profile details
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    
    // Movie preferences - stored as JSON string
    // null = onboarding not completed
    // "[]" = no preferences selected
    // "[\"Action\", \"Comedy\"]" = preferences selected
    public string? Preferences { get; set; }
    
    // Preferred movie length: "short", "medium", or "long"
    public string? MovieLength { get; set; }
    
    // Audit fields - track when records are created/modified
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties for Entity Framework relationships
    public ICollection<Swipe> Swipes { get; set; } = new List<Swipe>();
    public ICollection<Match> MatchesAsUser1 { get; set; } = new List<Match>();
    public ICollection<Match> MatchesAsUser2 { get; set; } = new List<Match>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
}