namespace CineMatchAPI.Domain.Entities;

/// <summary>
/// Represents a chat message between matched users.
/// Messages are persisted for history and delivered in real-time via SignalR.
/// </summary>
public class Message
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    // Which match/conversation this message belongs to
    public string MatchId { get; set; } = string.Empty;
    
    // Who sent the message
    public string SenderId { get; set; } = string.Empty;
    
    // The actual message content
    public string Text { get; set; } = string.Empty;
    
    // When the message was sent
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    // Read receipt tracking
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    
    // Navigation properties
    public Match Match { get; set; } = null!;
    public User Sender { get; set; } = null!;
}