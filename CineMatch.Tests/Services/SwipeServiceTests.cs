using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Application.Services;
using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;
using MatchEntity = CineMatchAPI.Domain.Entities.Match;

namespace CineMatch.Tests.Services;

/// <summary>
/// Comprehensive tests for SwipeService
/// Tests swipe functionality, match creation, and movie starring
/// </summary>
public class SwipeServiceTests
{
    private readonly Mock<ISwipeRepository> _swipeRepositoryMock;
    private readonly Mock<IMovieRepository> _movieRepositoryMock;
    private readonly Mock<IMatchRepository> _matchRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly SwipeService _swipeService;

    public SwipeServiceTests()
    {
    _swipeRepositoryMock = new Mock<ISwipeRepository>();
        _movieRepositoryMock = new Mock<IMovieRepository>();
        _matchRepositoryMock = new Mock<IMatchRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        
_swipeService = new SwipeService(
    _swipeRepositoryMock.Object,
         _movieRepositoryMock.Object,
            _matchRepositoryMock.Object,
            _userRepositoryMock.Object
  );
    }

    #region SwipeMovieAsync Tests

    [Fact]
    public async Task SwipeMovieAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var userId = "nonexistent-user";
        var dto = new SwipeDto("movie1", true);
        
        _userRepositoryMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(false);

      // Act
   var result = await _swipeService.SwipeMovieAsync(userId, dto);

 // Assert
        result.Success.Should().BeFalse();
        result.Matched.Should().BeFalse();
        result.MatchId.Should().BeNull();
        _userRepositoryMock.Verify(x => x.ExistsAsync(userId), Times.Once);
 }

    [Fact]
    public async Task SwipeMovieAsync_WithNonExistentMovie_ReturnsFailure()
    {
    // Arrange
    var userId = "user1";
        var dto = new SwipeDto("nonexistent-movie", true);
        
        _userRepositoryMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
        _movieRepositoryMock.Setup(x => x.ExistsAsync(dto.MovieId)).ReturnsAsync(false);

        // Act
        var result = await _swipeService.SwipeMovieAsync(userId, dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Matched.Should().BeFalse();
        result.MatchId.Should().BeNull();
        _movieRepositoryMock.Verify(x => x.ExistsAsync(dto.MovieId), Times.Once);
    }

    [Fact]
    public async Task SwipeMovieAsync_WithAlreadySwipedMovie_ReturnsFailure()
    {
    // Arrange
        var userId = "user1";
        var movieId = "movie1";
        var dto = new SwipeDto(movieId, true);
        var existingSwipe = new Swipe
        {
   Id = "swipe1",
    UserId = userId,
     MovieId = movieId,
          Liked = false,
            SwipedAt = DateTime.UtcNow
        };
        
        _userRepositoryMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
        _movieRepositoryMock.Setup(x => x.ExistsAsync(movieId)).ReturnsAsync(true);
        _swipeRepositoryMock.Setup(x => x.GetUserSwipeForMovieAsync(userId, movieId))
   .ReturnsAsync(existingSwipe);

        // Act
 var result = await _swipeService.SwipeMovieAsync(userId, dto);

        // Assert
        result.Success.Should().BeFalse();
result.Matched.Should().BeFalse();
     result.MatchId.Should().BeNull();
        _swipeRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Swipe>()), Times.Never);
    }

    [Fact]
    public async Task SwipeMovieAsync_WithDislike_CreatesSwipeWithoutMatching()
    {
        // Arrange
    var userId = "user1";
        var movieId = "movie1";
 var dto = new SwipeDto(movieId, false);
        
        _userRepositoryMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
  _movieRepositoryMock.Setup(x => x.ExistsAsync(movieId)).ReturnsAsync(true);
        _swipeRepositoryMock.Setup(x => x.GetUserSwipeForMovieAsync(userId, movieId))
 .ReturnsAsync((Swipe?)null);
      _swipeRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Swipe>()))
   .ReturnsAsync((Swipe s) => s);

     // Act
     var result = await _swipeService.SwipeMovieAsync(userId, dto);

        // Assert
     result.Success.Should().BeTrue();
        result.Matched.Should().BeFalse();
     result.MatchId.Should().BeNull();
        _swipeRepositoryMock.Verify(x => x.CreateAsync(It.Is<Swipe>(s => 
    s.UserId == userId && 
    s.MovieId == movieId && 
            s.Liked == false
        )), Times.Once);
 }

 [Fact]
public async Task SwipeMovieAsync_WithLikeButNoMatch_CreatesSwipeWithoutMatch()
    {
        // Arrange
  var userId = "user1";
     var movieId = "movie1";
        var dto = new SwipeDto(movieId, true);
        
  _userRepositoryMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
        _movieRepositoryMock.Setup(x => x.ExistsAsync(movieId)).ReturnsAsync(true);
      _swipeRepositoryMock.Setup(x => x.GetUserSwipeForMovieAsync(userId, movieId))
  .ReturnsAsync((Swipe?)null);
_swipeRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Swipe>()))
       .ReturnsAsync((Swipe s) => s);
        _swipeRepositoryMock.Setup(x => x.GetSwipesByMovieIdAsync(movieId))
         .ReturnsAsync(new List<Swipe>());

 // Act
        var result = await _swipeService.SwipeMovieAsync(userId, dto);

    // Assert
        result.Success.Should().BeTrue();
        result.Matched.Should().BeFalse();
     result.MatchId.Should().BeNull();
 _swipeRepositoryMock.Verify(x => x.CreateAsync(It.Is<Swipe>(s => 
            s.UserId == userId && 
         s.MovieId == movieId && 
    s.Liked == true
    )), Times.Once);
    }

    [Fact]
 public async Task SwipeMovieAsync_WithLikeAndMatch_CreatesSwipeAndMatch()
    {
 // Arrange
     var userId = "user1";
      var otherUserId = "user2";
  var movieId = "movie1";
 var dto = new SwipeDto(movieId, true);
        
    var otherUserSwipe = new Swipe
        {
Id = "swipe2",
       UserId = otherUserId,
         MovieId = movieId,
  Liked = true,
 SwipedAt = DateTime.UtcNow
     };
        
  _userRepositoryMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
   _movieRepositoryMock.Setup(x => x.ExistsAsync(movieId)).ReturnsAsync(true);
        _swipeRepositoryMock.Setup(x => x.GetUserSwipeForMovieAsync(userId, movieId))
          .ReturnsAsync((Swipe?)null);
      _swipeRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Swipe>()))
         .ReturnsAsync((Swipe s) => s);
     _swipeRepositoryMock.Setup(x => x.GetSwipesByMovieIdAsync(movieId))
       .ReturnsAsync(new List<Swipe> { otherUserSwipe });
   _matchRepositoryMock.Setup(x => x.GetMatchBetweenUsersForMovieAsync(userId, otherUserId, movieId))
            .ReturnsAsync((MatchEntity?)null);
      _matchRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<MatchEntity>()))
   .ReturnsAsync((MatchEntity m) => m);

        // Act
        var result = await _swipeService.SwipeMovieAsync(userId, dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Matched.Should().BeTrue();
     result.MatchId.Should().NotBeNullOrEmpty();
     _matchRepositoryMock.Verify(x => x.CreateAsync(It.Is<MatchEntity>(m =>
  m.User1Id == userId &&
            m.User2Id == otherUserId &&
   m.MovieId == movieId
     )), Times.Once);
    }

    [Fact]
    public async Task SwipeMovieAsync_WithLikeAndExistingMatch_CreatesSwipeButNoMatch()
    {
     // Arrange
    var userId = "user1";
        var otherUserId = "user2";
        var movieId = "movie1";
        var dto = new SwipeDto(movieId, true);
   
    var otherUserSwipe = new Swipe
        {
   Id = "swipe2",
   UserId = otherUserId,
    MovieId = movieId,
            Liked = true,
      SwipedAt = DateTime.UtcNow
        };
        
        var existingMatch = new MatchEntity
  {
       Id = "match1",
       User1Id = otherUserId,
   User2Id = userId,
 MovieId = movieId,
      MatchedAt = DateTime.UtcNow
};
      
        _userRepositoryMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
        _movieRepositoryMock.Setup(x => x.ExistsAsync(movieId)).ReturnsAsync(true);
      _swipeRepositoryMock.Setup(x => x.GetUserSwipeForMovieAsync(userId, movieId))
            .ReturnsAsync((Swipe?)null);
        _swipeRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Swipe>()))
       .ReturnsAsync((Swipe s) => s);
   _swipeRepositoryMock.Setup(x => x.GetSwipesByMovieIdAsync(movieId))
   .ReturnsAsync(new List<Swipe> { otherUserSwipe });
        _matchRepositoryMock.Setup(x => x.GetMatchBetweenUsersForMovieAsync(userId, otherUserId, movieId))
            .ReturnsAsync(existingMatch);

      // Act
  var result = await _swipeService.SwipeMovieAsync(userId, dto);

   // Assert
      result.Success.Should().BeTrue();
        result.Matched.Should().BeFalse();
        result.MatchId.Should().BeNull();
        _matchRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<MatchEntity>()), Times.Never);
    }

    [Fact]
  public async Task SwipeMovieAsync_WithMultipleLikes_CreatesOnlyOneMatch()
  {
  // Arrange
        var userId = "user1";
        var otherUser1 = "user2";
        var otherUser2 = "user3";
        var movieId = "movie1";
        var dto = new SwipeDto(movieId, true);
        
   var otherUserSwipes = new List<Swipe>
        {
     new Swipe { Id = "swipe2", UserId = otherUser1, MovieId = movieId, Liked = true, SwipedAt = DateTime.UtcNow },
  new Swipe { Id = "swipe3", UserId = otherUser2, MovieId = movieId, Liked = true, SwipedAt = DateTime.UtcNow }
};
    
        _userRepositoryMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
  _movieRepositoryMock.Setup(x => x.ExistsAsync(movieId)).ReturnsAsync(true);
        _swipeRepositoryMock.Setup(x => x.GetUserSwipeForMovieAsync(userId, movieId))
 .ReturnsAsync((Swipe?)null);
        _swipeRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Swipe>()))
       .ReturnsAsync((Swipe s) => s);
    _swipeRepositoryMock.Setup(x => x.GetSwipesByMovieIdAsync(movieId))
     .ReturnsAsync(otherUserSwipes);
     _matchRepositoryMock.Setup(x => x.GetMatchBetweenUsersForMovieAsync(userId, It.IsAny<string>(), movieId))
     .ReturnsAsync((MatchEntity?)null);
      _matchRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<MatchEntity>()))
            .ReturnsAsync((MatchEntity m) => m);

        // Act
  var result = await _swipeService.SwipeMovieAsync(userId, dto);

   // Assert
    result.Success.Should().BeTrue();
 result.Matched.Should().BeTrue();
result.MatchId.Should().NotBeNullOrEmpty();
     // Should only create one match (with the first user)
_matchRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<MatchEntity>()), Times.Once);
    }

  [Fact]
    public async Task SwipeMovieAsync_IgnoresDislikesWhenMatching()
    {
        // Arrange
        var userId = "user1";
  var otherUserId = "user2";
var movieId = "movie1";
   var dto = new SwipeDto(movieId, true);
        
      var otherUserSwipes = new List<Swipe>
      {
          new Swipe { Id = "swipe2", UserId = otherUserId, MovieId = movieId, Liked = false, SwipedAt = DateTime.UtcNow }
  };
      
        _userRepositoryMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
        _movieRepositoryMock.Setup(x => x.ExistsAsync(movieId)).ReturnsAsync(true);
    _swipeRepositoryMock.Setup(x => x.GetUserSwipeForMovieAsync(userId, movieId))
      .ReturnsAsync((Swipe?)null);
  _swipeRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Swipe>()))
   .ReturnsAsync((Swipe s) => s);
        _swipeRepositoryMock.Setup(x => x.GetSwipesByMovieIdAsync(movieId))
  .ReturnsAsync(otherUserSwipes);

        // Act
     var result = await _swipeService.SwipeMovieAsync(userId, dto);

 // Assert
      result.Success.Should().BeTrue();
     result.Matched.Should().BeFalse();
        result.MatchId.Should().BeNull();
 _matchRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<MatchEntity>()), Times.Never);
    }

    #endregion

#region GetUserStarredMoviesAsync Tests

    [Fact]
    public async Task GetUserStarredMoviesAsync_WithNoStarredMovies_ReturnsEmptyList()
    {
        // Arrange
        var userId = "user1";
        _swipeRepositoryMock.Setup(x => x.GetStarredMoviesByUserAsync(userId))
      .ReturnsAsync(new List<Swipe>());

        // Act
        var result = await _swipeService.GetUserStarredMoviesAsync(userId);

    // Assert
   result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserStarredMoviesAsync_WithStarredMovies_ReturnsMovieDtos()
    {
        // Arrange
      var userId = "user1";
        var starredSwipes = new List<Swipe>
        {
          new Swipe
            {
     Id = "swipe1",
   UserId = userId,
        MovieId = "movie1",
          Liked = true,
     SwipedAt = DateTime.UtcNow,
       Movie = new Movie
             {
                    Id = "movie1",
          Title = "Inception",
                    Genre = "Sci-Fi",
        Rating = 8.8,
                    Year = 2010,
           Runtime = 148,
        ImageUrl = "http://image.url",
         Description = "A mind-bending thriller"
    }
         }
   };
     
        _swipeRepositoryMock.Setup(x => x.GetStarredMoviesByUserAsync(userId))
       .ReturnsAsync(starredSwipes);

        // Act
        var result = await _swipeService.GetUserStarredMoviesAsync(userId);

        // Assert
  var movieList = result.ToList();
        movieList.Should().HaveCount(1);
    movieList[0].Id.Should().Be("movie1");
 movieList[0].Title.Should().Be("Inception");
     movieList[0].Genre.Should().Be("Sci-Fi");
    }

    #endregion

    #region GetSwipeCountAsync Tests

    [Fact]
 public async Task GetSwipeCountAsync_WithNoSwipes_ReturnsZero()
    {
 // Arrange
   var userId = "user1";
        _swipeRepositoryMock.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Swipe>());

    // Act
 var result = await _swipeService.GetSwipeCountAsync(userId);

        // Assert
        result.Should().Be(0);
    }

[Fact]
    public async Task GetSwipeCountAsync_WithSwipes_ReturnsCorrectCount()
    {
        // Arrange
   var userId = "user1";
   var swipes = new List<Swipe>
        {
     new Swipe { Id = "swipe1", UserId = userId, MovieId = "movie1", Liked = true, SwipedAt = DateTime.UtcNow },
       new Swipe { Id = "swipe2", UserId = userId, MovieId = "movie2", Liked = false, SwipedAt = DateTime.UtcNow },
          new Swipe { Id = "swipe3", UserId = userId, MovieId = "movie3", Liked = true, SwipedAt = DateTime.UtcNow }
        };
    
        _swipeRepositoryMock.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(swipes);

        // Act
        var result = await _swipeService.GetSwipeCountAsync(userId);

        // Assert
        result.Should().Be(3);
    }

    #endregion
}
