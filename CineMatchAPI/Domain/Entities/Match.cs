namespace CineMatchAPI.Domain.Entities;

/// <summary>
/// Represents a match between two users who both liked the same movie.
/// Once matched, users can chat about planning to watch the movie together.
/// </summary>
public class Match
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    // The two users who matched
    public string User1Id { get; set; } = string.Empty;
    public string User2Id { get; set; } = string.Empty;
    
    // The movie they both liked
    public string MovieId { get; set; } = string.Empty;
    
    // When the match was created
    public DateTime MatchedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User User1 { get; set; } = null!;
    public User User2 { get; set; } = null!;
    public Movie Movie { get; set; } = null!;
    
    // Messages in this match's chat
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}