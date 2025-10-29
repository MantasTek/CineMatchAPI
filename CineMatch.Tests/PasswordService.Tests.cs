using CineMatchAPI.Application.Services;
using CineMatchAPI.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace CineMatch.Tests.Services;

/// <summary>
/// Tests for PasswordService - verifies password hashing and verification.
/// </summary>
public class PasswordServiceTests
{
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _passwordService = new PasswordService();
    }

    #region HashPassword Tests

    [Fact]
    public void HashPassword_CreatesNonNullHash()
    {
        // Arrange
        var password = "testPassword123";

        // Act
        var hash = _passwordService.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void HashPassword_CreatesUniqueHashesForSamePassword()
    {
        // Arrange
        var password = "samePassword";

        // Act
        var hash1 = _passwordService.HashPassword(password);
        var hash2 = _passwordService.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void HashPassword_WithDifferentPasswords_CreatesDifferentHashes()
    {
        // Arrange
        var password1 = "password1";
        var password2 = "password2";

        // Act
        var hash1 = _passwordService.HashPassword(password1);
        var hash2 = _passwordService.HashPassword(password2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    #endregion

    #region VerifyPassword Tests

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "correctPassword";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var correctPassword = "correctPassword";
        var wrongPassword = "wrongPassword";
        var hash = _passwordService.HashPassword(correctPassword);

        // Act
        var result = _passwordService.VerifyPassword(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithEmptyPassword_ReturnsFalse()
    {
        // Arrange
        var password = "password";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword("", hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_CaseSensitive()
    {
        // Arrange
        var password = "Password123";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword("password123", hash);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}