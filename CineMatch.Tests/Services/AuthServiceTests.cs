using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Application.Services;
using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CineMatch.Tests.Services;

/// <summary>
/// Unit tests for AuthService following SOLID principles.
/// Tests are organized by functionality and scenario (positive/negative).
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordService> _passwordServiceMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordServiceMock = new Mock<IPasswordService>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _authService = new AuthService(
            _userRepositoryMock.Object,
            _passwordServiceMock.Object,
            _jwtTokenServiceMock.Object
        );
    }

    #region Register - Positive Scenarios

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsAuthResponse()
    {
        // Arrange
        var dto = new RegisterDto("John Doe", "john@example.com", "password123", "Stockholm");
        SetupSuccessfulRegistration(dto);

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be("test_token");
        result.User.Email.Should().Be(dto.Email);
        result.User.Name.Should().Be(dto.Name);
        result.User.Location.Should().Be(dto.Location);
    }

    [Fact]
    public async Task RegisterAsync_HashesPassword()
    {
        // Arrange
        var dto = new RegisterDto("Jane Smith", "jane@test.com", "myPassword", "Oslo");
        SetupSuccessfulRegistration(dto);

        // Act
        await _authService.RegisterAsync(dto);

        // Assert
        _passwordServiceMock.Verify(x => x.HashPassword(dto.Password), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_CreatesUserWithCorrectData()
    {
        // Arrange
        var dto = new RegisterDto("Test User", "test@test.com", "password", "Berlin");
        SetupSuccessfulRegistration(dto);

        // Act
        await _authService.RegisterAsync(dto);

        // Assert
        _userRepositoryMock.Verify(x => x.CreateAsync(It.Is<User>(u =>
            u.Email == dto.Email &&
            u.Name == dto.Name &&
            u.Location == dto.Location &&
            u.PasswordHash == "hashed_password"
        )), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_GeneratesJwtToken()
    {
        // Arrange
        var dto = new RegisterDto("User", "user@example.com", "pass", "City");
        SetupSuccessfulRegistration(dto);

        // Act
        await _authService.RegisterAsync(dto);

        // Assert
        _jwtTokenServiceMock.Verify(x => x.GenerateToken(It.IsAny<string>(), dto.Email), Times.Once);
    }

    #endregion

    #region Register - Negative Scenarios

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsNull()
    {
        // Arrange
        var dto = new RegisterDto("User", "existing@test.com", "password", "City");
        _userRepositoryMock.Setup(x => x.EmailExistsAsync(dto.Email)).ReturnsAsync(true);

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_DoesNotHashPassword()
    {
        // Arrange
        var dto = new RegisterDto("User", "duplicate@test.com", "pass", "City");
        _userRepositoryMock.Setup(x => x.EmailExistsAsync(dto.Email)).ReturnsAsync(true);

        // Act
        await _authService.RegisterAsync(dto);

        // Assert
        _passwordServiceMock.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_DoesNotCreateUser()
    {
        // Arrange
        var dto = new RegisterDto("User", "exists@test.com", "pass", "City");
        _userRepositoryMock.Setup(x => x.EmailExistsAsync(dto.Email)).ReturnsAsync(true);

        // Act
        await _authService.RegisterAsync(dto);

        // Assert
        _userRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    #endregion

    #region Login - Positive Scenarios

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var dto = new LoginDto("user@test.com", "password123");
        var user = CreateTestUser("user1", dto.Email, "Test User");
        SetupSuccessfulLogin(dto, user);

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be("test_token");
        result.User.Email.Should().Be(dto.Email);
        result.User.Id.Should().Be(user.Id);
        result.User.Name.Should().Be(user.Name);
    }

    [Fact]
    public async Task LoginAsync_VerifiesPassword()
    {
        // Arrange
        var dto = new LoginDto("test@test.com", "myPassword");
        var user = CreateTestUser("1", dto.Email, "User");
        SetupSuccessfulLogin(dto, user);

        // Act
        await _authService.LoginAsync(dto);

        // Assert
        _passwordServiceMock.Verify(x => x.VerifyPassword(dto.Password, user.PasswordHash), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_GeneratesToken()
    {
        // Arrange
        var dto = new LoginDto("valid@test.com", "correctPass");
        var user = CreateTestUser("userId", dto.Email, "Name");
        SetupSuccessfulLogin(dto, user);

        // Act
        await _authService.LoginAsync(dto);

        // Assert
        _jwtTokenServiceMock.Verify(x => x.GenerateToken(user.Id, user.Email), Times.Once);
    }

    #endregion

    #region Login - Negative Scenarios

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ReturnsNull()
    {
        // Arrange
        var dto = new LoginDto("nonexistent@test.com", "password");
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var dto = new LoginDto("user@test.com", "wrongPassword");
        var user = CreateTestUser("1", dto.Email, "User");
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _passwordServiceMock.Setup(x => x.VerifyPassword(dto.Password, user.PasswordHash)).Returns(false);

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_DoesNotGenerateToken()
    {
        // Arrange
        var dto = new LoginDto("user@test.com", "wrongPass");
        var user = CreateTestUser("1", dto.Email, "User");
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _passwordServiceMock.Setup(x => x.VerifyPassword(dto.Password, user.PasswordHash)).Returns(false);

        // Act
        await _authService.LoginAsync(dto);

        // Assert
        _jwtTokenServiceMock.Verify(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region GetUserById Tests

    [Fact]
    public async Task GetUserByIdAsync_WithExistingUser_ReturnsUserDto()
    {
        // Arrange
        var user = CreateTestUser("user123", "test@example.com", "Test User");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);

        // Act
        var result = await _authService.GetUserByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.Name.Should().Be(user.Name);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.GetByIdAsync("nonexistent")).ReturnsAsync((User?)null);

        // Act
        var result = await _authService.GetUserByIdAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods (DRY Principle)

    private void SetupSuccessfulRegistration(RegisterDto dto)
    {
        _userRepositoryMock.Setup(x => x.EmailExistsAsync(dto.Email)).ReturnsAsync(false);
        _passwordServiceMock.Setup(x => x.HashPassword(dto.Password)).Returns("hashed_password");
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(It.IsAny<string>(), dto.Email)).Returns("test_token");
        _userRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);
    }

    private void SetupSuccessfulLogin(LoginDto dto, User user)
    {
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _passwordServiceMock.Setup(x => x.VerifyPassword(dto.Password, user.PasswordHash)).Returns(true);
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(user.Id, user.Email)).Returns("test_token");
    }

    private static User CreateTestUser(string id, string email, string name)
    {
        return new User
        {
            Id = id,
            Email = email,
            Name = name,
            PasswordHash = "hashed_password",
            Location = "Stockholm",
            Preferences = "[]",
            MovieLength = "Medium"
        };
    }

    #endregion
}