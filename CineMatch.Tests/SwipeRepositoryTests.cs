using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using CineMatchAPI.Infrastructure.Data;
using CineMatchAPI.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CineMatch.Tests.Repositories;

public class SwipeRepositoryTests : IDisposable
{
    private readonly CineMatchDbContext _context;
    private readonly ISwipeRepository _repository;

    public SwipeRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CineMatchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new CineMatchDbContext(options);
        _repository = new SwipeRepository(_context);
    }

    [Fact]
    public async Task CreateAsync_AddsSwipeToDatabase()
    {
        // Arrange
        var swipe = new Swipe
        {
            Id = "swipe1",
            UserId = "user1",
            MovieId = "movie1",
            Liked = true
        };

        // Act
        await _repository.CreateAsync(swipe);

        // Assert
        var saved = await _context.Swipes.FindAsync("swipe1");
        saved.Should().NotBeNull();
        saved!.Liked.Should().BeTrue();
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsAllUserSwipes()
    {
        // Arrange
        await _repository.CreateAsync(new Swipe { Id = "1", UserId = "user1", MovieId = "m1", Liked = true });
        await _repository.CreateAsync(new Swipe { Id = "2", UserId = "user1", MovieId = "m2", Liked = false });
        await _repository.CreateAsync(new Swipe { Id = "3", UserId = "user2", MovieId = "m3", Liked = true });

        // Act
        var result = await _repository.GetByUserIdAsync("user1");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.UserId == "user1");
    }

    [Fact]
    public async Task GetByUserIdAsync_OrdersBySwipedAtDescending()
    {
        // Arrange
        await Task.Delay(10);
        await _repository.CreateAsync(new Swipe { Id = "1", UserId = "user1", MovieId = "m1", Liked = true, SwipedAt = DateTime.UtcNow.AddMinutes(-2) });
        await Task.Delay(10);
        await _repository.CreateAsync(new Swipe { Id = "2", UserId = "user1", MovieId = "m2", Liked = false, SwipedAt = DateTime.UtcNow });

        // Act
        var result = (await _repository.GetByUserIdAsync("user1")).ToList();

        // Assert
        result[0].Id.Should().Be("2");  // Most recent first
        result[1].Id.Should().Be("1");
    }

    [Fact]
    public async Task GetUserSwipeForMovieAsync_WithExistingSwipe_ReturnsSwipe()
    {
        // Arrange
        var swipe = new Swipe { Id = "1", UserId = "user1", MovieId = "movie1", Liked = true };
        await _repository.CreateAsync(swipe);

        // Act
        var result = await _repository.GetUserSwipeForMovieAsync("user1", "movie1");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("1");
    }

    [Fact]
    public async Task GetUserSwipeForMovieAsync_WithNoMatch_ReturnsNull()
    {
        // Act
        var result = await _repository.GetUserSwipeForMovieAsync("user1", "movie1");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserSwipeForMovieAsync_OnlyReturnsExactMatch()
    {
        // Arrange
        await _repository.CreateAsync(new Swipe { Id = "1", UserId = "user1", MovieId = "movie1", Liked = true });
        await _repository.CreateAsync(new Swipe { Id = "2", UserId = "user2", MovieId = "movie1", Liked = true });

        // Act
        var result = await _repository.GetUserSwipeForMovieAsync("user1", "movie1");

        // Assert
        result!.UserId.Should().Be("user1");
        result.MovieId.Should().Be("movie1");
    }

    [Fact]
    public async Task GetStarredMoviesByUserAsync_OnlyReturnsLikedMovies()
    {
        // Arrange
        await SeedMovies();
        await _repository.CreateAsync(new Swipe { Id = "1", UserId = "user1", MovieId = "m1", Liked = true });
        await _repository.CreateAsync(new Swipe { Id = "2", UserId = "user1", MovieId = "m2", Liked = false });
        await _repository.CreateAsync(new Swipe { Id = "3", UserId = "user1", MovieId = "m3", Liked = true });

        // Act
        var result = await _repository.GetStarredMoviesByUserAsync("user1");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.Liked == true);
    }

    [Fact]
    public async Task GetStarredMoviesByUserAsync_IncludesMovieData()
    {
        // Arrange
        await SeedMovies();
        await _repository.CreateAsync(new Swipe { Id = "1", UserId = "user1", MovieId = "m1", Liked = true });

        // Act
        var result = (await _repository.GetStarredMoviesByUserAsync("user1")).ToList();

        // Assert
        result[0].Movie.Should().NotBeNull();
        result[0].Movie.Title.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetStarredMoviesByUserAsync_WithNoLikes_ReturnsEmpty()
    {
        // Act
        var result = await _repository.GetStarredMoviesByUserAsync("user1");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAllByUserIdAsync_RemovesAllUserSwipes()
    {
        // Arrange
        await _repository.CreateAsync(new Swipe { Id = "1", UserId = "user1", MovieId = "m1", Liked = true });
        await _repository.CreateAsync(new Swipe { Id = "2", UserId = "user1", MovieId = "m2", Liked = false });
        await _repository.CreateAsync(new Swipe { Id = "3", UserId = "user2", MovieId = "m3", Liked = true });

        // Act
        await _repository.DeleteAllByUserIdAsync("user1");

        // Assert
        var remaining = await _repository.GetByUserIdAsync("user1");
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAllByUserIdAsync_OnlyDeletesSpecifiedUser()
    {
        // Arrange
        await _repository.CreateAsync(new Swipe { Id = "1", UserId = "user1", MovieId = "m1", Liked = true });
        await _repository.CreateAsync(new Swipe { Id = "2", UserId = "user2", MovieId = "m2", Liked = true });

        // Act
        await _repository.DeleteAllByUserIdAsync("user1");

        // Assert
        var otherUserSwipes = await _repository.GetByUserIdAsync("user2");
        otherUserSwipes.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsSwipe()
    {
        // Arrange
        var swipe = new Swipe { Id = "swipe1", UserId = "u1", MovieId = "m1", Liked = true };
        await _repository.CreateAsync(swipe);

        // Act
        var result = await _repository.GetByIdAsync("swipe1");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("swipe1");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    private async Task SeedMovies()
    {
        _context.Movies.Add(new Movie { Id = "m1", Title = "Movie 1", Genre = "Action", Rating = 8.0, Year = 2020, ImageUrl = "url", Description = "desc", Runtime = 120 });
        _context.Movies.Add(new Movie { Id = "m2", Title = "Movie 2", Genre = "Comedy", Rating = 7.5, Year = 2021, ImageUrl = "url", Description = "desc", Runtime = 90 });
        _context.Movies.Add(new Movie { Id = "m3", Title = "Movie 3", Genre = "Drama", Rating = 9.0, Year = 2022, ImageUrl = "url", Description = "desc", Runtime = 150 });
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}