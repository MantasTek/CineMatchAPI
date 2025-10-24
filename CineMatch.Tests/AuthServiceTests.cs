using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Application.Services;
using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CineMatch.Tests.Services;

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

    #region Register Tests - Positive Scenarios

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsAuthResponse()
    {
        // Arrange
        var dto = new RegisterDto("John Doe", "john@example.com", "password123", "Stockholm");
        _userRepositoryMock.Setup(x => x.EmailExistsAsync(dto.Email)).ReturnsAsync(false);
        _passwordServiceMock.Setup(x => x.HashPassword(dto.Password)).Returns("hashedPassword");
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(It.IsAny<string>(), dto.Email)).Returns("token123");
        _userRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be("token123");
        result.User.Email.Should().Be(dto.Email);
        result.User.Name.Should().Be(dto.Name);
        result.User.Location.Should().Be(dto.Location);
    }

    [Fact]
    public async Task RegisterAsync_CallsPasswordHashingOnce()
    {
        // Arrange
        var dto = new RegisterDto("Jane", "jane@test.com", "pass", "Oslo");
        _userRepositoryMock.Setup(x => x.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _passwordServiceMock.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed");
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<string>())).Returns("token");
        _userRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<User>())).ReturnsAsync(new User());

        // Act
        await _authService.RegisterAsync(dto);

        // Assert
        _passwordServiceMock.Verify(x => x.HashPassword(dto.Password), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_CreatesUserInRepository()
    {
        // Arrange
        var dto = new RegisterDto("Test User", "test@test.com", "password", "Berlin");
        _userRepositoryMock.Setup(x => x.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _passwordServiceMock.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed");
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<string>())).Returns("token");
        _userRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<User>())).ReturnsAsync(new User());

        // Act
        await _authService.RegisterAsync(dto);

        // Assert
        _userRepositoryMock.Verify(x => x.CreateAsync(It.Is<User>(u => 
            u.Email == dto.Email && 
            u.Name == dto.Name && 
            u.Location == dto.Location
        )), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_GeneratesJwtToken()
    {
        // Arrange
        var dto = new RegisterDto("User", "user@test.com", "pass", "City");
        _userRepositoryMock.Setup(x => x.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _passwordServiceMock.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed");
        _userRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<User>())).ReturnsAsync(new User { Id = "123", Email = dto.Email });
        _jwtTokenServiceMock.Setup(x => x.GenerateToken("123", dto.Email)).Returns("generatedToken");

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        _jwtTokenServiceMock.Verify(x => x.GenerateToken(It.IsAny<string>(), dto.Email), Times.Once);
        result!.Token.Should().Be("generatedToken");
    }

    #endregion

    #region Register Tests - Negative Scenarios

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsNull()
    {
        // Arrange
        var dto = new RegisterDto("John", "existing@example.com", "password", "Stockholm");
        _userRepositoryMock.Setup(x => x.EmailExistsAsync(dto.Email)).ReturnsAsync(true);

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_DoesNotCreateUser()
    {
        // Arrange
        var dto = new RegisterDto("John", "existing@test.com", "pass", "City");
        _userRepositoryMock.Setup(x => x.EmailExistsAsync(dto.Email)).ReturnsAsync(true);

        // Act
        await _authService.RegisterAsync(dto);

        // Assert
        _userRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_DoesNotHashPassword()
    {
        // Arrange
        var dto = new RegisterDto("User", "exists@test.com", "password", "Place");
        _userRepositoryMock.Setup(x => x.EmailExistsAsync(dto.Email)).ReturnsAsync(true);

        // Act
        await _authService.RegisterAsync(dto);

        // Assert
        _passwordServiceMock.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_DoesNotGenerateToken()
    {
        // Arrange
        var dto = new RegisterDto("User", "duplicate@test.com", "pass", "City");
        _userRepositoryMock.Setup(x => x.EmailExistsAsync(dto.Email)).ReturnsAsync(true);

        // Act
        await _authService.RegisterAsync(dto);

        // Assert
        _jwtTokenServiceMock.Verify(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Login Tests - Positive Scenarios

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var dto = new LoginDto("user@test.com", "password123");
        var user = new User 
        { 
            Id = "user1", 
            Email = dto.Email, 
            Name = "Test User",
            PasswordHash = "hashedPassword",
            Location = "Stockholm"
        };
        
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _passwordServiceMock.Setup(x => x.VerifyPassword(dto.Password, user.PasswordHash)).Returns(true);
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(user.Id, user.Email)).Returns("validToken");

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be("validToken");
        result.User.Email.Should().Be(dto.Email);
        result.User.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task LoginAsync_VerifiesPasswordAgainstHash()
    {
        // Arrange
        var dto = new LoginDto("test@test.com", "myPassword");
        var user = new User { Id = "1", Email = dto.Email, PasswordHash = "storedHash", Name = "User", Location = "City" };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _passwordServiceMock.Setup(x => x.VerifyPassword(dto.Password, user.PasswordHash)).Returns(true);
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<string>())).Returns("token");

        // Act
        await _authService.LoginAsync(dto);

        // Assert
        _passwordServiceMock.Verify(x => x.VerifyPassword(dto.Password, user.PasswordHash), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithCorrectPassword_GeneratesToken()
    {
        // Arrange
        var dto = new LoginDto("valid@test.com", "correctPass");
        var user = new User { Id = "userId", Email = dto.Email, PasswordHash = "hash", Name = "Name", Location = "Loc" };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _passwordServiceMock.Setup(x => x.VerifyPassword(dto.Password, user.PasswordHash)).Returns(true);
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(user.Id, user.Email)).Returns("jwtToken");

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        _jwtTokenServiceMock.Verify(x => x.GenerateToken(user.Id, user.Email), Times.Once);
    }

    #endregion

    #region Login Tests - Negative Scenarios

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
    public async Task LoginAsync_WithWrongPassword_ReturnsNull()
    {
        // Arrange
        var dto = new LoginDto("user@test.com", "wrongPassword");
        var user = new User { Id = "1", Email = dto.Email, PasswordHash = "correctHash", Name = "User", Location = "City" };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _passwordServiceMock.Setup(x => x.VerifyPassword(dto.Password, user.PasswordHash)).Returns(false);

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_DoesNotGenerateToken()
    {
        // Arrange
        var dto = new LoginDto("user@test.com", "wrongPass");
        var user = new User { Id = "1", Email = dto.Email, PasswordHash = "hash", Name = "User", Location = "City" };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _passwordServiceMock.Setup(x => x.VerifyPassword(dto.Password, user.PasswordHash)).Returns(false);

        // Act
        await _authService.LoginAsync(dto);

        // Assert
        _jwtTokenServiceMock.Verify(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_DoesNotVerifyPassword()
    {
        // Arrange
        var dto = new LoginDto("nobody@test.com", "password");
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);

        // Act
        await _authService.LoginAsync(dto);

        // Assert
        _passwordServiceMock.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region GetUserById Tests

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ReturnsUserDto()
    {
        // Arrange
        var userId = "user123";
        var user = new User 
        { 
            Id = userId, 
            Name = "John Doe", 
            Email = "john@test.com",
            Location = "Stockholm",
            Preferences = "[\"Action\", \"Comedy\"]",
            MovieLength = "medium"
        };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _authService.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Name.Should().Be("John Doe");
        result.Email.Should().Be("john@test.com");
        result.Preferences.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var userId = "nonexistent";
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _authService.GetUserByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByIdAsync_ParsesPreferencesCorrectly()
    {
        // Arrange
        var user = new User 
        { 
            Id = "1", 
            Name = "User", 
            Email = "user@test.com",
            Location = "City",
            Preferences = "[\"Drama\", \"Thriller\", \"Horror\"]"
        };
        _userRepositoryMock.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(user);

        // Act
        var result = await _authService.GetUserByIdAsync("1");

        // Assert
        result!.Preferences.Should().Contain(new[] { "Drama", "Thriller", "Horror" });
    }

    [Fact]
    public async Task GetUserByIdAsync_WithEmptyPreferences_ReturnsEmptyList()
    {
        // Arrange
        var user = new User 
        { 
            Id = "1", 
            Name = "User", 
            Email = "user@test.com",
            Location = "City",
            Preferences = "[]"
        };
        _userRepositoryMock.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(user);

        // Act
        var result = await _authService.GetUserByIdAsync("1");

        // Assert
        result!.Preferences.Should().BeEmpty();
    }

    #endregion
}