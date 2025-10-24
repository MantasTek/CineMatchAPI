using CineMatchAPI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CineMatchAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IMovieService _movieService;

    public AdminController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpPost("seed-movies")]
    public async Task<IActionResult> SeedMovies()
    {
        await _movieService.SeedMoviesAsync();
        return Ok(new { message = "Movies seeded successfully" });
    }
}