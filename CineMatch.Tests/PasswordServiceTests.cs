using CineMatchAPI.Application.Services;
using FluentAssertions;
using Xunit;

namespace CineMatch.Tests.Services;

public class PasswordServiceTests
{
    private readonly IPasswordService _passwordService;

    public PasswordServiceTests()
    {
        _passwordService = new PasswordService();
    }

    #region HashPassword Tests

    [Fact]
    public void HashPassword_WithValidPassword_ReturnsNonEmptyHash()
    {
        // Arrange
        var password = "MySecurePassword123";

        // Act
        var hash = _passwordService.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void HashPassword_WithSamePassword_ReturnsDifferentHashes()
    {
        // Arrange
        var password = "SamePassword";

        // Act
        var hash1 = _passwordService.HashPassword(password);
        var hash2 = _passwordService.HashPassword(password);

        // Assert - BCrypt uses salt, so hashes should be different
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void HashPassword_WithDifferentPasswords_ReturnsDifferentHashes()
    {
        // Act
        var hash1 = _passwordService.HashPassword("Password1");
        var hash2 = _passwordService.HashPassword("Password2");

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void HashPassword_WithShortPassword_StillWorks()
    {
        // Arrange
        var shortPassword = "abc";

        // Act
        var hash = _passwordService.HashPassword(shortPassword);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().StartWith("$2");  // BCrypt hash format
    }

    [Fact]
    public void HashPassword_WithLongPassword_StillWorks()
    {
        // Arrange
        var longPassword = new string('x', 100);

        // Act
        var hash = _passwordService.HashPassword(longPassword);

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void HashPassword_WithSpecialCharacters_WorksCorrectly()
    {
        // Arrange
        var password = "P@ssw0rd!#$%^&*()";

        // Act
        var hash = _passwordService.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("password")]
    [InlineData("Password123")]
    [InlineData("P@ssw0rd!")]
    [InlineData("")]
    [InlineData("   ")]
    public void HashPassword_WithVariousInputs_ReturnsValidHash(string password)
    {
        // Act
        var hash = _passwordService.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region VerifyPassword Tests - Positive

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "CorrectPassword123";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithSamePasswordMultipleTimes_AlwaysReturnsTrue()
    {
        // Arrange
        var password = "TestPassword";
        var hash = _passwordService.HashPassword(password);

        // Act & Assert
        _passwordService.VerifyPassword(password, hash).Should().BeTrue();
        _passwordService.VerifyPassword(password, hash).Should().BeTrue();
        _passwordService.VerifyPassword(password, hash).Should().BeTrue();
    }

    [Theory]
    [InlineData("simple")]
    [InlineData("Complex123!@#")]
    [InlineData("With Spaces")]
    [InlineData("UPPERCASE")]
    [InlineData("lowercase")]
    public void VerifyPassword_WithVariousPasswords_VerifiesCorrectly(string password)
    {
        // Arrange
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region VerifyPassword Tests - Negative

    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        var correctPassword = "CorrectPassword";
        var wrongPassword = "WrongPassword";
        var hash = _passwordService.HashPassword(correctPassword);

        // Act
        var result = _passwordService.VerifyPassword(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithCaseChange_ReturnsFalse()
    {
        // Arrange
        var password = "Password";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword("PASSWORD", hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithExtraSpaces_ReturnsFalse()
    {
        // Arrange
        var password = "Password";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword("Password ", hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithEmptyPasswordAgainstHash_ReturnsFalse()
    {
        // Arrange
        var password = "SomePassword";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword("", hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithSlightlyDifferentPassword_ReturnsFalse()
    {
        // Arrange
        var password = "MyPassword123";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword("MyPassword124", hash);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Security Tests

    [Fact]
    public void HashPassword_DifferentExecutions_ProduceDifferentSalts()
    {
        // Arrange
        var password = "SameInput";

        // Act
        var hash1 = _passwordService.HashPassword(password);
        var hash2 = _passwordService.HashPassword(password);

        // Assert - Salts should be different even for same password
        hash1.Should().NotBe(hash2);
        _passwordService.VerifyPassword(password, hash1).Should().BeTrue();
        _passwordService.VerifyPassword(password, hash2).Should().BeTrue();
    }

    [Fact]
    public void HashPassword_OutputLength_IsConsistent()
    {
        // Arrange & Act
        var hash1 = _passwordService.HashPassword("short");
        var hash2 = _passwordService.HashPassword(new string('x', 100));

        // Assert - BCrypt hashes should be 60 characters
        hash1.Length.Should().Be(60);
        hash2.Length.Should().Be(60);
    }

    [Fact]
    public void VerifyPassword_WithInvalidHash_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword";
        var invalidHash = "not-a-valid-bcrypt-hash";

        // Act
        var result = _passwordService.VerifyPassword(password, invalidHash);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}