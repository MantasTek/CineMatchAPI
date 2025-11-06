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
/// Additional tests for UserController to improve coverage
/// Tests user preferences retrieval and edge cases
/// </summary>
public class UserControllerExtendedTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly UserController _controller;
    private const string TestUserId = "user123";

    public UserControllerExtendedTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _controller = new UserController(_userServiceMock.Object);
        SetupUserClaims(TestUserId);
    }

    #region GetPreferences Tests

    [Fact]
    public async Task GetPreferences_WithValidUser_ReturnsOkWithPreferences()
    {
        // Arrange
    var userDto = CreateUserDto(TestUserId, new List<string> { "Action", "Comedy" }, "Medium");
      _userServiceMock
            .Setup(x => x.GetUserAsync(TestUserId))
    .ReturnsAsync(userDto);

        // Act
        var result = await _controller.GetPreferences();

     // Assert
      var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPreferences_WithNonExistentUser_ReturnsNotFound()
    {
    // Arrange
        _userServiceMock
            .Setup(x => x.GetUserAsync(TestUserId))
      .ReturnsAsync((UserDto?)null);

        // Act
        var result = await _controller.GetPreferences();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetPreferences_ReturnsGenresAndMovieLength()
    {
        // Arrange
      var genres = new List<string> { "Action", "Drama", "Sci-Fi" };
        var movieLength = "Long";
        var userDto = CreateUserDto(TestUserId, genres, movieLength);
        
        _userServiceMock
     .Setup(x => x.GetUserAsync(TestUserId))
            .ReturnsAsync(userDto);

      // Act
        var result = await _controller.GetPreferences();

      // Assert
 var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPreferences_UsesCorrectUserId()
    {
  // Arrange
        var userDto = CreateUserDto(TestUserId, new List<string> { "Action" }, "Medium");
        _userServiceMock
            .Setup(x => x.GetUserAsync(TestUserId))
            .ReturnsAsync(userDto);

   // Act
      await _controller.GetPreferences();

 // Assert
        _userServiceMock.Verify(x => x.GetUserAsync(TestUserId), Times.Once);
    }

    [Fact]
    public async Task GetPreferences_WithEmptyPreferences_ReturnsEmptyList()
    {
        // Arrange
        var userDto = CreateUserDto(TestUserId, new List<string>(), "Medium");
_userServiceMock
     .Setup(x => x.GetUserAsync(TestUserId))
    .ReturnsAsync(userDto);

    // Act
        var result = await _controller.GetPreferences();

        // Assert
    var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    #endregion

    #region GetCurrentUser Edge Cases

    [Fact]
  public async Task GetCurrentUser_WithNullUser_ReturnsNotFound()
    {
        // Arrange
  _userServiceMock
            .Setup(x => x.GetUserAsync(TestUserId))
 .ReturnsAsync((UserDto?)null);

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetCurrentUser_WithValidUser_ReturnsUserData()
    {
        // Arrange
        var userDto = CreateUserDto(TestUserId, new List<string> { "Action" }, "Medium");
        _userServiceMock
         .Setup(x => x.GetUserAsync(TestUserId))
            .ReturnsAsync(userDto);

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUser = okResult.Value.Should().BeOfType<UserDto>().Subject;
 returnedUser.Id.Should().Be(TestUserId);
    }

  #endregion

    #region UpdatePreferences Edge Cases

    [Fact]
    public async Task UpdatePreferences_WithValidData_ReturnsOk()
    {
        // Arrange
        var dto = new UpdatePreferencesDto(new List<string> { "Action", "Comedy" }, "Medium");
        _userServiceMock
            .Setup(x => x.UpdatePreferencesAsync(TestUserId, dto))
  .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdatePreferences(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
  public async Task UpdatePreferences_WhenUpdateFails_ReturnsBadRequest()
    {
   // Arrange
        var dto = new UpdatePreferencesDto(new List<string> { "Action" }, "Medium");
        _userServiceMock
     .Setup(x => x.UpdatePreferencesAsync(TestUserId, dto))
      .ReturnsAsync(false);

 // Act
    var result = await _controller.UpdatePreferences(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdatePreferences_WithEmptyGenres_CallsService()
    {
     // Arrange
        var dto = new UpdatePreferencesDto(new List<string>(), "Medium");
        _userServiceMock
            .Setup(x => x.UpdatePreferencesAsync(TestUserId, dto))
      .ReturnsAsync(true);

        // Act
        await _controller.UpdatePreferences(dto);

        // Assert
_userServiceMock.Verify(x => x.UpdatePreferencesAsync(TestUserId, dto), Times.Once);
    }

    [Fact]
    public async Task UpdatePreferences_WithMultipleGenres_CallsService()
  {
        // Arrange
      var dto = new UpdatePreferencesDto(
       new List<string> { "Action", "Comedy", "Drama", "Sci-Fi" }, 
            "Long"
        );
        _userServiceMock
 .Setup(x => x.UpdatePreferencesAsync(TestUserId, dto))
        .ReturnsAsync(true);

        // Act
        await _controller.UpdatePreferences(dto);

        // Assert
  _userServiceMock.Verify(x => x.UpdatePreferencesAsync(TestUserId, dto), Times.Once);
    }

    #endregion

    #region ResetUserData Edge Cases

    [Fact]
    public async Task ResetUserData_WhenSuccessful_ReturnsOk()
    {
        // Arrange
        _userServiceMock
    .Setup(x => x.ResetUserDataAsync(TestUserId))
   .ReturnsAsync(true);

        // Act
        var result = await _controller.ResetUserData();

  // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResetUserData_WhenFails_ReturnsBadRequest()
    {
      // Arrange
        _userServiceMock
     .Setup(x => x.ResetUserDataAsync(TestUserId))
            .ReturnsAsync(false);

// Act
        var result = await _controller.ResetUserData();

        // Assert
    result.Should().BeOfType<BadRequestObjectResult>();
    }

 [Fact]
    public async Task ResetUserData_CallsServiceWithCorrectUserId()
    {
        // Arrange
        _userServiceMock
     .Setup(x => x.ResetUserDataAsync(TestUserId))
     .ReturnsAsync(true);

        // Act
        await _controller.ResetUserData();

        // Assert
    _userServiceMock.Verify(x => x.ResetUserDataAsync(TestUserId), Times.Once);
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

    private UserDto CreateUserDto(string userId, List<string> preferences, string movieLength)
    {
    return new UserDto(
        userId,
        "Test User",
    $"{userId}@test.com",
  "Stockholm",
     null,
  null,
preferences,
    movieLength
        );
    }

    #endregion
}
