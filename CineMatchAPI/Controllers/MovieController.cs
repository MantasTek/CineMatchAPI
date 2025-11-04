using CineMatchAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CineMatchAPI.Controllers;

[ApiController]
[Route("api/movies")]
[Authorize]
public class MovieController : ControllerBase
{
    private readonly IMovieService _movieService;
    private readonly IUserService _userService;
    private readonly ILogger<MovieController> _logger;

    public MovieController(IMovieService movieService, IUserService userService, ILogger<MovieController> logger)
    {
        _movieService = movieService;
        _userService = userService;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> GetMovies(
        [FromQuery] string? genre,
        [FromQuery] string? length,
        [FromQuery] int page =1)
    {
        try
        {
            var userId = GetUserId();

            // Fallback to user's saved preferences if query params are missing
            if (string.IsNullOrWhiteSpace(genre) || string.IsNullOrWhiteSpace(length))
            {
                var user = await _userService.GetUserAsync(userId);
                genre ??= user?.Preferences?.FirstOrDefault();
                length ??= user?.MovieLength ?? "medium";
            }

            if (string.IsNullOrWhiteSpace(genre) || string.IsNullOrWhiteSpace(length))
            {
                return BadRequest(new { message = "Genre and length are required" });
            }

            var movies = await _movieService.GetMoviesByPreferencesAsync(userId, genre, length, page);
            var movieList = movies.ToList();
            
            return Ok(new {
                data = movieList,
                totalCount = movieList.Count,
                page = page,
                totalPages =1
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching movies");
            return StatusCode(500, new { message = "Error fetching movies", error = ex.Message });
        }
    }
}