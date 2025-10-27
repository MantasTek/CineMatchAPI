using CineMatchAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineMatchAPI.Controllers;

/// <summary>
/// Controller for movie-related endpoints.
/// Handles fetching movies based on user preferences.
/// </summary>
[ApiController]
[Route("api/movies")]
[Authorize]
public class MovieController : ControllerBase
{
    private readonly IMovieService _movieService;
    private readonly ILogger<MovieController> _logger;

    public MovieController(IMovieService movieService, ILogger<MovieController> logger)
    {
        _movieService = movieService;
        _logger = logger;
    }

    /// <summary>
    /// Get movies based on user preferences
    /// GET: api/movies?genre=Action&length=short&page=1
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMovies(
        [FromQuery] string genre,
        [FromQuery] string length,
        [FromQuery] int page = 1)
    {
        try
        {
            // Validate required parameters
            if (string.IsNullOrEmpty(genre) || string.IsNullOrEmpty(length))
            {
                return BadRequest(new { message = "Genre and length are required" });
            }

            var movies = await _movieService.GetMoviesByPreferencesAsync(genre, length, page);
            
            if (!movies.Any())
            {
                return Ok(new { 
                    message = "No movies found. The database might need seeding. Call POST /api/admin/seed-movies first.",
                    data = new List<object>()
                });
            }

            return Ok(movies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching movies for genre: {Genre}, length: {Length}", genre, length);
            return StatusCode(500, new { 
                message = "An error occurred while fetching movies", 
                error = ex.Message 
            });
        }
    }
}