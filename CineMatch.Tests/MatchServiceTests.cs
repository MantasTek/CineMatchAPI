using CineMatchAPI.Application.Services;
using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace CineMatch.Tests.Services;

/// <summary>
/// Comprehensive unit tests for MatchService.
/// Tests cover match retrieval, potential match finding, and helper methods.
/// This addresses the critical 0% coverage gap identified in the analysis.
/// </summary>
public class MatchServiceTests
{
    private readonly Mock<IMatchRepository> _matchRepositoryMock;
    private readonly Mock<ISwipeRepository> _swipeRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IMovieRepository> _movieRepositoryMock;
    private readonly MatchService _matchService;

    public MatchServiceTests()
    {
        _matchRepositoryMock = new Mock<IMatchRepository>();
        _swipeRepositoryMock = new Mock<ISwipeRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _movieRepositoryMock = new Mock<IMovieRepository>();
        
        _matchService = new MatchService(
            _matchRepositoryMock.Object,
            _swipeRepositoryMock.Object,
            _userRepositoryMock.Object,
            _movieRepositoryMock.Object
        );
    }

    #region GetUserMatchesAsync - Positive Scenarios

    [Fact]
    public async Task GetUserMatchesAsync_WithValidUserId_ReturnsUserMatches()
    {
        // Arrange
        var userId = "user1";
        var matches = new List<Moq.Match>
        {
            CreateMatch("match1", userId, "user2", "movie1"),
            CreateMatch("match2", userId, "user3", "movie2")
        };
        
        _matchRepositoryMock
    .Setup(x => x.GetByUserIdAsync(userId))
    .ReturnsAsync((IEnumerable<Match>)matches);

        // Act
        var result = await _matchService.GetUserMatchesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Id.Should().Be("match1");
    }

    [Fact]
    public async Task GetUserMatchesAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var userId = "user1";
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Match>());

        // Act
        var result = await _matchService.GetUserMatchesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserMatchesAsync_ReturnsOtherUserInMatch()
    {
        // Arrange
        var userId = "user1";
        var otherUserId = "user2";
        var match = CreateMatch("match1", userId, otherUserId, "movie1");
        
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Match> { match });

        // Act
        var result = await _matchService.GetUserMatchesAsync(userId);

        // Assert
        var matchDto = result.First();
        matchDto.OtherUser.Id.Should().Be(otherUserId);
        matchDto.OtherUser.Name.Should().Be("User2");
    }

    [Fact]
    public async Task GetUserMatchesAsync_WhenUserIsUser2_ReturnsUser1AsOtherUser()
    {
        // Arrange
        var userId = "user2";
        var otherUserId = "user1";
        var match = CreateMatch("match1", otherUserId, userId, "movie1");
        
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Match> { match });

        // Act
        var result = await _matchService.GetUserMatchesAsync(userId);

        // Assert
        var matchDto = result.First();
        matchDto.OtherUser.Id.Should().Be(otherUserId);
        matchDto.OtherUser.Name.Should().Be("User1");
    }

    [Fact]
    public async Task GetUserMatchesAsync_IncludesMovieDetails()
    {
        // Arrange
        var userId = "user1";
        var match = CreateMatch("match1", userId, "user2", "movie1");
        
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Match> { match });

        // Act
        var result = await _matchService.GetUserMatchesAsync(userId);

        // Assert
        var matchDto = result.First();
        matchDto.Movie.Should().NotBeNull();
        matchDto.Movie.Id.Should().Be("movie1");
        matchDto.Movie.Title.Should().Be("Movie1");
        matchDto.Movie.Genre.Should().Be("Action");
    }

    [Fact]
    public async Task GetUserMatchesAsync_IncludesMatchedAtTimestamp()
    {
        // Arrange
        var userId = "user1";
        var expectedDate = new DateTime(2024, 10, 15, 10, 30, 0, DateTimeKind.Utc);
        var match = CreateMatch("match1", userId, "user2", "movie1");
        match.MatchedAt = expectedDate;
        
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Match> { match });

        // Act
        var result = await _matchService.GetUserMatchesAsync(userId);

        // Assert
        var matchDto = result.First();
        matchDto.MatchedAt.Should().Be(expectedDate);
    }

    #endregion

    #region GetUserMatchesAsync - Negative Scenarios

    [Fact]
    public async Task GetUserMatchesAsync_SkipsMatchesWithNullMovie()
    {
        // Arrange
        var userId = "user1";
        var validMatch = CreateMatch("match1", userId, "user2", "movie1");
        var invalidMatch = CreateMatch("match2", userId, "user3", "movie2");
        invalidMatch.Movie = null;
        
            _matchRepositoryMock
        .Setup(x => x.GetByUserIdAsync(userId))
        .ReturnsAsync((IEnumerable<Match>)matches);

        // Act
        var result = await _matchService.GetUserMatchesAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be("match1");
    }

    [Fact]
    public async Task GetUserMatchesAsync_SkipsMatchesWithNullUser1()
    {
        // Arrange
        var userId = "user1";
        var validMatch = CreateMatch("match1", userId, "user2", "movie1");
        var invalidMatch = CreateMatch("match2", userId, "user3", "movie2");
        invalidMatch.User1 = null;
        
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Match> { validMatch, invalidMatch });

        // Act
        var result = await _matchService.GetUserMatchesAsync(userId);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUserMatchesAsync_SkipsMatchesWithNullUser2()
    {
        // Arrange
        var userId = "user1";
        var validMatch = CreateMatch("match1", userId, "user2", "movie1");
        var invalidMatch = CreateMatch("match2", userId, "user3", "movie2");
        invalidMatch.User2 = null;
        
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Match> { validMatch, invalidMatch });

        // Act
        var result = await _matchService.GetUserMatchesAsync(userId);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUserMatchesAsync_HandlesMultipleInvalidMatches()
    {
        // Arrange
        var userId = "user1";
        var match1 = CreateMatch("match1", userId, "user2", "movie1");
        match1.Movie = null;
        
        var match2 = CreateMatch("match2", userId, "user3", "movie2");
        match2.User1 = null;
        
        var match3 = CreateMatch("match3", userId, "user4", "movie3");
        match3.User2 = null;
        
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Match> { match1, match2, match3 });

        // Act
        var result = await _matchService.GetUserMatchesAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region FindPotentialMatchesAsync - Positive Scenarios

    [Fact]
    public async Task FindPotentialMatchesAsync_WithCommonLikes_ReturnsPotentialMatches()
    {
        // Arrange
        var userId = "user1";
        var otherUserId = "user2";
        var commonMovieId = "movie1";
        
        SetupPotentialMatchScenario(userId, otherUserId, commonMovieId);

        // Act
        var result = await _matchService.FindPotentialMatchesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        var match = result.First();
        match.OtherUser.Id.Should().Be(otherUserId);
        match.Movie.Id.Should().Be(commonMovieId);
    }

    [Fact]
    public async Task FindPotentialMatchesAsync_WithNoLikes_ReturnsEmpty()
    {
        // Arrange
        var userId = "user1";
        _swipeRepositoryMock
            .Setup(x => x.GetUserLikesAsync(userId))
            .ReturnsAsync(new List<Swipe>());

        // Act
        var result = await _matchService.FindPotentialMatchesAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindPotentialMatchesAsync_ExcludesAlreadyMatchedUsers()
    {
        // Arrange
        var userId = "user1";
        var alreadyMatchedUserId = "user2";
        var newUserId = "user3";
        var commonMovieId = "movie1";
        
        // User1 likes movie1
        var userLikes = new List<Swipe>
        {
            CreateSwipe(userId, commonMovieId, true)
        };
        _swipeRepositoryMock
            .Setup(x => x.GetUserLikesAsync(userId))
            .ReturnsAsync(userLikes);
        
        // User1 already has a match with user2
        var existingMatch = CreateMatch("existingMatch", userId, alreadyMatchedUserId, commonMovieId);
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Match> { existingMatch });
        
        // Both user2 (already matched) and user3 (new) like movie1
        var allUsers = new List<User>
        {
            CreateUser(userId, "User1"),
            CreateUser(alreadyMatchedUserId, "User2"),
            CreateUser(newUserId, "User3")
        };
        _userRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(allUsers);
        
        // Setup likes for both users
        _swipeRepositoryMock
            .Setup(x => x.GetUserLikesAsync(alreadyMatchedUserId))
            .ReturnsAsync(new List<Swipe> { CreateSwipe(alreadyMatchedUserId, commonMovieId, true) });
        
        _swipeRepositoryMock
            .Setup(x => x.GetUserLikesAsync(newUserId))
            .ReturnsAsync(new List<Swipe> { CreateSwipe(newUserId, commonMovieId, true) });
        
        var movie = CreateMovie(commonMovieId, "Movie1");
        _movieRepositoryMock
            .Setup(x => x.GetByIdAsync(commonMovieId))
            .ReturnsAsync(movie);

        // Act
        var result = await _matchService.FindPotentialMatchesAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        result.First().OtherUser.Id.Should().Be(newUserId);
        result.First().OtherUser.Id.Should().NotBe(alreadyMatchedUserId);
    }

    [Fact]
    public async Task FindPotentialMatchesAsync_FindsFirstCommonMovie()
    {
        // Arrange
        var userId = "user1";
        var otherUserId = "user2";
        var movie1Id = "movie1";
        var movie2Id = "movie2";
        
        // User1 likes multiple movies
        var userLikes = new List<Swipe>
        {
            CreateSwipe(userId, movie1Id, true),
            CreateSwipe(userId, movie2Id, true)
        };
        _swipeRepositoryMock
            .Setup(x => x.GetUserLikesAsync(userId))
            .ReturnsAsync(userLikes);
        
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Match>());
        
        var users = new List<User>
        {
            CreateUser(userId, "User1"),
            CreateUser(otherUserId, "User2")
        };
        _userRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);
        
        // User2 also likes both movies
        var otherUserLikes = new List<Swipe>
        {
            CreateSwipe(otherUserId, movie1Id, true),
            CreateSwipe(otherUserId, movie2Id, true)
        };
        _swipeRepositoryMock
            .Setup(x => x.GetUserLikesAsync(otherUserId))
            .ReturnsAsync(otherUserLikes);
        
        var movie = CreateMovie(movie1Id, "Movie1");
        _movieRepositoryMock
            .Setup(x => x.GetByIdAsync(movie1Id))
            .ReturnsAsync(movie);

        // Act
        var result = await _matchService.FindPotentialMatchesAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        result.First().Movie.Id.Should().Be(movie1Id);
    }

    [Fact]
    public async Task FindPotentialMatchesAsync_ExcludesSelf()
    {
        // Arrange
        var userId = "user1";
        var movieId = "movie1";
        
        var userLikes = new List<Swipe>
        {
            CreateSwipe(userId, movieId, true)
        };
        _swipeRepositoryMock
            .Setup(x => x.GetUserLikesAsync(userId))
            .ReturnsAsync(userLikes);
        
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Match>());
        
        // Only the user themselves exists
        var users = new List<User>
        {
            CreateUser(userId, "User1")
        };
        _userRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);

        // Act
        var result = await _matchService.FindPotentialMatchesAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region FindPotentialMatchesAsync - Negative Scenarios

    [Fact]
    public async Task FindPotentialMatchesAsync_WithNoCommonLikes_ReturnsEmpty()
    {
        // Arrange
        var userId = "user1";
        var otherUserId = "user2";
        
        var userLikes = new List<Swipe>
        {
            CreateSwipe(userId, "movie1", true)
        };
        _swipeRepositoryMock
            .Setup(x => x.GetUserLikesAsync(userId))
            .ReturnsAsync(userLikes);
        
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Match>());
        
        var users = new List<User>
        {
            CreateUser(userId, "User1"),
            CreateUser(otherUserId, "User2")
        };
        _userRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);
        
        // User2 likes different movie
        var otherUserLikes = new List<Swipe>
        {
            CreateSwipe(otherUserId, "movie2", true)
        };
        _swipeRepositoryMock
            .Setup(x => x.GetUserLikesAsync(otherUserId))
            .ReturnsAsync(otherUserLikes);

        // Act
        var result = await _matchService.FindPotentialMatchesAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindPotentialMatchesAsync_WithNullMovie_SkipsMatch()
    {
        // Arrange
        var userId = "user1";
        var otherUserId = "user2";
        var movieId = "movie1";
        
        SetupBasicPotentialMatchData(userId, otherUserId, movieId);
        
        // Movie doesn't exist
        _movieRepositoryMock
            .Setup(x => x.GetByIdAsync(movieId))
            .ReturnsAsync((Movie?)null);

        // Act
        var result = await _matchService.FindPotentialMatchesAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindPotentialMatchesAsync_WithMultipleUsersAndNoCommonMovies_ReturnsEmpty()
    {
        // Arrange
        var userId = "user1";
        
        var userLikes = new List<Swipe>
        {
            CreateSwipe(userId, "movie1", true)
        };
        _swipeRepositoryMock
            .Setup(x => x.GetUserLikesAsync(userId))
            .ReturnsAsync(userLikes);
        
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Match>());
        
        var users = new List<User>
        {
            CreateUser(userId, "User1"),
            CreateUser("user2", "User2"),
            CreateUser("user3", "User3"),
            CreateUser("user4", "User4")
        };
        _userRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);
        
        // All other users like different movies
        _swipeRepositoryMock
            .Setup(x => x.GetUserLikesAsync("user2"))
            .ReturnsAsync(new List<Swipe> { CreateSwipe("user2", "movie2", true) });
        
        _swipeRepositoryMock
            .Setup(x => x.GetUserLikesAsync("user3"))
            .ReturnsAsync(new List<Swipe> { CreateSwipe("user3", "movie3", true) });
        
        _swipeRepositoryMock
            .Setup(x => x.GetUserLikesAsync("user4"))
            .ReturnsAsync(new List<Swipe> { CreateSwipe("user4", "movie4", true) });

        // Act
        var result = await _matchService.FindPotentialMatchesAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private Match CreateMatch(string matchId, string user1Id, string user2Id, string movieId)
    {
        return new Match
        {
            Id = matchId,
            User1Id = user1Id,
            User2Id = user2Id,
            MovieId = movieId,
            MatchedAt = DateTime.UtcNow,
            User1 = CreateUser(user1Id, $"User{user1Id.Last()}"),
            User2 = CreateUser(user2Id, $"User{user2Id.Last()}"),
            Movie = CreateMovie(movieId, $"Movie{movieId.Last()}")
        };
    }

    private User CreateUser(string id, string name)
    {
        return new User
        {
            Id = id,
            Name = name,
            Email = $"{name.ToLower()}@test.com",
            Location = "Test City",
            Bio = $"Bio for {name}",
            AvatarUrl = $"https://example.com/{id}.jpg",
            Preferences = "[]",
            MovieLength = "medium",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private Movie CreateMovie(string id, string title)
    {
        return new Movie
        {
            Id = id,
            Title = title,
            Genre = "Action",
            Rating = 8.5,
            Year = 2024,
            ImageUrl = $"https://example.com/{id}.jpg",
            Description = $"Description for {title}",
            Runtime = 120
        };
    }

    private Swipe CreateSwipe(string userId, string movieId, bool liked)
    {
        return new Swipe
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            MovieId = movieId,
            Liked = liked,
            SwipedAt = DateTime.UtcNow
        };
    }

    private void SetupPotentialMatchScenario(string userId, string otherUserId, string commonMovieId)
    {
        SetupBasicPotentialMatchData(userId, otherUserId, commonMovieId);
        
        var movie = CreateMovie(commonMovieId, "CommonMovie");
        _movieRepositoryMock
            .Setup(x => x.GetByIdAsync(commonMovieId))
            .ReturnsAsync(movie);
    }

    private void SetupBasicPotentialMatchData(string userId, string otherUserId, string commonMovieId)
    {
        var userLikes = new List<Swipe>
        {
            CreateSwipe(userId, commonMovieId, true)
        };
        _swipeRepositoryMock
            .Setup(x => x.GetUserLikesAsync(userId))
            .ReturnsAsync(userLikes);
        
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Match>());
        
        var users = new List<User>
        {
            CreateUser(userId, "User1"),
            CreateUser(otherUserId, "User2")
        };
        _userRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);
        
        var otherUserLikes = new List<Swipe>
        {
            CreateSwipe(otherUserId, commonMovieId, true)
        };
        _swipeRepositoryMock
            .Setup(x => x.GetUserLikesAsync(otherUserId))
            .ReturnsAsync(otherUserLikes);
    }

    #endregion
}