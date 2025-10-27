using CineMatchAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineMatchAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly CineMatchDbContext _context;

    public TestController(CineMatchDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "CineMatch API is running!", timestamp = DateTime.UtcNow });
    }

    [HttpGet("movies")]
    public async Task<IActionResult> TestMovies()
    {
        var allMovies = await _context.Movies.ToListAsync();
        var actionMovies = allMovies.Where(m => m.Genre == "Action").ToList();
        var mediumAction = actionMovies.Where(m => m.Runtime >= 90 && m.Runtime <= 130).ToList();
        
        return Ok(new {
            totalMovies = allMovies.Count,
            totalAction = actionMovies.Count,
            mediumAction = mediumAction.Count,
            sampleMovies = mediumAction.Take(3).Select(m => new { m.Title, m.Runtime, m.Genre })
        });
    }
}