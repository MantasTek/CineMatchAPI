using CineMatchAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CineMatchAPI.Controllers;

/// <summary>
/// Controller responsible for movie discovery and retrieval.
/// 
/// This controller interfaces with TheMovieDB API through the MovieService
/// to provide personalized movie recommendations based on user preferences.
/// 
/// Key responsibilities:
/// - Fetch movies filtered by genre and runtime
/// - Handle preference fallbacks for users mid-onboarding
/// - Exclude movies the user has already swiped
/// 
/// CRITICAL FIX: Improved fallback logic ensures movies are ALWAYS returned,
/// even when users are transitioning from onboarding to the swipe page.
/// </summary>
[ApiController]
[Route("api/movies")]
[Authorize]
public class MovieController : ControllerBase
{
    private readonly IMovieService _movieService;
    private readonly IUserService _userService;
    private readonly ILogger<MovieController> _logger;

    public MovieController(
        IMovieService movieService, 
        IUserService userService, 
        ILogger<MovieController> logger)
    {
        _movieService = movieService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Helper method to extract the user ID from the JWT claims.
    /// This provides a clean, consistent way to identify the authenticated user.
    /// </summary>
    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    /// <summary>
    /// Get movies filtered by genre and length preferences.
    /// 
    /// This endpoint supports three modes of operation:
    /// 1. Explicit parameters: Client sends genre and length
    /// 2. User preferences: Falls back to saved user preferences
    /// 3. System defaults: Uses sensible defaults if nothing else is available
    /// 
    /// CRITICAL FIX: The third mode is crucial for handling the edge case where
    /// a user has just completed onboarding and is navigating to the swipe page.
    /// In this scenario, there may be a brief moment where the frontend hasn't
    /// received the updated user object yet, but we still want to show movies.
    /// 
    /// Query Parameters:
    /// - genre: Movie genre (Action, Comedy, Drama, etc.) - Optional
    /// - length: Movie length preference (short, medium, long) - Optional
    /// - page: Pagination page number (default: 1) - Optional
    /// 
    /// Returns:
    /// - 200 OK: Paginated list of movies matching criteria
    /// - 500 Internal Server Error: If movie fetching fails
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMovies(
        [FromQuery] string? genre,
        [FromQuery] string? length,
        [FromQuery] int page = 1)
    {
        try
        {
            var userId = GetUserId();

            // CRITICAL FIX: Improved preference resolution with multiple fallback levels
            // This ensures we ALWAYS have valid genre and length values to query with
            
            // Level 1: Use query parameters if provided (highest priority)
            // This allows the frontend to explicitly request specific movies
            // Note: Declared as nullable to match the method parameters
            string? finalGenre = genre;
            string? finalLength = length;

            // Level 2: Fall back to user's saved preferences if query params are missing
            // This is the normal case for users who have completed onboarding
            if (string.IsNullOrWhiteSpace(finalGenre) || string.IsNullOrWhiteSpace(finalLength))
            {
                _logger.LogInformation("Query parameters incomplete, fetching user preferences for userId: {UserId}", userId);
                
                var user = await _userService.GetUserAsync(userId);
                
                if (user != null)
                {
                    // Use user's first preference if available
                    if (string.IsNullOrWhiteSpace(finalGenre) && user.Preferences != null && user.Preferences.Count > 0)
                    {
                        finalGenre = user.Preferences[0];
                        _logger.LogInformation("Using user's preference genre: {Genre}", finalGenre);
                    }
                    
                    // Use user's movie length preference if available
                    if (string.IsNullOrWhiteSpace(finalLength) && !string.IsNullOrWhiteSpace(user.MovieLength))
                    {
                        finalLength = user.MovieLength;
                        _logger.LogInformation("Using user's preference length: {Length}", finalLength);
                    }
                }
            }

            // Level 3: System defaults (lowest priority, but ensures we NEVER return empty-handed)
            // This is crucial for edge cases:
            // - New users who haven't set preferences yet
            // - Users mid-onboarding transition
            // - Error cases where user data isn't available
            if (string.IsNullOrWhiteSpace(finalGenre))
            {
                finalGenre = "Action";  // Action is a popular, safe default
                _logger.LogInformation("No genre found, using system default: Action");
            }

            if (string.IsNullOrWhiteSpace(finalLength))
            {
                finalLength = "medium";  // Medium length (90-130 min) covers most movies
                _logger.LogInformation("No length preference found, using system default: medium");
            }

            // Fetch movies with the resolved preferences
            _logger.LogInformation(
                "Fetching movies for userId: {UserId}, genre: {Genre}, length: {Length}, page: {Page}",
                userId, finalGenre, finalLength, page);

            var movies = await _movieService.GetMoviesByPreferencesAsync(
                userId, 
                finalGenre, 
                finalLength, 
                page);

            var movieList = movies.ToList();
            
            _logger.LogInformation("Successfully fetched {Count} movies for userId: {UserId}", movieList.Count, userId);
            
            // Return consistent paginated response structure
            return Ok(new 
            {
                data = movieList,
                totalCount = movieList.Count,
                page = page,
                totalPages = 1  // Simplified pagination for now
            });
        }
        catch (Exception ex)
        {
            // Comprehensive error logging for debugging
            _logger.LogError(ex, 
                "Error fetching movies for userId: {UserId}, genre: {Genre}, length: {Length}", 
                GetUserId(), genre ?? "null", length ?? "null");
            
            // Return detailed error response to help frontend debugging
            return StatusCode(500, new 
            { 
                message = "Error fetching movies", 
                error = ex.Message,
                // In production, you might want to omit the detailed error for security
                // but during development, this is invaluable for debugging
                details = ex.StackTrace
            });
        }
    }
}