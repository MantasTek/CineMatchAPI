using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Application.Services;
using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;
using MatchEntity = CineMatchAPI.Domain.Entities.Match;

namespace CineMatch.Tests.Services;

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

    #region GetUserMatchesAsync Tests

    [Fact]
    public async Task GetUserMatchesAsync_WithValidUserId_ReturnsUserMatches()
    {
        var userId = "user1";
        var matches = new List<MatchEntity>
        {
            CreateMatch("match1", userId, "user2", "movie1"),
            CreateMatch("match2", userId, "user3", "movie2")
        };
        
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(matches);

        var result = await _matchService.GetUserMatchesAsync(userId);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserMatchesAsync_WithNoMatches_ReturnsEmptyList()
    {
        var userId = "user1";
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<MatchEntity>());

        var result = await _matchService.GetUserMatchesAsync(userId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserMatchesAsync_SkipsMatchesWithNullMovie()
    {
        var userId = "user1";
        var validMatch = CreateMatch("match1", userId, "user2", "movie1");
        var invalidMatch = CreateMatch("match2", userId, "user3", "movie2");
        invalidMatch.Movie = null!;
        
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<MatchEntity> { validMatch, invalidMatch });

        var result = await _matchService.GetUserMatchesAsync(userId);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUserMatchesAsync_SkipsMatchesWithNullUsers()
    {
        var userId = "user1";
        var validMatch = CreateMatch("match1", userId, "user2", "movie1");
        var invalidMatch = CreateMatch("match2", userId, "user3", "movie2");
        invalidMatch.User1 = null!;
        
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<MatchEntity> { validMatch, invalidMatch });

        var result = await _matchService.GetUserMatchesAsync(userId);

        result.Should().HaveCount(1);
    }

    #endregion

    #region FindPotentialMatchesAsync Tests

    [Fact]
    public async Task FindPotentialMatchesAsync_WithNoLikes_ReturnsEmpty()
    {
        var userId = "user1";
        _swipeRepositoryMock
            .Setup(x => x.GetUserLikesAsync(userId))
            .ReturnsAsync(new List<Swipe>());

        var result = await _matchService.FindPotentialMatchesAsync(userId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindPotentialMatchesAsync_WithUserLikes_SearchesForPotentials()
    {
        var userId = "user1";
        var userSwipes = new List<Swipe>
        {
            new Swipe { UserId = userId, MovieId = "movie1", Liked = true }
        };
        
        _swipeRepositoryMock
            .Setup(x => x.GetUserLikesAsync(userId))
            .ReturnsAsync(userSwipes);
            
        _matchRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<MatchEntity>());

        var result = await _matchService.FindPotentialMatchesAsync(userId);

        _swipeRepositoryMock.Verify(x => x.GetUserLikesAsync(userId), Times.Once);
    }

    #endregion

    #region Helper Methods

    private MatchEntity CreateMatch(string matchId, string user1Id, string user2Id, string movieId)
    {
        return new MatchEntity
        {
            Id = matchId,
            User1Id = user1Id,
            User2Id = user2Id,
            MovieId = movieId,
            MatchedAt = DateTime.UtcNow,
            User1 = new User
            {
                Id = user1Id,
                Name = $"User{user1Id}",
                Email = $"{user1Id}@test.com",
                PasswordHash = "hash",
                Preferences = "[]"
            },
            User2 = new User
            {
                Id = user2Id,
                Name = $"User{user2Id}",
                Email = $"{user2Id}@test.com",
                PasswordHash = "hash",
                Preferences = "[]"
            },
            Movie = new Movie
            {
                Id = movieId,
                Title = "Test Movie",
                Genre = "Action",
                Rating = 8.0,
                Year = 2024,
                Runtime = 120,
                ImageUrl = "http://test.com/image.jpg",
                Description = "Test"
            }
        };
    }

    #endregion
}