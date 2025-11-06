using CineMatchAPI.Application.Services;
using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using CineMatchAPI.Infrastructure.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CineMatch.Tests.Services;

/// <summary>
/// Tests for MovieService FilterByLength method
/// Target: Low cyclomatic complexity (8) method with 0% coverage
/// </summary>
public class MovieServiceFilterTests
{
 private readonly Mock<IMovieRepository> _mockRepository;
    private readonly Mock<ITMDbService> _mockTmdbService;
 private readonly MovieService _service;

    public MovieServiceFilterTests()
    {
        _mockRepository = new Mock<IMovieRepository>();
        _mockTmdbService = new Mock<ITMDbService>();
      _service = new MovieService(_mockRepository.Object, _mockTmdbService.Object);
    }

    #region FilterByLength Tests

    [Fact]
    public async Task GetMoviesByPreferencesAsync_WithShortLength_ReturnsOnlyShortMovies()
    {
    // Arrange
        var userId = "user1";
   var genre = "Action";
    var movies = new List<Movie>
        {
          CreateMovie("1", "Short Movie 1", 70),   // < 90 minutes
CreateMovie("2", "Short Movie 2", 85),   // < 90 minutes
     CreateMovie("3", "Medium Movie", 110),   // 90-130 minutes
      CreateMovie("4", "Long Movie", 150)      // > 130 minutes
        };

        _mockRepository.Setup(r => r.GetByGenreExcludingSwipedAsync(genre, userId, 1, 20))
     .ReturnsAsync(movies);

        // Act
   var result = await _service.GetMoviesByPreferencesAsync(userId, genre, "short");

   // Assert
        result.Should().HaveCount(2);
        result.All(m => m.Runtime < 90).Should().BeTrue();
        result.Select(m => m.Title).Should().Contain(new[] { "Short Movie 1", "Short Movie 2" });
    }

    [Fact]
    public async Task GetMoviesByPreferencesAsync_WithMediumLength_ReturnsOnlyMediumMovies()
    {
        // Arrange
      var userId = "user1";
        var genre = "Drama";
        var movies = new List<Movie>
        {
 CreateMovie("1", "Short Movie", 85),     // < 90 minutes
         CreateMovie("2", "Medium Movie 1", 90),  // 90-130 minutes
      CreateMovie("3", "Medium Movie 2", 115), // 90-130 minutes
            CreateMovie("4", "Medium Movie 3", 130), // 90-130 minutes
            CreateMovie("5", "Long Movie", 145)    // > 130 minutes
     };

    _mockRepository.Setup(r => r.GetByGenreExcludingSwipedAsync(genre, userId, 1, 20))
   .ReturnsAsync(movies);

    // Act
      var result = await _service.GetMoviesByPreferencesAsync(userId, genre, "medium");

  // Assert
  result.Should().HaveCount(3);
        result.All(m => m.Runtime >= 90 && m.Runtime <= 130).Should().BeTrue();
   result.Select(m => m.Title).Should().Contain(new[] 
        { 
  "Medium Movie 1", 
       "Medium Movie 2", 
     "Medium Movie 3" 
        });
    }

    [Fact]
    public async Task GetMoviesByPreferencesAsync_WithLongLength_ReturnsOnlyLongMovies()
    {
        // Arrange
        var userId = "user1";
      var genre = "Sci-Fi";
  var movies = new List<Movie>
        {
          CreateMovie("1", "Short Movie", 80),     // < 90 minutes
            CreateMovie("2", "Medium Movie", 120),   // 90-130 minutes
   CreateMovie("3", "Long Movie 1", 131),   // > 130 minutes
        CreateMovie("4", "Long Movie 2", 180),   // > 130 minutes
            CreateMovie("5", "Long Movie 3", 200)    // > 130 minutes
        };

    _mockRepository.Setup(r => r.GetByGenreExcludingSwipedAsync(genre, userId, 1, 20))
   .ReturnsAsync(movies);

    // Act
        var result = await _service.GetMoviesByPreferencesAsync(userId, genre, "long");

        // Assert
        result.Should().HaveCount(3);
        result.All(m => m.Runtime > 130).Should().BeTrue();
   result.Select(m => m.Title).Should().Contain(new[] 
      { 
      "Long Movie 1", 
     "Long Movie 2", 
  "Long Movie 3" 
        });
    }

    [Fact]
    public async Task GetMoviesByPreferencesAsync_WithAnyLength_ReturnsAllMovies()
    {
        // Arrange
 var userId = "user1";
        var genre = "Comedy";
    var movies = new List<Movie>
   {
       CreateMovie("1", "Short Movie", 60),
            CreateMovie("2", "Medium Movie", 100),
            CreateMovie("3", "Long Movie", 160)
   };

        _mockRepository.Setup(r => r.GetByGenreExcludingSwipedAsync(genre, userId, 1, 20))
         .ReturnsAsync(movies);

        // Act
  var result = await _service.GetMoviesByPreferencesAsync(userId, genre, "any");

   // Assert
   result.Should().HaveCount(3);
   result.Select(m => m.Title).Should().Contain(new[] 
        { 
        "Short Movie", 
     "Medium Movie", 
    "Long Movie" 
        });
    }

    [Fact]
    public async Task GetMoviesByPreferencesAsync_WithInvalidLength_ReturnsAllMovies()
    {
     // Arrange
     var userId = "user1";
    var genre = "Horror";
      var movies = new List<Movie>
        {
          CreateMovie("1", "Movie 1", 70),
            CreateMovie("2", "Movie 2", 110),
  CreateMovie("3", "Movie 3", 150)
        };

        _mockRepository.Setup(r => r.GetByGenreExcludingSwipedAsync(genre, userId, 1, 20))
       .ReturnsAsync(movies);

        // Act
        var result = await _service.GetMoviesByPreferencesAsync(userId, genre, "invalid");

        // Assert - Should default to all movies when length is invalid
    result.Should().HaveCount(3);
    }

 [Fact]
    public async Task GetMoviesByPreferencesAsync_WithUppercaseLength_FiltersCorrectly()
    {
        // Arrange
 var userId = "user1";
        var genre = "Action";
        var movies = new List<Movie>
 {
            CreateMovie("1", "Short Movie", 80),
CreateMovie("2", "Medium Movie", 100),
         CreateMovie("3", "Long Movie", 140)
        };

   _mockRepository.Setup(r => r.GetByGenreExcludingSwipedAsync(genre, userId, 1, 20))
   .ReturnsAsync(movies);

   // Act
        var result = await _service.GetMoviesByPreferencesAsync(userId, genre, "SHORT");

      // Assert - Should be case-insensitive
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Short Movie");
    }

    [Fact]
    public async Task GetMoviesByPreferencesAsync_WithMixedCaseLength_FiltersCorrectly()
    {
        // Arrange
 var userId = "user1";
        var genre = "Drama";
        var movies = new List<Movie>
     {
            CreateMovie("1", "Short Movie", 75),
            CreateMovie("2", "Medium Movie 1", 95),
   CreateMovie("3", "Medium Movie 2", 125),
            CreateMovie("4", "Long Movie", 155)
        };

        _mockRepository.Setup(r => r.GetByGenreExcludingSwipedAsync(genre, userId, 1, 20))
  .ReturnsAsync(movies);

 // Act
  var result = await _service.GetMoviesByPreferencesAsync(userId, genre, "MeDiUm");

      // Assert - Should be case-insensitive
   result.Should().HaveCount(2);
  result.Select(m => m.Title).Should().Contain(new[] { "Medium Movie 1", "Medium Movie 2" });
    }

    [Fact]
    public async Task GetMoviesByPreferencesAsync_WithExactBoundary_90Minutes_IncludedInMedium()
    {
        // Arrange
      var userId = "user1";
    var genre = "Romance";
 var movies = new List<Movie>
        {
      CreateMovie("1", "89 Minutes", 89),  // Should be short
            CreateMovie("2", "90 Minutes", 90),   // Should be medium
  CreateMovie("3", "91 Minutes", 91)       // Should be medium
     };

  _mockRepository.Setup(r => r.GetByGenreExcludingSwipedAsync(genre, userId, 1, 20))
.ReturnsAsync(movies);

        // Act
  var resultShort = await _service.GetMoviesByPreferencesAsync(userId, genre, "short");
        var resultMedium = await _service.GetMoviesByPreferencesAsync(userId, genre, "medium");

        // Assert
 resultShort.Should().HaveCount(1);
        resultShort.First().Runtime.Should().Be(89);
        
        resultMedium.Should().HaveCount(2);
        resultMedium.Select(m => m.Runtime).Should().Contain(new[] { 90, 91 });
    }

    [Fact]
    public async Task GetMoviesByPreferencesAsync_WithExactBoundary_130Minutes_IncludedInMedium()
    {
  // Arrange
        var userId = "user1";
        var genre = "Thriller";
 var movies = new List<Movie>
  {
            CreateMovie("1", "129 Minutes", 129),    // Should be medium
            CreateMovie("2", "130 Minutes", 130),    // Should be medium
      CreateMovie("3", "131 Minutes", 131)     // Should be long
        };

      _mockRepository.Setup(r => r.GetByGenreExcludingSwipedAsync(genre, userId, 1, 20))
            .ReturnsAsync(movies);

    // Act
 var resultMedium = await _service.GetMoviesByPreferencesAsync(userId, genre, "medium");
        var resultLong = await _service.GetMoviesByPreferencesAsync(userId, genre, "long");

        // Assert
 resultMedium.Should().HaveCount(2);
     resultMedium.Select(m => m.Runtime).Should().Contain(new[] { 129, 130 });
        
        resultLong.Should().HaveCount(1);
        resultLong.First().Runtime.Should().Be(131);
    }

[Fact]
    public async Task GetMoviesByPreferencesAsync_WithEmptyMovieList_ReturnsEmpty()
{
        // Arrange
        var userId = "user1";
    var genre = "Documentary";
var movies = new List<Movie>();

        _mockRepository.Setup(r => r.GetByGenreExcludingSwipedAsync(genre, userId, 1, 20))
            .ReturnsAsync(movies);

        // Act
        var result = await _service.GetMoviesByPreferencesAsync(userId, genre, "short");

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private Movie CreateMovie(string id, string title, int runtime)
    {
    return new Movie
        {
        Id = id,
       Title = title,
            Genre = "Test Genre",
            Rating = 7.5,
        Year = 2024,
            Runtime = runtime,
    ImageUrl = "http://test.com/image.jpg",
            Description = "Test movie",
 CachedAt = DateTime.UtcNow
        };
    }

    #endregion
}
