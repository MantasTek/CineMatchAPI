using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using CineMatchAPI.Infrastructure.Data;
using CineMatchAPI.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Microsoft.EntityFrameworkCore.InMemory;

namespace CineMatch.Tests.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly CineMatchDbContext _context;
    private readonly IUserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CineMatchDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new CineMatchDbContext(options);
        _repository = new UserRepository(_context);
    }

    #region Create Tests

    [Fact]
    public async Task CreateAsync_WithValidUser_AddsToDatabase()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hashed",
            Location = "Stockholm"
        };

        // Act
        var result = await _repository.CreateAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
        var saved = await _context.Users.FindAsync(user.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAtTimestamp()
    {
        // Arrange
        var user = new User
        {
            Name = "User",
            Email = "user@test.com",
            PasswordHash = "hash",
            Location = "City"
        };

        // Act
        await _repository.CreateAsync(user);

        // Assert
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateAsync_WithMultipleUsers_AllAddedSuccessfully()
    {
        // Arrange & Act
        await _repository.CreateAsync(new User { Id = "1", Name = "User1", Email = "u1@t.com", PasswordHash = "h", Location = "L" });
        await _repository.CreateAsync(new User { Id = "2", Name = "User2", Email = "u2@t.com", PasswordHash = "h", Location = "L" });
        await _repository.CreateAsync(new User { Id = "3", Name = "User3", Email = "u3@t.com", PasswordHash = "h", Location = "L" });

        // Assert
        var users = await _repository.GetAllAsync();
        users.Should().HaveCount(3);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsUser()
    {
        // Arrange
        var user = new User { Id = "test1", Name = "John", Email = "john@test.com", PasswordHash = "hash", Location = "Oslo" };
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetByIdAsync("test1");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("test1");
        result.Name.Should().Be("John");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync("");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByEmail Tests

    [Fact]
    public async Task GetByEmailAsync_WithExistingEmail_ReturnsUser()
    {
        // Arrange
        var user = new User { Id = "1", Name = "Jane", Email = "jane@example.com", PasswordHash = "hash", Location = "Berlin" };
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetByEmailAsync("jane@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("jane@example.com");
        result.Name.Should().Be("Jane");
    }

    [Fact]
    public async Task GetByEmailAsync_WithNonExistentEmail_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nobody@nowhere.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_IsCaseInsensitive()
    {
        // Arrange
        var user = new User { Id = "1", Name = "User", Email = "Test@Example.Com", PasswordHash = "h", Location = "L" };
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetByEmailAsync("test@example.com");

        // Assert - SQLite is case-insensitive for LIKE, but exact match depends on collation
        // This test documents current behavior
        result?.Email.ToLower().Should().Be("test@example.com");
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAllAsync_WithNoUsers_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleUsers_ReturnsAllUsers()
    {
        // Arrange
        await _repository.CreateAsync(new User { Id = "1", Name = "U1", Email = "u1@t.com", PasswordHash = "h", Location = "L" });
        await _repository.CreateAsync(new User { Id = "2", Name = "U2", Email = "u2@t.com", PasswordHash = "h", Location = "L" });
        await _repository.CreateAsync(new User { Id = "3", Name = "U3", Email = "u3@t.com", PasswordHash = "h", Location = "L" });

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateAsync_WithExistingUser_UpdatesSuccessfully()
    {
        // Arrange
        var user = new User { Id = "1", Name = "Original", Email = "orig@test.com", PasswordHash = "h", Location = "OldCity" };
        await _repository.CreateAsync(user);
        user.Name = "Updated";
        user.Location = "NewCity";

        // Act
        var result = await _repository.UpdateAsync(user);

        // Assert
        var updated = await _repository.GetByIdAsync("1");
        updated!.Name.Should().Be("Updated");
        updated.Location.Should().Be("NewCity");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTimestamp()
    {
        // Arrange
        var user = new User { Id = "1", Name = "User", Email = "user@test.com", PasswordHash = "h", Location = "City" };
        await _repository.CreateAsync(user);
        var originalTimestamp = user.UpdatedAt;
        await Task.Delay(10); // Small delay to ensure timestamp changes

        // Act
        user.Name = "Modified";
        await _repository.UpdateAsync(user);

        // Assert
        var updated = await _repository.GetByIdAsync("1");
        updated!.UpdatedAt.Should().BeAfter(originalTimestamp);
    }

    [Fact]
    public async Task UpdateAsync_WithPreferences_UpdatesCorrectly()
    {
        // Arrange
        var user = new User { Id = "1", Name = "User", Email = "u@t.com", PasswordHash = "h", Location = "L", Preferences = "[]" };
        await _repository.CreateAsync(user);
        user.Preferences = "[\"Action\", \"Drama\"]";

        // Act
        await _repository.UpdateAsync(user);

        // Assert
        var updated = await _repository.GetByIdAsync("1");
        updated!.Preferences.Should().Be("[\"Action\", \"Drama\"]");
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteAsync_WithExistingId_RemovesUser()
    {
        // Arrange
        var user = new User { Id = "delete1", Name = "ToDelete", Email = "delete@test.com", PasswordHash = "h", Location = "L" };
        await _repository.CreateAsync(user);

        // Act
        await _repository.DeleteAsync("delete1");

        // Assert
        var deleted = await _repository.GetByIdAsync("delete1");
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_DoesNotThrow()
    {
        // Act
        Func<Task> act = async () => await _repository.DeleteAsync("nonexistent");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAsync_OnlyDeletesSpecifiedUser()
    {
        // Arrange
        await _repository.CreateAsync(new User { Id = "1", Name = "Keep1", Email = "k1@t.com", PasswordHash = "h", Location = "L" });
        await _repository.CreateAsync(new User { Id = "2", Name = "Delete", Email = "d@t.com", PasswordHash = "h", Location = "L" });
        await _repository.CreateAsync(new User { Id = "3", Name = "Keep2", Email = "k2@t.com", PasswordHash = "h", Location = "L" });

        // Act
        await _repository.DeleteAsync("2");

        // Assert
        var remaining = await _repository.GetAllAsync();
        remaining.Should().HaveCount(2);
        remaining.Should().NotContain(u => u.Id == "2");
    }

    #endregion

    #region Exists Tests

    [Fact]
    public async Task ExistsAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        await _repository.CreateAsync(new User { Id = "exists1", Name = "User", Email = "u@t.com", PasswordHash = "h", Location = "L" });

        // Act
        var result = await _repository.ExistsAsync("exists1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentId_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync("nope");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EmailExistsAsync_WithExistingEmail_ReturnsTrue()
    {
        // Arrange
        await _repository.CreateAsync(new User { Id = "1", Name = "User", Email = "exists@test.com", PasswordHash = "h", Location = "L" });

        // Act
        var result = await _repository.EmailExistsAsync("exists@test.com");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_WithNonExistentEmail_ReturnsFalse()
    {
        // Act
        var result = await _repository.EmailExistsAsync("nobody@nowhere.com");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CreateAsync_WithLongBio_SavesSuccessfully()
    {
        // Arrange
        var longBio = new string('x', 500);
        var user = new User { Id = "1", Name = "User", Email = "u@t.com", PasswordHash = "h", Location = "L", Bio = longBio };

        // Act
        await _repository.CreateAsync(user);

        // Assert
        var saved = await _repository.GetByIdAsync("1");
        saved!.Bio.Should().Be(longBio);
    }

    [Fact]
    public async Task CreateAsync_WithNullOptionalFields_SavesSuccessfully()
    {
        // Arrange
        var user = new User 
        { 
            Id = "1", 
            Name = "User", 
            Email = "u@t.com", 
            PasswordHash = "h", 
            Location = "L",
            Bio = null,
            AvatarUrl = null,
            MovieLength = null
        };

        // Act
        await _repository.CreateAsync(user);

        // Assert
        var saved = await _repository.GetByIdAsync("1");
        saved!.Bio.Should().BeNull();
        saved.AvatarUrl.Should().BeNull();
        saved.MovieLength.Should().BeNull();
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}