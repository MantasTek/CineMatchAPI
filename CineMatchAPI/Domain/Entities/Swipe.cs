namespace CineMatchAPI.Domain.Entities;

/// <summary>
/// Records a user's decision about a movie (like or dislike).
/// This is the core data for our matching algorithm.
/// </summary>
public class Swipe
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    // Foreign keys - who swiped on what
    public string UserId { get; set; } = string.Empty;
    public string MovieId { get; set; } = string.Empty;
    
    // The decision - true for like, false for dislike
    public bool Liked { get; set; }
    
    // When this swipe occurred
    public DateTime SwipedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties back to related entities
    public User User { get; set; } = null!;
    public Movie Movie { get; set; } = null!;
}