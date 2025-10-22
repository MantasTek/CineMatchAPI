using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CineMatchAPI.Controllers;

/// <summary>
/// Simple test controller to verify our database and dependency injection are working.
/// This will be removed once we have proper controllers, but it's useful for initial testing.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    // Dependency injection automatically provides the repository
    public TestController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// A simple GET endpoint that returns a test message.
    /// GET: api/test
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "CineMatch API is running!", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Test database connectivity by counting users.
    /// GET: api/test/db
    /// </summary>
    [HttpGet("db")]
    public async Task<IActionResult> TestDatabase()
    {
        try
        {
            var users = await _userRepository.GetAllAsync();
            return Ok(new 
            { 
                message = "Database connection successful",
                userCount = users.Count(),
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                message = "Database connection failed", 
                error = ex.Message 
            });
        }
    }
}