using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Application.Services;
using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CineMatch.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ISwipeRepository> _swipeRepositoryMock;
    private readonly Mock<IMatchRepository> _matchRepositoryMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _swipeRepositoryMock = new Mock<ISwipeRepository>();
        _matchRepositoryMock = new Mock<IMatchRepository>();
        _userService = new UserService(
            _userRepositoryMock.Object,
            _swipeRepositoryMock.Object,
            _matchRepositoryMock.Object
        );
    }

    #region GetUserAsync Tests

    [Fact]
    public async Task GetUserAsync_WithExistingUser_ReturnsUserDto()
    {
        var userId = "user1";
        var user = CreateUser(userId, "Test User", "test@example.com");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var result = await _userService.GetUserAsync(userId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Name.Should().Be("Test User");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetUserAsync_WithNonExistentUser_ReturnsNull()
    {
        var userId = "nonexistent";
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        var result = await _userService.GetUserAsync(userId);

        result.Should().BeNull();
    }

    #endregion

    #region UpdatePreferencesAsync Tests

    [Fact]
    public async Task UpdatePreferencesAsync_WithValidData_UpdatesPreferences()
    {
        var userId = "user1";
        var user = CreateUser(userId, "User", "user@test.com");
        var dto = new UpdatePreferencesDto(new List<string> { "Action", "Comedy" }, "medium");
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync(user);

        var result = await _userService.UpdatePreferencesAsync(userId, dto);

        result.Should().BeTrue();
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.Is<User>(u => 
            u.MovieLength == "medium"
        )), Times.Once);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WithNonExistentUser_ReturnsFalse()
    {
        var userId = "nonexistent";
        var dto = new UpdatePreferencesDto(new List<string> { "Action" }, "medium");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        var result = await _userService.UpdatePreferencesAsync(userId, dto);

        result.Should().BeFalse();
    }

    #endregion

    #region ResetUserDataAsync Tests

    [Fact]
    public async Task ResetUserDataAsync_DeletesSwipesAndMatches()
    {
        var userId = "user1";
        var user = CreateUser(userId, "User", "user@test.com");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync(user);
        _swipeRepositoryMock.Setup(x => x.DeleteAllByUserIdAsync(userId)).Returns(Task.CompletedTask);
        _matchRepositoryMock.Setup(x => x.DeleteAllByUserIdAsync(userId)).Returns(Task.CompletedTask);

        var result = await _userService.ResetUserDataAsync(userId);

        result.Should().BeTrue();
        _swipeRepositoryMock.Verify(x => x.DeleteAllByUserIdAsync(userId), Times.Once);
        _matchRepositoryMock.Verify(x => x.DeleteAllByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task ResetUserDataAsync_WithNonExistentUser_ReturnsFalse()
    {
        var userId = "nonexistent";
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        var result = await _userService.ResetUserDataAsync(userId);

        result.Should().BeFalse();
        _swipeRepositoryMock.Verify(x => x.DeleteAllByUserIdAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Helper Methods

    private User CreateUser(string id, string name, string email)
    {
        return new User
        {
            Id = id,
            Name = name,
            Email = email,
            PasswordHash = "hash",
            Preferences = "[]",
            MovieLength = "medium"
        };
    }

    #endregion
}