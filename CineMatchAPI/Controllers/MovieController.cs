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
    private readonly MovieService _movieService;

    public MovieController(MovieService movieService)
    {
        _movieService = movieService;
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
        // Validate required parameters
        if (string.IsNullOrEmpty(genre) || string.IsNullOrEmpty(length))
        {
            return BadRequest(new { message = "Genre and length are required" });
        }

        var movies = await _movieService.GetMoviesByPreferencesAsync(genre, length, page);
        return Ok(movies);
    }
}