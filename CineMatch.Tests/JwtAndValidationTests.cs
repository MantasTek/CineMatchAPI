using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Application.Services;
using CineMatchAPI.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace CineMatch.Tests.Services;

#region JwtTokenService Tests

public class JwtTokenServiceTests
{
    private readonly IJwtTokenService _tokenService;
    private readonly IConfiguration _configuration;

    public JwtTokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            {"Jwt:Key", "ThisIsAVeryLongSecretKeyForTestingPurposes123456789"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        _tokenService = new JwtTokenService(_configuration);
    }

    [Fact]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        var token = _tokenService.GenerateToken("user123", "test@example.com");

        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_ReturnsValidJwtFormat()
    {
        var token = _tokenService.GenerateToken("user123", "test@example.com");

        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void GenerateToken_ContainsUserId()
    {
        var token = _tokenService.GenerateToken("user123", "test@example.com");

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

        userIdClaim.Should().NotBeNull();
        userIdClaim!.Value.Should().Be("user123");
    }

    [Fact]
    public void GenerateToken_ContainsEmail()
    {
        var token = _tokenService.GenerateToken("user123", "test@example.com");

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

        emailClaim.Should().NotBeNull();
        emailClaim!.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void GenerateToken_ContainsIssuer()
    {
        var token = _tokenService.GenerateToken("user123", "test@example.com");

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("TestIssuer");
    }

    [Fact]
    public void GenerateToken_ContainsAudience()
    {
        var token = _tokenService.GenerateToken("user123", "test@example.com");

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Audiences.Should().Contain("TestAudience");
    }

    [Fact]
    public void GenerateToken_HasExpirationTime()
    {
        var token = _tokenService.GenerateToken("user123", "test@example.com");

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_WithDifferentUserIds_GeneratesDifferentTokens()
    {
        var token1 = _tokenService.GenerateToken("user1", "test1@example.com");
        var token2 = _tokenService.GenerateToken("user2", "test2@example.com");

        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateToken_WithDifferentEmails_GeneratesDifferentTokens()
    {
        var token1 = _tokenService.GenerateToken("user123", "email1@test.com");
        var token2 = _tokenService.GenerateToken("user123", "email2@test.com");

        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateToken_CalledTwiceWithSameInputs_GeneratesDifferentTokens()
    {
        var token1 = _tokenService.GenerateToken("user123", "test@example.com");
        System.Threading.Thread.Sleep(1000); // Ensure different timestamps
        var token2 = _tokenService.GenerateToken("user123", "test@example.com");

        token1.Should().NotBe(token2);
    }

    [Theory]
    [InlineData("user1", "test1@example.com")]
    [InlineData("alice", "alice@test.com")]
    [InlineData("123-456", "user@domain.co.uk")]
    public void GenerateToken_WithVariousInputs_CreatesValidTokens(string userId, string email)
    {
        var token = _tokenService.GenerateToken(userId, email);

        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId);
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == email);
    }

    [Fact]
    public void GenerateToken_HasIssuedAtTime()
    {
        var beforeGeneration = DateTime.UtcNow;
        var token = _tokenService.GenerateToken("user123", "test@example.com");
        var afterGeneration = DateTime.UtcNow;

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.ValidFrom.Should().BeOnOrAfter(beforeGeneration.AddSeconds(-5));
        jwtToken.ValidFrom.Should().BeOnOrBefore(afterGeneration.AddSeconds(5));
    }

    [Fact]
    public void GenerateToken_ExpiresInFuture()
    {
        var token = _tokenService.GenerateToken("user123", "test@example.com");

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.ValidTo.Should().BeAfter(DateTime.UtcNow.AddMinutes(10));
    }
}

#endregion

#region Entity Validation Tests

public class EntityValidationTests
{
    [Fact]
    public void User_DefaultId_IsNotEmpty()
    {
        var user = new User();

        user.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void User_DefaultPreferences_IsEmptyArray()
    {
        var user = new User();

        user.Preferences.Should().Be("[]");
    }

    [Fact]
    public void User_DefaultCreatedAt_IsRecent()
    {
        var user = new User();

        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void User_DefaultUpdatedAt_IsRecent()
    {
        var user = new User();

        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Match_DefaultId_IsNotEmpty()
    {
        var match = new Match();

        match.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Match_DefaultMatchedAt_IsRecent()
    {
        var match = new Match();

        match.MatchedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Message_DefaultIsRead_IsFalse()
    {
        var message = new Message();

        message.IsRead.Should().BeFalse();
    }

    [Fact]
    public void Message_DefaultReadAt_IsNull()
    {
        var message = new Message();

        message.ReadAt.Should().BeNull();
    }

    [Fact]
    public void Swipe_DefaultId_IsNotEmpty()
    {
        var swipe = new Swipe();

        swipe.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Swipe_DefaultSwipedAt_IsRecent()
    {
        var swipe = new Swipe();

        swipe.SwipedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Movie_CanSetAllProperties()
    {
        var movie = new Movie
        {
            Id = "test",
            Title = "Test Movie",
            Genre = "Action",
            Rating = 8.5,
            Year = 2023,
            Runtime = 120,
            ImageUrl = "url",
            Description = "description"
        };

        movie.Id.Should().Be("test");
        movie.Title.Should().Be("Test Movie");
        movie.Genre.Should().Be("Action");
        movie.Rating.Should().Be(8.5);
        movie.Year.Should().Be(2023);
        movie.Runtime.Should().Be(120);
    }
}

#endregion

#region DTO Validation Tests

public class DtoValidationTests
{
    [Fact]
    public void LoginDto_CanBeCreated()
    {
        var dto = new LoginDto("email@test.com", "password");

        dto.Email.Should().Be("email@test.com");
        dto.Password.Should().Be("password");
    }

    [Fact]
    public void RegisterDto_CanBeCreated()
    {
        var dto = new RegisterDto("Name", "email@test.com", "password", "City");

        dto.Name.Should().Be("Name");
        dto.Email.Should().Be("email@test.com");
        dto.Password.Should().Be("password");
        dto.Location.Should().Be("City");
    }

    [Fact]
    public void SwipeDto_WithLike_SetsLikedTrue()
    {
        var dto = new SwipeDto("movie1", true);

        dto.MovieId.Should().Be("movie1");
        dto.Liked.Should().BeTrue();
    }

    [Fact]
    public void SwipeDto_WithDislike_SetsLikedFalse()
    {
        var dto = new SwipeDto("movie1", false);

        dto.MovieId.Should().Be("movie1");
        dto.Liked.Should().BeFalse();
    }

    [Fact]
    public void UpdatePreferencesDto_CanBeCreated()
    {
        var genres = new List<string> { "Action", "Comedy" };
        var dto = new UpdatePreferencesDto(genres, "medium");

        dto.Genres.Should().HaveCount(2);
        dto.MovieLength.Should().Be("medium");
    }

    [Fact]
    public void SendMessageDto_CanBeCreated()
    {
        var dto = new SendMessageDto("match1", "Hello!");

        dto.MatchId.Should().Be("match1");
        dto.Text.Should().Be("Hello!");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("medium")]
    [InlineData("long")]
    public void UpdatePreferencesDto_AcceptsValidLengths(string length)
    {
        var dto = new UpdatePreferencesDto(new List<string> { "Action" }, length);

        dto.MovieLength.Should().Be(length);
    }

    [Theory]
    [InlineData("Action")]
    [InlineData("Comedy")]
    [InlineData("Drama")]
    [InlineData("Sci-Fi")]
    public void UpdatePreferencesDto_AcceptsVariousGenres(string genre)
    {
        var dto = new UpdatePreferencesDto(new List<string> { genre }, "medium");

        dto.Genres.Should().Contain(genre);
    }
}

#endregion

#region Password Service Additional Tests

public class PasswordServiceAdditionalTests
{
    private readonly IPasswordService _passwordService;

    public PasswordServiceAdditionalTests()
    {
        _passwordService = new PasswordService();
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        var password = "MyPassword123";
        var hash = _passwordService.HashPassword(password);

        var result = _passwordService.VerifyPassword(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        var password = "CorrectPassword";
        var hash = _passwordService.HashPassword(password);

        var result = _passwordService.VerifyPassword("WrongPassword", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_IsCaseSensitive()
    {
        var password = "Password123";
        var hash = _passwordService.HashPassword(password);

        var result = _passwordService.VerifyPassword("password123", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithDifferentHash_ReturnsFalse()
    {
        var hash1 = _passwordService.HashPassword("Password1");
        var hash2 = _passwordService.HashPassword("Password2");

        var result = _passwordService.VerifyPassword("Password1", hash2);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("password")]
    [InlineData("P@ssw0rd!")]
    [InlineData("VeryLongPasswordWith123NumbersAndSpecialChars!@#")]
    public void HashAndVerify_WithVariousPasswords_WorksCorrectly(string password)
    {
        var hash = _passwordService.HashPassword(password);

        var result = _passwordService.VerifyPassword(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void HashPassword_WithUnicodeCharacters_WorksCorrectly()
    {
        var password = "Пароль123密码";
        var hash = _passwordService.HashPassword(password);

        var result = _passwordService.VerifyPassword(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void HashPassword_WithEmojis_WorksCorrectly()
    {
        var password = "Password😀123🔒";
        var hash = _passwordService.HashPassword(password);

        var result = _passwordService.VerifyPassword(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithEmptyHash_ReturnsFalse()
    {
        var result = _passwordService.VerifyPassword("password", "");

        result.Should().BeFalse();
    }
}

#endregion