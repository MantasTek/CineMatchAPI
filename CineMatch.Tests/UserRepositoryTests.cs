using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using CineMatchAPI.Infrastructure.Data;
using CineMatchAPI.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CineMatch.Tests.Repositories;

/// <summary>
/// Integration tests for UserRepository using in-memory database.
/// Tests verify CRUD operations and data integrity.
/// </summary>
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

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Create Tests

    [Fact]
    public async Task CreateAsync_WithValidUser_AddsToDatabase()
    {
        // Arrange
        var user = CreateUser("user1", "Test User", "test@example.com");

        // Act
        var result = await _repository.CreateAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
        var saved = await _context.Users.FindAsync(user.Id);
        saved.Should().NotBeNull();
        saved!.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAtTimestamp()
    {
        // Arrange
        var user = CreateUser("user1", "User", "user@test.com");
        var beforeCreate = DateTime.UtcNow;

        // Act
        await _repository.CreateAsync(user);

        // Assert
        user.CreatedAt.Should().BeCloseTo(beforeCreate, TimeSpan.FromSeconds(2));
        user.UpdatedAt.Should().BeCloseTo(beforeCreate, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreateAsync_WithMultipleUsers_AllAddedSuccessfully()
    {
        // Arrange & Act
        await _repository.CreateAsync(CreateUser("1", "User1", "u1@test.com"));
        await _repository.CreateAsync(CreateUser("2", "User2", "u2@test.com"));
        await _repository.CreateAsync(CreateUser("3", "User3", "u3@test.com"));

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
        var user = CreateUser("test1", "John", "john@test.com");
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetByIdAsync("test1");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("test1");
        result.Name.Should().Be("John");
        result.Email.Should().Be("john@test.com");
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
        var user = CreateUser("1", "Jane", "jane@example.com");
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
    public async Task GetByEmailAsync_IsCaseSensitive()
    {
        // Arrange
        var user = CreateUser("1", "User", "test@example.com");
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetByEmailAsync("TEST@EXAMPLE.COM");

        // Assert - SQLite == operator is case-sensitive
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_WithExactCase_ReturnsUser()
    {
        // Arrange
        var user = CreateUser("1", "User", "Test@Example.Com");
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetByEmailAsync("Test@Example.Com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("Test@Example.Com");
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
        await _repository.CreateAsync(CreateUser("1", "U1", "u1@test.com"));
        await _repository.CreateAsync(CreateUser("2", "U2", "u2@test.com"));
        await _repository.CreateAsync(CreateUser("3", "U3", "u3@test.com"));

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Select(u => u.Id).Should().Contain(new[] { "1", "2", "3" });
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateAsync_WithExistingUser_UpdatesSuccessfully()
    {
        // Arrange
        var user = CreateUser("1", "Original", "orig@test.com");
        user.Location = "OldCity";
        await _repository.CreateAsync(user);
        
        user.Name = "Updated";
        user.Location = "NewCity";

        // Act
        await _repository.UpdateAsync(user);

        // Assert
        var updated = await _repository.GetByIdAsync("1");
        updated!.Name.Should().Be("Updated");
        updated.Location.Should().Be("NewCity");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTimestamp()
    {
        // Arrange
        var user = CreateUser("1", "User", "user@test.com");
        await _repository.CreateAsync(user);
        var originalTimestamp = user.UpdatedAt;
        await Task.Delay(50);

        // Act
        user.Name = "Modified";
        await _repository.UpdateAsync(user);

        // Assert
        var updated = await _repository.GetByIdAsync("1");
        updated!.UpdatedAt.Should().BeAfter(originalTimestamp);
    }

    [Fact]
    public async Task UpdateAsync_PreservesCreatedAt()
    {
        // Arrange
        var user = CreateUser("1", "User", "user@test.com");
        await _repository.CreateAsync(user);
        var originalCreatedAt = user.CreatedAt;
        await Task.Delay(50);

        // Act
        user.Name = "Updated";
        await _repository.UpdateAsync(user);

        // Assert
        var updated = await _repository.GetByIdAsync("1");
        updated!.CreatedAt.Should().Be(originalCreatedAt);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteAsync_WithExistingUser_RemovesFromDatabase()
    {
        // Arrange
        var user = CreateUser("delete1", "ToDelete", "delete@test.com");
        await _repository.CreateAsync(user);

        // Act
        await _repository.DeleteAsync("delete1");

        // Assert
        var deleted = await _repository.GetByIdAsync("delete1");
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentUser_DoesNotThrow()
    {
        // Act & Assert
        var act = async () => await _repository.DeleteAsync("nonexistent");
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Exists Tests

    [Fact]
    public async Task ExistsAsync_WithExistingUser_ReturnsTrue()
    {
        // Arrange
        var user = CreateUser("exists1", "User", "user@test.com");
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.ExistsAsync("exists1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EmailExistsAsync_WithExistingEmail_ReturnsTrue()
    {
        // Arrange
        var user = CreateUser("1", "User", "exists@test.com");
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.EmailExistsAsync("exists@test.com");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_WithNonExistentEmail_ReturnsFalse()
    {
        // Act
        var result = await _repository.EmailExistsAsync("nonexistent@test.com");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods (DRY Principle)

    private static User CreateUser(string id, string name, string email)
    {
        return new User
        {
            Id = id,
            Name = name,
            Email = email,
            PasswordHash = "hashed_password",
            Location = "Stockholm",
            Preferences = "[]",
            MovieLength = "Medium"
        };
    }

    #endregion
}