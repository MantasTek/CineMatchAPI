using CineMatchAPI.Application.Services;
using CineMatchAPI.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CineMatch.Tests.Controllers;

/// <summary>
/// Tests for AdminController endpoints
/// Verifies administrative functions like movie seeding
/// </summary>
public class AdminControllerTests
{
    private readonly Mock<IMovieService> _movieServiceMock;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
    _movieServiceMock = new Mock<IMovieService>();
     _controller = new AdminController(_movieServiceMock.Object);
}

    #region SeedMovies Tests

    [Fact]
    public async Task SeedMovies_CallsMovieServiceSeedMethod()
    {
        // Arrange
      _movieServiceMock
            .Setup(x => x.SeedMoviesAsync())
          .Returns(Task.CompletedTask);

        // Act
 var result = await _controller.SeedMovies();

        // Assert
    _movieServiceMock.Verify(x => x.SeedMoviesAsync(), Times.Once);
    }

    [Fact]
    public async Task SeedMovies_ReturnsOkResult()
    {
        // Arrange
  _movieServiceMock
  .Setup(x => x.SeedMoviesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SeedMovies();

     // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SeedMovies_ReturnsSuccessMessage()
    {
        // Arrange
        _movieServiceMock
            .Setup(x => x.SeedMoviesAsync())
      .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SeedMovies();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task SeedMovies_WhenServiceThrowsException_AllowsExceptionToPropagate()
    {
      // Arrange
        _movieServiceMock
      .Setup(x => x.SeedMoviesAsync())
    .ThrowsAsync(new Exception("Seeding failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await _controller.SeedMovies());
    }

    [Fact]
    public async Task SeedMovies_OnlyCallsServiceOnce()
    {
        // Arrange
        _movieServiceMock
  .Setup(x => x.SeedMoviesAsync())
            .Returns(Task.CompletedTask);

   // Act
      await _controller.SeedMovies();
  await _controller.SeedMovies();

   // Assert
  _movieServiceMock.Verify(x => x.SeedMoviesAsync(), Times.Exactly(2));
    }

    #endregion
}
