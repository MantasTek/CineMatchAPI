using CineMatchAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CineMatchAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MatchController : ControllerBase
{
    private readonly IMatchService _matchService;

    public MatchController(IMatchService matchService)
    {
        _matchService = matchService;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> GetMatches()
    {
        var matches = await _matchService.GetUserMatchesAsync(GetUserId());
        return Ok(matches);
    }

    [HttpGet("potential")]
    public async Task<IActionResult> GetPotentialMatches()
    {
        var matches = await _matchService.FindPotentialMatchesAsync(GetUserId());
        return Ok(matches);
    }
}