using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Application.Services;
using CineMatchAPI.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CineMatch.Tests.Controllers;

/// <summary>
/// Tests for SwipeController endpoints
/// Verifies swipe functionality, starred movies, and swipe counting
/// </summary>
public class SwipeControllerTests
{
    private readonly Mock<ISwipeService> _swipeServiceMock;
    private readonly SwipeController _controller;
    private const string TestUserId = "user123";

    public SwipeControllerTests()
    {
        _swipeServiceMock = new Mock<ISwipeService>();
        _controller = new SwipeController(_swipeServiceMock.Object);
        SetupUserClaims(TestUserId);
    }

    #region SwipeMovie Tests

    [Fact]
    public async Task SwipeMovie_WithValidSwipe_ReturnsOk()
    {
    // Arrange
        var dto = new SwipeDto("movie1", true);
var swipeResult = new SwipeResultDto(true, false, null);
        
        _swipeServiceMock
 .Setup(x => x.SwipeMovieAsync(TestUserId, dto))
        .ReturnsAsync(swipeResult);

      // Act
        var result = await _controller.SwipeMovie(dto);

      // Assert
 var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        _swipeServiceMock.Verify(x => x.SwipeMovieAsync(TestUserId, dto), Times.Once);
    }

    [Fact]
    public async Task SwipeMovie_WhenAlreadySwiped_ReturnsBadRequest()
    {
  // Arrange
        var dto = new SwipeDto("movie1", true);
 var swipeResult = new SwipeResultDto(false, false, null);
        
        _swipeServiceMock
        .Setup(x => x.SwipeMovieAsync(TestUserId, dto))
            .ReturnsAsync(swipeResult);

      // Act
      var result = await _controller.SwipeMovie(dto);

      // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task SwipeMovie_WithMatchCreated_ReturnsMatchInfo()
    {
        // Arrange
var dto = new SwipeDto("movie1", true);
        var matchId = "match123";
        var swipeResult = new SwipeResultDto(true, true, matchId);
        
        _swipeServiceMock
            .Setup(x => x.SwipeMovieAsync(TestUserId, dto))
            .ReturnsAsync(swipeResult);

  // Act
        var result = await _controller.SwipeMovie(dto);

        // Assert
   var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
  var value = okResult.Value;
      value.Should().NotBeNull();
    }

    [Fact]
    public async Task SwipeMovie_WithLike_CallsServiceCorrectly()
  {
      // Arrange
        var dto = new SwipeDto("movie1", true);
        var swipeResult = new SwipeResultDto(true, false, null);
        
        _swipeServiceMock
   .Setup(x => x.SwipeMovieAsync(TestUserId, dto))
     .ReturnsAsync(swipeResult);

        // Act
     await _controller.SwipeMovie(dto);

    // Assert
        _swipeServiceMock.Verify(x => x.SwipeMovieAsync(
        TestUserId,
            It.Is<SwipeDto>(d => d.MovieId == "movie1" && d.Liked == true)
        ), Times.Once);
    }

    [Fact]
    public async Task SwipeMovie_WithDislike_CallsServiceCorrectly()
    {
     // Arrange
        var dto = new SwipeDto("movie1", false);
        var swipeResult = new SwipeResultDto(true, false, null);
   
        _swipeServiceMock
     .Setup(x => x.SwipeMovieAsync(TestUserId, dto))
    .ReturnsAsync(swipeResult);

        // Act
        await _controller.SwipeMovie(dto);

        // Assert
        _swipeServiceMock.Verify(x => x.SwipeMovieAsync(
            TestUserId,
    It.Is<SwipeDto>(d => d.MovieId == "movie1" && d.Liked == false)
        ), Times.Once);
    }

    #endregion

    #region GetStarredMovies Tests

    [Fact]
  public async Task GetStarredMovies_ReturnsOkWithMovies()
    {
     // Arrange
        var movies = new List<MovieDto>
        {
  new MovieDto("1", "Movie1", "Action", 8.0, 2024, "url1", "desc1", 120),
            new MovieDto("2", "Movie2", "Drama", 7.5, 2023, "url2", "desc2", 130)
      };
        
        _swipeServiceMock
            .Setup(x => x.GetUserStarredMoviesAsync(TestUserId))
   .ReturnsAsync(movies);

        // Act
        var result = await _controller.GetStarredMovies();

        // Assert
    var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        var returnedMovies = okResult.Value.Should().BeAssignableTo<IEnumerable<MovieDto>>().Subject;
   returnedMovies.Should().HaveCount(2);
  }

    [Fact]
    public async Task GetStarredMovies_WithNoStarredMovies_ReturnsEmptyList()
    {
        // Arrange
        _swipeServiceMock
      .Setup(x => x.GetUserStarredMoviesAsync(TestUserId))
        .ReturnsAsync(new List<MovieDto>());

 // Act
        var result = await _controller.GetStarredMovies();

        // Assert
 var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedMovies = okResult.Value.Should().BeAssignableTo<IEnumerable<MovieDto>>().Subject;
        returnedMovies.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStarredMovies_UsesCorrectUserId()
    {
  // Arrange
        _swipeServiceMock
            .Setup(x => x.GetUserStarredMoviesAsync(TestUserId))
            .ReturnsAsync(new List<MovieDto>());

 // Act
        await _controller.GetStarredMovies();

    // Assert
        _swipeServiceMock.Verify(x => x.GetUserStarredMoviesAsync(TestUserId), Times.Once);
    }

  #endregion

    #region GetSwipeCount Tests

    [Fact]
    public async Task GetSwipeCount_ReturnsOkWithCount()
    {
     // Arrange
        var count = 42;
        _swipeServiceMock
     .Setup(x => x.GetSwipeCountAsync(TestUserId))
        .ReturnsAsync(count);

  // Act
        var result = await _controller.GetSwipeCount();

  // Assert
   var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

  [Fact]
public async Task GetSwipeCount_WithZeroSwipes_ReturnsZero()
    {
      // Arrange
 _swipeServiceMock
     .Setup(x => x.GetSwipeCountAsync(TestUserId))
            .ReturnsAsync(0);

        // Act
        var result = await _controller.GetSwipeCount();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
 public async Task GetSwipeCount_UsesCorrectUserId()
    {
        // Arrange
        _swipeServiceMock
    .Setup(x => x.GetSwipeCountAsync(TestUserId))
        .ReturnsAsync(10);

   // Act
        await _controller.GetSwipeCount();

     // Assert
   _swipeServiceMock.Verify(x => x.GetSwipeCountAsync(TestUserId), Times.Once);
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

    #endregion
}
