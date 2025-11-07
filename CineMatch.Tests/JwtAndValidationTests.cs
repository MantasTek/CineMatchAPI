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

    // FIXED: This test now checks for the correct claim type that JwtTokenService actually uses
    [Fact]
    public void GenerateToken_ContainsUserId()
    {
        var token = _tokenService.GenerateToken("user123", "test@example.com");

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        // The service uses JwtRegisteredClaimNames.Sub which appears as "sub" in the token
        // We check for both "sub" and ClaimTypes.NameIdentifier for compatibility
        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => 
            c.Type == JwtRegisteredClaimNames.Sub || 
            c.Type == ClaimTypes.NameIdentifier);

        userIdClaim.Should().NotBeNull();
        userIdClaim!.Value.Should().Be("user123");
    }

    // FIXED: This test now checks for the correct claim type for email
    [Fact]
    public void GenerateToken_ContainsEmail()
    {
        var token = _tokenService.GenerateToken("user123", "test@example.com");

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        // The service uses JwtRegisteredClaimNames.Email which appears as "email" in the token
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => 
            c.Type == JwtRegisteredClaimNames.Email || 
            c.Type == ClaimTypes.Email);

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

    // FIXED: This test now checks for the correct claim types
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
        
        // Check for the correct claim types that the service actually uses
        jwtToken.Claims.Should().Contain(c => 
            (c.Type == JwtRegisteredClaimNames.Sub || c.Type == ClaimTypes.NameIdentifier) && 
            c.Value == userId);
        jwtToken.Claims.Should().Contain(c => 
            (c.Type == JwtRegisteredClaimNames.Email || c.Type == ClaimTypes.Email) && 
            c.Value == email);
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

    #region MatchDto Tests
 
    [Fact]
    public void MatchDto_CanBeCreated_WithAllProperties()
    {
        // Arrange
        var matchId = "match123";
        var otherUser = new UserDto("user456", "John Doe", "john@test.com", "New York", null, null, new List<string> { "Action" }, "medium");
   var movie = new MovieDto("movie789", "The Matrix", "Sci-Fi", 8.7, 1999, "http://image.url", "A hacker discovers reality", 136);
        var matchedAt = DateTime.UtcNow;

        // Act
        var matchDto = new MatchDto(matchId, otherUser, movie, matchedAt);

  // Assert
        matchDto.Should().NotBeNull();
        matchDto.Id.Should().Be(matchId);
        matchDto.OtherUser.Should().Be(otherUser);
        matchDto.Movie.Should().Be(movie);
    matchDto.MatchedAt.Should().Be(matchedAt);
}

    [Fact]
    public void MatchDto_Id_ReturnsCorrectValue()
    {
        // Arrange
        var matchId = "unique-match-id";
        var matchDto = CreateTestMatchDto(matchId);

        // Act & Assert
   matchDto.Id.Should().Be(matchId);
    }

[Fact]
    public void MatchDto_OtherUser_ReturnsCorrectUser()
    {
        // Arrange
        var user = new UserDto("user1", "Alice", "alice@test.com", "Seattle", "Bio", "avatar.jpg", new List<string> { "Drama" }, "long");
        var matchDto = new MatchDto("match1", user, CreateTestMovieDto(), DateTime.UtcNow);

        // Act & Assert
        matchDto.OtherUser.Should().Be(user);
        matchDto.OtherUser.Id.Should().Be("user1");
  matchDto.OtherUser.Name.Should().Be("Alice");
        matchDto.OtherUser.Email.Should().Be("alice@test.com");
    }

    [Fact]
    public void MatchDto_Movie_ReturnsCorrectMovie()
    {
        // Arrange
var movie = new MovieDto("movie1", "Inception", "Thriller", 8.8, 2010, "http://test.com/img", "Dream within a dream", 148);
   var matchDto = new MatchDto("match1", CreateTestUserDto(), movie, DateTime.UtcNow);

      // Act & Assert
        matchDto.Movie.Should().Be(movie);
        matchDto.Movie.Id.Should().Be("movie1");
  matchDto.Movie.Title.Should().Be("Inception");
      matchDto.Movie.Genre.Should().Be("Thriller");
   matchDto.Movie.Rating.Should().Be(8.8);
        matchDto.Movie.Year.Should().Be(2010);
    }

    [Fact]
    public void MatchDto_MatchedAt_ReturnsCorrectDateTime()
    {
        // Arrange
        var matchedAt = new DateTime(2025, 1, 6, 12, 30, 0, DateTimeKind.Utc);
        var matchDto = CreateTestMatchDto("match1", matchedAt);

        // Act & Assert
     matchDto.MatchedAt.Should().Be(matchedAt);
    }

    [Fact]
public void MatchDto_WithRecentMatchedAt_IsRecent()
{
    // Arrange
        var matchDto = new MatchDto("match1", CreateTestUserDto(), CreateTestMovieDto(), DateTime.UtcNow);

        // Act & Assert
        matchDto.MatchedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MatchDto_WithPastMatchedAt_IsInPast()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-7);
   var matchDto = CreateTestMatchDto("match1", pastDate);

        // Act & Assert
      matchDto.MatchedAt.Should().BeBefore(DateTime.UtcNow);
        matchDto.MatchedAt.Should().BeCloseTo(pastDate, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("match-1")]
    [InlineData("abc123")]
    [InlineData("550e8400-e29b-41d4-a716-446655440000")]
    public void MatchDto_WithVariousIds_StoresIdCorrectly(string matchId)
    {
        // Arrange & Act
   var matchDto = CreateTestMatchDto(matchId);

        // Assert
   matchDto.Id.Should().Be(matchId);
    }

    [Fact]
    public void MatchDto_WithDifferentUsers_StoresDifferentUserData()
    {
        // Arrange
        var user1 = new UserDto("user1", "Alice", "alice@test.com", "Seattle", null, null, new List<string>(), "short");
        var user2 = new UserDto("user2", "Bob", "bob@test.com", "Portland", null, null, new List<string>(), "medium");
        
        var match1 = new MatchDto("match1", user1, CreateTestMovieDto(), DateTime.UtcNow);
        var match2 = new MatchDto("match2", user2, CreateTestMovieDto(), DateTime.UtcNow);

  // Act & Assert
     match1.OtherUser.Should().Be(user1);
        match2.OtherUser.Should().Be(user2);
        match1.OtherUser.Should().NotBe(match2.OtherUser);
    }

    [Fact]
    public void MatchDto_WithDifferentMovies_StoresDifferentMovieData()
    {
        // Arrange
        var movie1 = new MovieDto("m1", "Movie A", "Action", 7.5, 2020, "url1", "Desc1", 90);
      var movie2 = new MovieDto("m2", "Movie B", "Comedy", 6.8, 2021, "url2", "Desc2", 100);
        
        var match1 = new MatchDto("match1", CreateTestUserDto(), movie1, DateTime.UtcNow);
        var match2 = new MatchDto("match2", CreateTestUserDto(), movie2, DateTime.UtcNow);

  // Act & Assert
 match1.Movie.Should().Be(movie1);
        match2.Movie.Should().Be(movie2);
        match1.Movie.Should().NotBe(match2.Movie);
    }

    [Fact]
    public void MatchDto_Equality_WithSameValues_AreEqual()
    {
        // Arrange
        var matchId = "match1";
        var user = CreateTestUserDto();
        var movie = CreateTestMovieDto();
        var matchedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var match1 = new MatchDto(matchId, user, movie, matchedAt);
        var match2 = new MatchDto(matchId, user, movie, matchedAt);

     // Act & Assert
        match1.Should().Be(match2);
match1.Equals(match2).Should().BeTrue();
    }

    [Fact]
    public void MatchDto_Equality_WithDifferentIds_AreNotEqual()
    {
        // Arrange
      var user = CreateTestUserDto();
  var movie = CreateTestMovieDto();
        var matchedAt = DateTime.UtcNow;

     var match1 = new MatchDto("match1", user, movie, matchedAt);
        var match2 = new MatchDto("match2", user, movie, matchedAt);

        // Act & Assert
        match1.Should().NotBe(match2);
    }

    [Fact]
    public void MatchDto_GetHashCode_WithSameValues_ReturnsSameHashCode()
    {
 // Arrange
        var user = CreateTestUserDto();
   var movie = CreateTestMovieDto();
   var matchedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

   var match1 = new MatchDto("match1", user, movie, matchedAt);
      var match2 = new MatchDto("match1", user, movie, matchedAt);

    // Act & Assert
        match1.GetHashCode().Should().Be(match2.GetHashCode());
    }

    [Fact]
    public void MatchDto_ToString_ContainsRelevantInformation()
    {
        // Arrange
        var matchDto = CreateTestMatchDto("match123");

        // Act
        var result = matchDto.ToString();

        // Assert
        result.Should().Contain("match123");
        result.Should().NotBeNullOrEmpty();
    }

[Fact]
public void MatchDto_Deconstruction_ReturnsAllProperties()
{
    // Arrange
    var matchId = "match1";
    var user = CreateTestUserDto();
    var movie = CreateTestMovieDto();
    var matchedAt = DateTime.UtcNow;
    var matchDto = new MatchDto(matchId, user, movie, matchedAt);

    // Act
    var (id, otherUser, returnedMovie, returnedMatchedAt) = matchDto;

 // Assert
        id.Should().Be(matchId);
        otherUser.Should().Be(user);
    returnedMovie.Should().Be(movie);
    returnedMatchedAt.Should().Be(matchedAt);
    }

    [Fact]
    public void MatchDto_WithNullUserPreferences_HandlesGracefully()
    {
     // Arrange
        var user = new UserDto("user1", "Test", "test@test.com", "City", null, null, null, null);
        var matchDto = new MatchDto("match1", user, CreateTestMovieDto(), DateTime.UtcNow);

        // Act & Assert
        matchDto.OtherUser.Preferences.Should().BeNull();
        matchDto.OtherUser.MovieLength.Should().BeNull();
    }

    [Fact]
    public void MatchDto_WithUserWithBioAndAvatar_StoresAllUserData()
    {
        // Arrange
        var user = new UserDto("user1", "Jane", "jane@test.com", "Boston", "Love movies!", "http://avatar.com/jane.jpg", new List<string> { "Action", "Comedy" }, "long");
        var matchDto = new MatchDto("match1", user, CreateTestMovieDto(), DateTime.UtcNow);

    // Act & Assert
    matchDto.OtherUser.Bio.Should().Be("Love movies!");
        matchDto.OtherUser.AvatarUrl.Should().Be("http://avatar.com/jane.jpg");
        matchDto.OtherUser.Preferences.Should().HaveCount(2);
        matchDto.OtherUser.MovieLength.Should().Be("long");
    }

    [Fact]
    public void MatchDto_WithCompleteMovieData_StoresAllMovieProperties()
    {
        // Arrange
   var movie = new MovieDto("movie1", "The Shawshank Redemption", "Drama", 9.3, 1994, "http://image.url/shawshank.jpg", "Two imprisoned men bond", 142);
        var matchDto = new MatchDto("match1", CreateTestUserDto(), movie, DateTime.UtcNow);

        // Act & Assert
    matchDto.Movie.Id.Should().Be("movie1");
 matchDto.Movie.Title.Should().Be("The Shawshank Redemption");
     matchDto.Movie.Genre.Should().Be("Drama");
        matchDto.Movie.Rating.Should().Be(9.3);
        matchDto.Movie.Year.Should().Be(1994);
 matchDto.Movie.ImageUrl.Should().Be("http://image.url/shawshank.jpg");
        matchDto.Movie.Description.Should().Be("Two imprisoned men bond");
        matchDto.Movie.Runtime.Should().Be(142);
    }

    [Theory]
    [InlineData(2020, 90)]
    [InlineData(2021, 120)]
    [InlineData(2022, 150)]
    [InlineData(1999, 180)]
    public void MatchDto_WithDifferentMovieYearsAndRuntimes_StoresCorrectly(int year, int runtime)
    {
        // Arrange
  var movie = new MovieDto("m1", "Test Movie", "Action", 7.0, year, "url", "desc", runtime);
        var matchDto = new MatchDto("match1", CreateTestUserDto(), movie, DateTime.UtcNow);

        // Act & Assert
     matchDto.Movie.Year.Should().Be(year);
        matchDto.Movie.Runtime.Should().Be(runtime);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(5.5)]
    [InlineData(7.8)]
    [InlineData(9.9)]
    [InlineData(10.0)]
    public void MatchDto_WithDifferentMovieRatings_StoresCorrectly(double rating)
    {
        // Arrange
        var movie = new MovieDto("m1", "Test", "Action", rating, 2020, "url", "desc", 100);
        var matchDto = new MatchDto("match1", CreateTestUserDto(), movie, DateTime.UtcNow);

   // Act & Assert
 matchDto.Movie.Rating.Should().Be(rating);
    }

    #endregion

    // Helper methods for MatchDto tests
    private static UserDto CreateTestUserDto(string id = "testUser")
    {
      return new UserDto(id, "Test User", "test@test.com", "Test City", null, null, new List<string>(), "medium");
    }

    private static MovieDto CreateTestMovieDto(string id = "testMovie")
    {
   return new MovieDto(id, "Test Movie", "Action", 8.0, 2024, "http://test.com/image.jpg", "Test Description", 120);
    }

    private static MatchDto CreateTestMatchDto(string id = "testMatch", DateTime? matchedAt = null)
    {
        return new MatchDto(id, CreateTestUserDto(), CreateTestMovieDto(), matchedAt ?? DateTime.UtcNow);
 }
}

#endregion