using CineMatchAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineMatchAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MovieController : ControllerBase
{
    private readonly MovieService _movieService;

    public MovieController(MovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMovies(
        [FromQuery] string genre,
        [FromQuery] string length,
        [FromQuery] int page = 1)
    {
        if (string.IsNullOrEmpty(genre) || string.IsNullOrEmpty(length))
        {
            return BadRequest(new { message = "Genre and length are required" });
        }

        var movies = await _movieService.GetMoviesByPreferencesAsync(genre, length, page);
        return Ok(movies);
    }
}