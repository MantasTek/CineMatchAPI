using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Application.Services;
using CineMatchAPI.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CineMatch.Tests.Controllers;

public class MovieControllerTests
{
    private readonly Mock<IMovieService> _movieServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<MovieController>> _loggerMock;
    private readonly MovieController _controller;
    private const string TestUserId = "user123";

    public MovieControllerTests()
    {
        _movieServiceMock = new Mock<IMovieService>();
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<MovieController>>();
        _controller = new MovieController(_movieServiceMock.Object, _userServiceMock.Object, _loggerMock.Object);
        SetupUserClaims(TestUserId);
    }

    #region GetMovies Tests

    [Fact]
    public async Task GetMovies_WithValidParameters_ReturnsOk()
    {
        var genre = "Action";
        var length = "medium";
        var movies = new List<MovieDto>
        {
            CreateMovieDto("1", "Movie1", genre),
            CreateMovieDto("2", "Movie2", genre)
        };
        
        _movieServiceMock
            .Setup(x => x.GetMoviesByPreferencesAsync(TestUserId, genre, length,1,20))
            .ReturnsAsync(movies);

        var result = await _controller.GetMovies(genre, length,1);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetMovies_WithMissingGenre_ReturnsBadRequest()
    {
        _userServiceMock
            .Setup(x => x.GetUserAsync(TestUserId))
            .ReturnsAsync(new UserDto(TestUserId, "Name", "email@test.com", "City", null, null, null, null));

        var result = await _controller.GetMovies("", "medium",1);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMovies_WithMissingLength_ReturnsBadRequest()
    {
        _userServiceMock
            .Setup(x => x.GetUserAsync(TestUserId))
            .ReturnsAsync(new UserDto(TestUserId, "Name", "email@test.com", "City", null, null, new List<string>{"Action"}, null));

        var result = await _controller.GetMovies("Action", "",1);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMovies_WithNullGenre_ReturnsBadRequest()
    {
        _userServiceMock
            .Setup(x => x.GetUserAsync(TestUserId))
            .ReturnsAsync(new UserDto(TestUserId, "Name", "email@test.com", "City", null, null, null, null));

        var result = await _controller.GetMovies(null!, "medium",1);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMovies_WithException_ReturnsInternalServerError()
    {
        _movieServiceMock
            .Setup(x => x.GetMoviesByPreferencesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Test exception"));

        var result = await _controller.GetMovies("Action", "medium",1);

        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetMovies_CallsServiceWithCorrectUserId()
    {
        var genre = "Comedy";
        var length = "short";
        _movieServiceMock
            .Setup(x => x.GetMoviesByPreferencesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<MovieDto>());

        await _controller.GetMovies(genre, length,1);

        _movieServiceMock.Verify(x => x.GetMoviesByPreferencesAsync(TestUserId, genre, length,1,20), Times.Once);
    }

    [Fact]
    public async Task GetMovies_WithDifferentPage_PassesCorrectPageNumber()
    {
        var genre = "Drama";
        var length = "long";
        var page =3;
        _movieServiceMock
            .Setup(x => x.GetMoviesByPreferencesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<MovieDto>());

        await _controller.GetMovies(genre, length, page);

        _movieServiceMock.Verify(x => x.GetMoviesByPreferencesAsync(TestUserId, genre, length, page,20), Times.Once);
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

    private MovieDto CreateMovieDto(string id, string title, string genre)
    {
        return new MovieDto(
            id,
            title,
            genre,
            8.0,
            2024,
            "http://image.url",
            "Test description",
            120
        );
    }

    #endregion
}