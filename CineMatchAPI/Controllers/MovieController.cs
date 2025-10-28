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
    private readonly ILogger<MovieController> _logger;

    public MovieController(IMovieService movieService, ILogger<MovieController> logger)
    {
        _movieService = movieService;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> GetMovies(
        [FromQuery] string genre,
        [FromQuery] string length,
        [FromQuery] int page = 1)
    {
        try
        {
            if (string.IsNullOrEmpty(genre) || string.IsNullOrEmpty(length))
            {
                return BadRequest(new { message = "Genre and length are required" });
            }

            var userId = GetUserId();
            var movies = await _movieService.GetMoviesByPreferencesAsync(userId, genre, length, page);
            var movieList = movies.ToList();
            
            return Ok(new {
                data = movieList,
                totalCount = movieList.Count,
                page = page,
                totalPages = 1
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching movies");
            return StatusCode(500, new { message = "Error fetching movies", error = ex.Message });
        }
    }
}