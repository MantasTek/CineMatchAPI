using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Application.Services;
using CineMatchAPI.Controllers;
using CineMatchAPI.Domain.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CineMatch.Tests.Controllers;

public class MatchControllerTests
{
    private readonly Mock<IMatchService> _matchServiceMock;
    private readonly Mock<IMatchRepository> _matchRepositoryMock;
    private readonly MatchController _controller;
    private const string TestUserId = "user123";

    public MatchControllerTests()
    {
        _matchServiceMock = new Mock<IMatchService>();
        _matchRepositoryMock = new Mock<IMatchRepository>();
        _controller = new MatchController(_matchServiceMock.Object, _matchRepositoryMock.Object);
        SetupUserClaims(TestUserId);
    }

    #region GetMatches Tests

    [Fact]
    public async Task GetMatches_ReturnsOkWithMatches()
    {
        var matches = new List<MatchDto>
        {
            CreateMatchDto("match1", "user2", "movie1"),
            CreateMatchDto("match2", "user3", "movie2")
        };
        _matchServiceMock.Setup(x => x.GetUserMatchesAsync(TestUserId)).ReturnsAsync(matches);

        var result = await _controller.GetMatches();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedMatches = okResult.Value.Should().BeAssignableTo<IEnumerable<MatchDto>>().Subject;
        returnedMatches.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMatches_WithNoMatches_ReturnsEmptyList()
    {
        _matchServiceMock.Setup(x => x.GetUserMatchesAsync(TestUserId)).ReturnsAsync(new List<MatchDto>());

        var result = await _controller.GetMatches();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedMatches = okResult.Value.Should().BeAssignableTo<IEnumerable<MatchDto>>().Subject;
        returnedMatches.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMatches_UsesCurrentUserId()
    {
        _matchServiceMock.Setup(x => x.GetUserMatchesAsync(It.IsAny<string>())).ReturnsAsync(new List<MatchDto>());

        await _controller.GetMatches();

        _matchServiceMock.Verify(x => x.GetUserMatchesAsync(TestUserId), Times.Once);
    }

    #endregion

    #region GetPotentialMatches Tests

    [Fact]
    public async Task GetPotentialMatches_ReturnsOkWithPotentialMatches()
    {
        var potentialMatches = new List<MatchDto>
        {
            CreateMatchDto("potential1", "user4", "movie3")
        };
        _matchServiceMock.Setup(x => x.FindPotentialMatchesAsync(TestUserId)).ReturnsAsync(potentialMatches);

        var result = await _controller.GetPotentialMatches();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedMatches = okResult.Value.Should().BeAssignableTo<IEnumerable<MatchDto>>().Subject;
        returnedMatches.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPotentialMatches_WithNoPotentials_ReturnsEmptyList()
    {
        _matchServiceMock.Setup(x => x.FindPotentialMatchesAsync(TestUserId)).ReturnsAsync(new List<MatchDto>());

        var result = await _controller.GetPotentialMatches();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedMatches = okResult.Value.Should().BeAssignableTo<IEnumerable<MatchDto>>().Subject;
        returnedMatches.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private void SetupUserClaims(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, $"{userId}@test.com")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private MatchDto CreateMatchDto(string matchId, string otherUserId, string movieId)
    {
        return new MatchDto(
            matchId,
            new UserDto(otherUserId, $"User {otherUserId}", $"{otherUserId}@test.com", "City", null, null, new List<string>(), "medium"),
            new MovieDto(movieId, "Test Movie", "Action", 8.0, 2024, "http://image.url", "Description", 120),
            DateTime.UtcNow
        );
    }

    #endregion
}