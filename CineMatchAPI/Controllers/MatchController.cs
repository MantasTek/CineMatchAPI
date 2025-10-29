using CineMatchAPI.Application.Services;
using CineMatchAPI.Domain.Interfaces;
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
    private readonly IMatchRepository _matchRepository;

    public MatchController(IMatchService matchService, IMatchRepository matchRepository)
    {
        _matchService = matchService;
        _matchRepository = matchRepository;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> GetMatches()
    {
        var matches = await _matchService.GetUserMatchesAsync(GetUserId());
        return Ok(matches);
    }

    [HttpGet("{matchId}")]
    public async Task<IActionResult> GetMatchById(string matchId)
    {
        var match = await _matchRepository.GetByIdAsync(matchId);
        if (match == null)
        {
            return NotFound(new { message = "Match not found" });
        }

        var userId = GetUserId();
        if (match.User1Id != userId && match.User2Id != userId)
        {
            return Unauthorized(new { message = "Not authorized to view this match" });
        }

        var otherUser = match.User1Id == userId ? match.User2 : match.User1;

        return Ok(new
        {
            id = match.Id,
            otherUser = new
            {
                id = otherUser.Id,
                name = otherUser.Name,
                email = otherUser.Email,
                location = otherUser.Location,
                bio = otherUser.Bio,
                avatarUrl = otherUser.AvatarUrl
            },
            movie = new
            {
                id = match.Movie.Id,
                title = match.Movie.Title,
                genre = match.Movie.Genre,
                rating = match.Movie.Rating,
                year = match.Movie.Year,
                imageUrl = match.Movie.ImageUrl,
                description = match.Movie.Description,
                runtime = match.Movie.Runtime
            },
            matchedAt = match.MatchedAt
        });
    }

    [HttpGet("potential")]
    public async Task<IActionResult> GetPotentialMatches()
    {
        var matches = await _matchService.FindPotentialMatchesAsync(GetUserId());
        return Ok(matches);
    }
}