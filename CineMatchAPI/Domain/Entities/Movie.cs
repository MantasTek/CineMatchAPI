namespace CineMatchAPI.Domain.Entities;

/// <summary>
/// Represents a movie from TheMovieDB.
/// Movies are cached locally to reduce API calls and enable offline operation.
/// </summary>
public class Movie
{
    // TheMovieDB ID - we use their ID as our primary key
    public string Id { get; set; } = string.Empty;
    
    public string Title { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public double Rating { get; set; }
    public int Year { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Runtime in minutes - used for length filtering
    public int Runtime { get; set; }
    
    // When we cached this movie from TheMovieDB
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public ICollection<Swipe> Swipes { get; set; } = new List<Swipe>();
}