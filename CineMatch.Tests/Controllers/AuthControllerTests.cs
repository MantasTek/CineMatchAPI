using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Application.Services;
using CineMatchAPI.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CineMatch.Tests.Controllers;

/// <summary>
/// Tests for AuthController endpoints.
/// Verifies HTTP response codes and data flow.
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithValidData_ReturnsOkWithAuthResponse()
    {
        // Arrange
        var dto = new RegisterDto("John", "john@test.com", "password", "Stockholm");
        var authResponse = CreateAuthResponse("user1", "john@test.com");
        _authServiceMock.Setup(x => x.RegisterAsync(dto)).ReturnsAsync(authResponse);

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(authResponse);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        var dto = new RegisterDto("User", "existing@test.com", "password", "City");
        _authServiceMock.Setup(x => x.RegisterAsync(dto)).ReturnsAsync((AuthResponseDto?)null);

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Register_CallsAuthService()
    {
        // Arrange
        var dto = new RegisterDto("Test", "test@test.com", "pass", "Location");
        _authServiceMock.Setup(x => x.RegisterAsync(dto)).ReturnsAsync((AuthResponseDto?)null);

        // Act
        await _controller.Register(dto);

        // Assert
        _authServiceMock.Verify(x => x.RegisterAsync(dto), Times.Once);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithAuthResponse()
    {
        // Arrange
        var dto = new LoginDto("user@test.com", "password");
        var authResponse = CreateAuthResponse("user1", "user@test.com");
        _authServiceMock.Setup(x => x.LoginAsync(dto)).ReturnsAsync(authResponse);

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(authResponse);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var dto = new LoginDto("user@test.com", "wrongPassword");
        _authServiceMock.Setup(x => x.LoginAsync(dto)).ReturnsAsync((AuthResponseDto?)null);

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_CallsAuthService()
    {
        // Arrange
        var dto = new LoginDto("test@test.com", "password");
        _authServiceMock.Setup(x => x.LoginAsync(dto)).ReturnsAsync((AuthResponseDto?)null);

        // Act
        await _controller.Login(dto);

        // Assert
        _authServiceMock.Verify(x => x.LoginAsync(dto), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static AuthResponseDto CreateAuthResponse(string userId, string email)
    {
        var userDto = new UserDto(
            userId,
            "Test User",
            email,
            "Stockholm",
            null,
            null,
            new List<string>(),
            "Medium"
        );
        return new AuthResponseDto("test_token", userDto);
    }

    #endregion
}