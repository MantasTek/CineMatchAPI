using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CineMatchAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _userService.GetUserAsync(GetUserId());
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var user = await _userService.GetUserAsync(GetUserId());
        if (user == null) return NotFound();
        return Ok(new { genres = user.Preferences, movieLength = user.MovieLength });
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesDto dto)
    {
        var success = await _userService.UpdatePreferencesAsync(GetUserId(), dto);
        if (!success) return BadRequest(new { message = "Failed to update preferences" });
        return Ok(new { message = "Preferences updated successfully" });
    }

    [HttpPost("reset")]
    public async Task<IActionResult> ResetUserData()
    {
        var success = await _userService.ResetUserDataAsync(GetUserId());
        if (!success) return BadRequest(new { message = "Failed to reset" });
        return Ok(new { message = "User data reset successfully" });
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SwipeController : ControllerBase
{
    private readonly ISwipeService _swipeService;

    public SwipeController(ISwipeService swipeService)
    {
        _swipeService = swipeService;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    [HttpPost]
    public async Task<IActionResult> SwipeMovie([FromBody] SwipeDto dto)
    {
        var result = await _swipeService.SwipeMovieAsync(GetUserId(), dto);
        
        if (!result.Success)
        {
            return BadRequest(new { message = "Already swiped on this movie" });
        }

        return Ok(new { matched = result.Matched, matchId = result.MatchId });
    }

    [HttpGet("starred")]
    public async Task<IActionResult> GetStarredMovies()
    {
        var movies = await _swipeService.GetUserStarredMoviesAsync(GetUserId());
        return Ok(movies);
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetSwipeCount()
    {
        var count = await _swipeService.GetSwipeCountAsync(GetUserId());
        return Ok(new { count });
    }
}