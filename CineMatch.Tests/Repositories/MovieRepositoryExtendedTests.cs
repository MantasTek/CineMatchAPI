using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using CineMatchAPI.Infrastructure.Data;
using CineMatchAPI.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CineMatch.Tests.Repositories;

/// <summary>
/// Extended tests for MovieRepository
/// Tests edge cases and complex query scenarios
/// </summary>
public class MovieRepositoryExtendedTests : IDisposable
{
    private readonly CineMatchDbContext _context;
    private readonly IMovieRepository _repository;

  public MovieRepositoryExtendedTests()
    {
  var options = new DbContextOptionsBuilder<CineMatchDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CineMatchDbContext(options);
      _repository = new MovieRepository(_context);
    }

    #region GetByGenreExcludingSwipedAsync Tests

    [Fact]
    public async Task GetByGenreExcludingSwipedAsync_WithNoSwipes_ReturnsAllMoviesInGenre()
    {
        // Arrange
        var userId = "user1";
  var genre = "Action";
  
        var movies = new List<Movie>
        {
            CreateMovie("1", "Movie1", genre),
        CreateMovie("2", "Movie2", genre),
            CreateMovie("3", "Movie3", genre)
  };
        
        await _context.Movies.AddRangeAsync(movies);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByGenreExcludingSwipedAsync(genre, userId, 1, 10);

      // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetByGenreExcludingSwipedAsync_ExcludesSwipedMovies()
    {
        // Arrange
  var userId = "user1";
      var genre = "Action";
    
      var movies = new List<Movie>
 {
         CreateMovie("1", "Movie1", genre),
            CreateMovie("2", "Movie2", genre),
            CreateMovie("3", "Movie3", genre)
};
        
  await _context.Movies.AddRangeAsync(movies);
  
        var swipes = new List<Swipe>
   {
            new Swipe { Id = "s1", UserId = userId, MovieId = "1", Liked = true, SwipedAt = DateTime.UtcNow },
            new Swipe { Id = "s2", UserId = userId, MovieId = "2", Liked = false, SwipedAt = DateTime.UtcNow }
        };
   
        await _context.Swipes.AddRangeAsync(swipes);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByGenreExcludingSwipedAsync(genre, userId, 1, 10);

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be("3");
    }

    [Fact]
    public async Task GetByGenreExcludingSwipedAsync_WithDifferentGenre_ReturnsEmpty()
    {
        // Arrange
 var userId = "user1";
        var targetGenre = "Comedy";
      
    var movies = new List<Movie>
        {
            CreateMovie("1", "Movie1", "Action"),
         CreateMovie("2", "Movie2", "Drama")
        };
        
        await _context.Movies.AddRangeAsync(movies);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByGenreExcludingSwipedAsync(targetGenre, userId, 1, 10);

        // Assert
   result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByGenreExcludingSwipedAsync_OnlyExcludesCurrentUserSwipes()
    {
        // Arrange
        var userId = "user1";
      var otherUserId = "user2";
        var genre = "Action";
        
        var movies = new List<Movie>
        {
 CreateMovie("1", "Movie1", genre),
 CreateMovie("2", "Movie2", genre)
        };
 
    await _context.Movies.AddRangeAsync(movies);
        
        var swipes = new List<Swipe>
     {
            new Swipe { Id = "s1", UserId = otherUserId, MovieId = "1", Liked = true, SwipedAt = DateTime.UtcNow }
        };
    
        await _context.Swipes.AddRangeAsync(swipes);
        await _context.SaveChangesAsync();

     // Act
        var result = await _repository.GetByGenreExcludingSwipedAsync(genre, userId, 1, 10);

        // Assert
  result.Should().HaveCount(2); // Should include movie1 since current user hasn't swiped it
    }

    [Fact]
    public async Task GetByGenreExcludingSwipedAsync_WithPagination_ReturnsCorrectPage()
    {
     // Arrange
        var userId = "user1";
        var genre = "Action";
        
        var movies = Enumerable.Range(1, 25)
  .Select(i => CreateMovie(i.ToString(), $"Movie{i}", genre))
   .ToList();

        await _context.Movies.AddRangeAsync(movies);
        await _context.SaveChangesAsync();

        // Act
        var page1 = await _repository.GetByGenreExcludingSwipedAsync(genre, userId, 1, 10);
        var page2 = await _repository.GetByGenreExcludingSwipedAsync(genre, userId, 2, 10);
        var page3 = await _repository.GetByGenreExcludingSwipedAsync(genre, userId, 3, 10);

// Assert
        page1.Should().HaveCount(10);
        page2.Should().HaveCount(10);
  page3.Should().HaveCount(5);
    }

 [Fact]
    public async Task GetByGenreExcludingSwipedAsync_CaseInsensitiveGenre()
    {
        // Arrange
        var userId = "user1";
      
        var movies = new List<Movie>
        {
     CreateMovie("1", "Movie1", "Action"),
         CreateMovie("2", "Movie2", "ACTION")
        };
        
        await _context.Movies.AddRangeAsync(movies);
        await _context.SaveChangesAsync();

   // Act
      var result = await _repository.GetByGenreExcludingSwipedAsync("action", userId, 1, 10);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingMovie_ReturnsTrue()
    {
        // Arrange
        var movie = CreateMovie("1", "Movie1", "Action");
        await _context.Movies.AddAsync(movie);
        await _context.SaveChangesAsync();

   // Act
   var exists = await _repository.ExistsAsync("1");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentMovie_ReturnsFalse()
    {
     // Act
  var exists = await _repository.ExistsAsync("nonexistent");

 // Assert
        exists.Should().BeFalse();
    }

    [Fact]
  public async Task ExistsAsync_WithNullId_ReturnsFalse()
  {
 // Act
   var exists = await _repository.ExistsAsync(null!);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithEmptyId_ReturnsFalse()
  {
 // Act
        var exists = await _repository.ExistsAsync("");

     // Assert
        exists.Should().BeFalse();
    }

    #endregion

    #region GetByGenreAsync Tests

    [Fact]
    public async Task GetByGenreAsync_WithMatchingGenre_ReturnsMovies()
  {
     // Arrange
        var genre = "Comedy";
        var movies = new List<Movie>
        {
  CreateMovie("1", "Comedy1", genre),
CreateMovie("2", "Comedy2", genre),
  CreateMovie("3", "Action1", "Action")
   };
  
     await _context.Movies.AddRangeAsync(movies);
      await _context.SaveChangesAsync();

    // Act
 var result = await _repository.GetByGenreAsync(genre, 1, 20);

        // Assert
      result.Should().HaveCount(2);
  result.All(m => m.Genre.ToLower() == genre.ToLower()).Should().BeTrue();
    }

    [Fact]
    public async Task GetByGenreAsync_WithNoMatches_ReturnsEmpty()
    {
        // Arrange
  var movies = new List<Movie>
{
         CreateMovie("1", "Movie1", "Action"),
            CreateMovie("2", "Movie2", "Drama")
        };
        
    await _context.Movies.AddRangeAsync(movies);
        await _context.SaveChangesAsync();

        // Act
   var result = await _repository.GetByGenreAsync("Horror", 1, 20);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_AddsMovieToDatabase()
    {
        // Arrange
    var movie = CreateMovie("1", "NewMovie", "Action");

     // Act
        await _repository.CreateAsync(movie);

   // Assert
        var savedMovie = await _context.Movies.FindAsync("1");
     savedMovie.Should().NotBeNull();
  savedMovie!.Title.Should().Be("NewMovie");
    }

    [Fact]
    public async Task CreateAsync_MultipleMovies_AllAdded()
    {
     // Arrange
        var movies = new List<Movie>
        {
    CreateMovie("1", "Movie1", "Action"),
   CreateMovie("2", "Movie2", "Drama")
    };

      // Act
        foreach (var movie in movies)
        {
        await _repository.CreateAsync(movie);
        }

        // Assert
 var allMovies = await _context.Movies.ToListAsync();
        allMovies.Should().HaveCount(2);
    }

    #endregion

    #region Helper Methods

    private Movie CreateMovie(string id, string title, string genre)
    {
  return new Movie
        {
      Id = id,
   Title = title,
   Genre = genre,
            Rating = 7.5,
      Year = 2024,
    Runtime = 120,
    ImageUrl = "http://test.com/image.jpg",
            Description = "Test movie",
            CachedAt = DateTime.UtcNow
    };
    }

    public void Dispose()
    {
     _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #endregion
}
