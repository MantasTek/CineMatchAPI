using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using CineMatchAPI.Infrastructure.Data;
using CineMatchAPI.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CineMatch.Tests.Repositories;

/// <summary>
/// Integration tests for SwipeRepository using in-memory database.
/// Tests verify CRUD operations and data integrity.
/// </summary>
public class SwipeRepositoryTests : IDisposable
{
    private readonly CineMatchDbContext _context;
    private readonly ISwipeRepository _repository;

    public SwipeRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CineMatchDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new CineMatchDbContext(options);
        _repository = new SwipeRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Create Tests

    [Fact]
    public async Task CreateAsync_WithValidSwipe_AddsToDatabase()
    {
        // Arrange
        var swipe = CreateSwipe("user1", "movie1", true);

        // Act
        var result = await _repository.CreateAsync(swipe);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(swipe.Id);
        var saved = await _context.Swipes.FindAsync(swipe.Id);
        saved.Should().NotBeNull();
        saved!.Liked.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_SetsSwipedAtTimestamp()
    {
        // Arrange
        var swipe = CreateSwipe("user1", "movie1", false);
        var beforeCreate = DateTime.UtcNow;

        // Act
        await _repository.CreateAsync(swipe);

        // Assert
        swipe.SwipedAt.Should().BeCloseTo(beforeCreate, TimeSpan.FromSeconds(2));
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsSwipe()
    {
        // Arrange
        var swipe = CreateSwipe("user1", "movie1", true);
        await _repository.CreateAsync(swipe);

        // Act
        var result = await _repository.GetByIdAsync(swipe.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(swipe.Id);
        result.UserId.Should().Be("user1");
        result.MovieId.Should().Be("movie1");
        result.Liked.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByUserId Tests

    [Fact]
    public async Task GetByUserIdAsync_WithExistingUser_ReturnsSwipes()
    {
        // Arrange
        var userId = "user1";
        await _repository.CreateAsync(CreateSwipe(userId, "movie1", true));
        await _repository.CreateAsync(CreateSwipe(userId, "movie2", false));
        await _repository.CreateAsync(CreateSwipe("user2", "movie3", true));

        // Act
        var result = await _repository.GetByUserIdAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.All(s => s.UserId == userId).Should().BeTrue();
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNoSwipes_ReturnsEmpty()
    {
        // Act
        var result = await _repository.GetByUserIdAsync("user1");

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetUserSwipeForMovie Tests

    [Fact]
    public async Task GetUserSwipeForMovieAsync_WithExistingSwipe_ReturnsSwipe()
    {
        // Arrange
        var userId = "user1";
        var movieId = "movie1";
        var swipe = CreateSwipe(userId, movieId, true);
        await _repository.CreateAsync(swipe);

        // Act
        var result = await _repository.GetUserSwipeForMovieAsync(userId, movieId);

        // Assert
        result.Should().NotBeNull();
        result!.Liked.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserSwipeForMovieAsync_WithNoSwipe_ReturnsNull()
    {
        // Act
        var result = await _repository.GetUserSwipeForMovieAsync("user1", "movie1");

        // Assert
        result.Should().BeNull();
    }

    #endregion


    #region GetSwipesByMovieId Tests

    [Fact]
    public async Task GetSwipesByMovieIdAsync_ReturnsSwipesForMovie()
    {
        // Arrange
        var movieId = "movie1";
        await _repository.CreateAsync(CreateSwipe("user1", movieId, true));
        await _repository.CreateAsync(CreateSwipe("user2", movieId, false));
        await _repository.CreateAsync(CreateSwipe("user3", "movie2", true));

        // Act
        var result = await _repository.GetSwipesByMovieIdAsync(movieId);

        // Assert
        result.Should().HaveCount(2);
        result.All(s => s.MovieId == movieId).Should().BeTrue();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateAsync_WithExistingSwipe_UpdatesSuccessfully()
    {
        // Arrange
        var swipe = CreateSwipe("user1", "movie1", false);
        await _repository.CreateAsync(swipe);

        swipe.Liked = true;

        // Act
        await _repository.UpdateAsync(swipe);

        // Assert
        var updated = await _repository.GetByIdAsync(swipe.Id);
        updated!.Liked.Should().BeTrue();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteAsync_WithExistingSwipe_RemovesFromDatabase()
    {
        // Arrange
        var swipe = CreateSwipe("user1", "movie1", true);
        await _repository.CreateAsync(swipe);

        // Act
        await _repository.DeleteAsync(swipe.Id);

        // Assert
        var deleted = await _repository.GetByIdAsync(swipe.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAllByUserIdAsync_RemovesAllUserSwipes()
    {
        // Arrange
        var userId = "user1";
        await _repository.CreateAsync(CreateSwipe(userId, "movie1", true));
        await _repository.CreateAsync(CreateSwipe(userId, "movie2", false));
        await _repository.CreateAsync(CreateSwipe("user2", "movie3", true));

        // Act
        await _repository.DeleteAllByUserIdAsync(userId);

        // Assert
        var userSwipes = await _repository.GetByUserIdAsync(userId);
        userSwipes.Should().BeEmpty();
        var otherSwipe = await _repository.GetByIdAsync("user2-movie3"); // Assuming ID is userId-movieId or something, but actually it's Guid
        // Wait, ID is Guid, so need to adjust
        // Actually, in CreateSwipe, I set Id to $"{userId}-{movieId}", but in real it's Guid.
        // For test, perhaps find by user
        var remaining = await _repository.GetByUserIdAsync("user2");
        remaining.Should().HaveCount(1);
    }

    #endregion

    #region Helper Methods

    private static Swipe CreateSwipe(string userId, string movieId, bool liked)
    {
        return new Swipe
        {
            Id = $"{userId}-{movieId}", // For simplicity in tests
            UserId = userId,
            MovieId = movieId,
            Liked = liked
        };
    }

    #endregion
}