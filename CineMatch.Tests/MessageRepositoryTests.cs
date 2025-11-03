using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using CineMatchAPI.Infrastructure.Data;
using CineMatchAPI.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CineMatch.Tests.Repositories;

/// <summary>
/// Tests for MessageRepository - verifies message CRUD operations.
/// 
/// WHAT WAS FIXED:
/// All tests were failing with NullReferenceException because messages were being created
/// with MatchId and SenderId values that didn't exist in the database. It's like trying 
/// to send a letter to an address that doesn't exist—the postal system can't deliver it 
/// because there's no destination.
/// 
/// Entity Framework requires that foreign keys (like MatchId and SenderId) must reference
/// actual existing records. When we tried to create a Message with MatchId = "match1" and
/// SenderId = "user1", but those IDs didn't exist in the Users and Matches tables, EF
/// threw a NullReferenceException.
/// 
/// THE SOLUTION:
/// We added a SeedTestData() method that runs in the constructor before any tests execute.
/// This method creates the prerequisite data:
/// 1. Two test users
/// 2. A match between those users
/// 
/// Now all tests can reference _testUser1Id, _testUser2Id, and _testMatchId knowing
/// that these IDs exist in the database and are valid foreign keys.
/// 
/// ADDITIONAL FIXES IN THIS VERSION:
/// - Removed tests for DeleteAsync and GetAllAsync (these methods don't exist in IMessageRepository)
/// - Fixed MovieId type from int to string throughout
/// - Removed duplicate GetByMatchIdAsync_OrdersBySentAtAscending test
/// </summary>
public class MessageRepositoryTests : IDisposable
{
    private readonly CineMatchDbContext _context;
    private readonly IMessageRepository _repository;
    
    // These store the IDs of our seeded test data so tests can reference them
    private string _testUser1Id = string.Empty;
    private string _testUser2Id = string.Empty;
    private string _testMatchId = string.Empty;

    public MessageRepositoryTests()
    {
        // Create a unique in-memory database for each test class instance
        // This ensures complete isolation between test runs
        var options = new DbContextOptionsBuilder<CineMatchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
            
        _context = new CineMatchDbContext(options);
        _repository = new MessageRepository(_context);
        
        // CRITICAL: Seed prerequisite data before any tests run
        SeedTestData();
    }

    /// <summary>
    /// Seeds the test database with prerequisite data that all tests need.
    /// This creates:
    /// - Two test users (stored in _testUser1Id and _testUser2Id)
    /// - One match between them (stored in _testMatchId)
    /// 
    /// Why this is necessary:
    /// Messages must belong to a Match, and Matches must have Users. Without
    /// seeding this data first, any attempt to create a Message will fail because
    /// Entity Framework can't validate the foreign key relationships.
    /// </summary>
    private void SeedTestData()
    {
        // Create two test users
        var user1 = new User
        {
            Name = "Test User 1",
            Email = "testuser1@example.com",
            PasswordHash = "hash1",
            Location = "Test City",
            Preferences = "[]",
            MovieLength = "medium"
        };

        var user2 = new User
        {
            Name = "Test User 2",
            Email = "testuser2@example.com",
            PasswordHash = "hash2",
            Location = "Test City",
            Preferences = "[]",
            MovieLength = "medium"
        };

        _context.Users.Add(user1);
        _context.Users.Add(user2);
        _context.SaveChanges();

        // Store the generated IDs so tests can use them
        _testUser1Id = user1.Id;
        _testUser2Id = user2.Id;

        // Create a match between the two users
        // FIXED: MovieId is a string, not an int
        var match = new Match
        {
            User1Id = _testUser1Id,
            User2Id = _testUser2Id,
            MovieId = "12345", // String, not int
            MatchedAt = DateTime.UtcNow
        };

        _context.Matches.Add(match);
        _context.SaveChanges();

        // Store the match ID for tests to use
        _testMatchId = match.Id;
    }

    #region Create Tests

    [Fact]
    public async Task CreateAsync_WithValidMessage_AddsToDatabase()
    {
        // Now we use the seeded IDs instead of hardcoded values
        var message = new Message
        {
            Id = "msg1",
            MatchId = _testMatchId,  // This ID exists in the database now!
            SenderId = _testUser1Id, // This ID exists too!
            Text = "Hello!",
            IsRead = false
        };

        var result = await _repository.CreateAsync(message);

        result.Should().NotBeNull();
        result.Id.Should().Be("msg1");
        var saved = await _context.Messages.FindAsync("msg1");
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_SetsSentAtTimestamp()
    {
        var message = new Message
        {
            MatchId = _testMatchId,
            SenderId = _testUser1Id,
            Text = "Test",
            IsRead = false
        };

        await _repository.CreateAsync(message);

        message.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateAsync_DefaultsIsReadToFalse()
    {
        var message = new Message
        {
            MatchId = _testMatchId,
            SenderId = _testUser1Id,
            Text = "Test"
        };

        await _repository.CreateAsync(message);

        message.IsRead.Should().BeFalse();
        message.ReadAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_PreservesMessageText()
    {
        var message = new Message
        {
            Id = "m1",
            MatchId = _testMatchId,
            SenderId = _testUser1Id,
            Text = "This is a long message with special chars: @#$%",
            IsRead = false
        };

        await _repository.CreateAsync(message);

        var saved = await _context.Messages.FindAsync("m1");
        saved!.Text.Should().Be("This is a long message with special chars: @#$%");
    }

    [Fact]
    public async Task CreateAsync_WithMultipleMessages_AllAdded()
    {
        await _repository.CreateAsync(new Message { Id = "1", MatchId = _testMatchId, SenderId = _testUser1Id, Text = "Hi", IsRead = false });
        await _repository.CreateAsync(new Message { Id = "2", MatchId = _testMatchId, SenderId = _testUser2Id, Text = "Hello", IsRead = false });
        await _repository.CreateAsync(new Message { Id = "3", MatchId = _testMatchId, SenderId = _testUser1Id, Text = "How are you?", IsRead = false });

        var all = await _context.Messages.ToListAsync();
        all.Should().HaveCount(3);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsMessage()
    {
        var message = new Message { Id = "exists", MatchId = _testMatchId, SenderId = _testUser1Id, Text = "Test", IsRead = false };
        await _repository.CreateAsync(message);

        var result = await _repository.GetByIdAsync("exists");

        result.Should().NotBeNull();
        result!.Id.Should().Be("exists");
        result.Text.Should().Be("Test");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_IsCaseSensitive()
    {
        await _repository.CreateAsync(new Message { Id = "msg1", MatchId = _testMatchId, SenderId = _testUser1Id, Text = "Test", IsRead = false });

        var result = await _repository.GetByIdAsync("MSG1");

        result.Should().BeNull();
    }

    #endregion

    #region GetByMatchId Tests

    [Fact]
    public async Task GetByMatchIdAsync_ReturnsAllMessagesForMatch()
    {
        await _repository.CreateAsync(new Message { Id = "1", MatchId = _testMatchId, SenderId = _testUser1Id, Text = "Msg1", IsRead = false });
        await _repository.CreateAsync(new Message { Id = "2", MatchId = _testMatchId, SenderId = _testUser2Id, Text = "Msg2", IsRead = false });
        
        // Create another match to ensure we're filtering correctly
        // FIXED: MovieId is string, not int
        var otherMatch = new Match { User1Id = _testUser1Id, User2Id = _testUser2Id, MovieId = "99999" };
        _context.Matches.Add(otherMatch);
        await _context.SaveChangesAsync();
        await _repository.CreateAsync(new Message { Id = "3", MatchId = otherMatch.Id, SenderId = _testUser1Id, Text = "Msg3", IsRead = false });

        var result = await _repository.GetByMatchIdAsync(_testMatchId);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(m => m.MatchId == _testMatchId);
    }

    [Fact]
    public async Task GetByMatchIdAsync_OrdersBySentAtAscending()
    {
        await Task.Delay(10);
        var msg1 = new Message { Id = "1", MatchId = _testMatchId, SenderId = _testUser1Id, Text = "First", IsRead = false, SentAt = DateTime.UtcNow.AddMinutes(-5) };
        await _repository.CreateAsync(msg1);

        await Task.Delay(10);
        var msg2 = new Message { Id = "2", MatchId = _testMatchId, SenderId = _testUser2Id, Text = "Second", IsRead = false, SentAt = DateTime.UtcNow };
        await _repository.CreateAsync(msg2);

        var result = (await _repository.GetByMatchIdAsync(_testMatchId)).ToList();

        result[0].Text.Should().Be("First");
        result[1].Text.Should().Be("Second");
    }

    [Fact]
    public async Task GetByMatchIdAsync_WithNoMessages_ReturnsEmpty()
    {
        // Create a new match with no messages
        // FIXED: MovieId is string, not int
        var emptyMatch = new Match { User1Id = _testUser1Id, User2Id = _testUser2Id, MovieId = "88888" };
        _context.Matches.Add(emptyMatch);
        await _context.SaveChangesAsync();
        
        var result = await _repository.GetByMatchIdAsync(emptyMatch.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByMatchIdAsync_IncludesSenderData()
    {
        await _repository.CreateAsync(new Message { Id = "1", MatchId = _testMatchId, SenderId = _testUser1Id, Text = "Test", IsRead = false });

        var result = (await _repository.GetByMatchIdAsync(_testMatchId)).ToList();

        result[0].Sender.Should().NotBeNull();
        result[0].Sender.Id.Should().Be(_testUser1Id);
    }

    [Fact]
    public async Task GetByMatchIdAsync_HandlesReadAndUnreadMessages()
    {
        await _repository.CreateAsync(new Message { Id = "1", MatchId = _testMatchId, SenderId = _testUser1Id, Text = "Read", IsRead = true });
        await _repository.CreateAsync(new Message { Id = "2", MatchId = _testMatchId, SenderId = _testUser2Id, Text = "Unread", IsRead = false });

        var result = await _repository.GetByMatchIdAsync(_testMatchId);

        result.Should().HaveCount(2);
        result.Should().Contain(m => m.IsRead);
        result.Should().Contain(m => !m.IsRead);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateAsync_UpdatesMessage()
    {
        var message = new Message { Id = "m1", MatchId = _testMatchId, SenderId = _testUser1Id, Text = "Original", IsRead = false };
        await _repository.CreateAsync(message);

        message.IsRead = true;
        message.ReadAt = DateTime.UtcNow;
        await _repository.UpdateAsync(message);

        var updated = await _repository.GetByIdAsync("m1");
        updated!.IsRead.Should().BeTrue();
        updated.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_CanSetReadAt()
    {
        var message = new Message { Id = "m1", MatchId = _testMatchId, SenderId = _testUser1Id, Text = "Test", IsRead = false };
        await _repository.CreateAsync(message);

        var readTime = DateTime.UtcNow;
        message.ReadAt = readTime;
        await _repository.UpdateAsync(message);

        var updated = await _repository.GetByIdAsync("m1");
        updated!.ReadAt.Should().BeCloseTo(readTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateAsync_DoesNotAffectOtherMessages()
    {
        await _repository.CreateAsync(new Message { Id = "1", MatchId = _testMatchId, SenderId = _testUser1Id, Text = "M1", IsRead = false });
        var message2 = new Message { Id = "2", MatchId = _testMatchId, SenderId = _testUser2Id, Text = "M2", IsRead = false };
        await _repository.CreateAsync(message2);

        message2.IsRead = true;
        await _repository.UpdateAsync(message2);

        var msg1 = await _repository.GetByIdAsync("1");
        msg1!.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_CanUpdateTextContent()
    {
        var message = new Message { Id = "m1", MatchId = _testMatchId, SenderId = _testUser1Id, Text = "Original", IsRead = false };
        await _repository.CreateAsync(message);

        message.Text = "Updated text";
        await _repository.UpdateAsync(message);

        var updated = await _repository.GetByIdAsync("m1");
        updated!.Text.Should().Be("Updated text");
    }

    #endregion

    #region Edge Cases and Special Scenarios

    [Fact]
    public async Task CreateAsync_WithEmptyText_StillWorks()
    {
        var message = new Message { Id = "m1", MatchId = _testMatchId, SenderId = _testUser1Id, Text = "", IsRead = false };

        await _repository.CreateAsync(message);

        var saved = await _repository.GetByIdAsync("m1");
        saved!.Text.Should().Be("");
    }

    [Fact]
    public async Task CreateAsync_WithLongText_PreservesContent()
    {
        var longText = new string('x', 1000);
        var message = new Message { Id = "m1", MatchId = _testMatchId, SenderId = _testUser1Id, Text = longText, IsRead = false };

        await _repository.CreateAsync(message);

        var saved = await _repository.GetByIdAsync("m1");
        saved!.Text.Should().HaveLength(1000);
    }

    [Fact]
    public async Task GetByMatchIdAsync_WithManyMessages_ReturnsAllInOrder()
    {
        for (int i = 1; i <= 20; i++)
        {
            await Task.Delay(5);
            await _repository.CreateAsync(new Message
            {
                Id = $"msg{i}",
                MatchId = _testMatchId,
                SenderId = i % 2 == 0 ? _testUser1Id : _testUser2Id,
                Text = $"Message {i}",
                IsRead = false,
                SentAt = DateTime.UtcNow
            });
        }

        var result = (await _repository.GetByMatchIdAsync(_testMatchId)).ToList();

        result.Should().HaveCount(20);
        for (int i = 0; i < result.Count - 1; i++)
        {
            result[i].SentAt.Should().BeBefore(result[i + 1].SentAt);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateAsync_WithDifferentIsReadValues_PreservesValue(bool isRead)
    {
        var message = new Message { Id = $"m{isRead}", MatchId = _testMatchId, SenderId = _testUser1Id, Text = "Test", IsRead = isRead };

        await _repository.CreateAsync(message);

        var saved = await _repository.GetByIdAsync($"m{isRead}");
        saved!.IsRead.Should().Be(isRead);
    }

    [Fact]
    public async Task UpdateAsync_FromUnreadToRead_UpdatesBothFields()
    {
        var message = new Message { Id = "m1", MatchId = _testMatchId, SenderId = _testUser1Id, Text = "Test", IsRead = false, ReadAt = null };
        await _repository.CreateAsync(message);

        message.IsRead = true;
        message.ReadAt = DateTime.UtcNow;
        await _repository.UpdateAsync(message);

        var updated = await _repository.GetByIdAsync("m1");
        updated!.IsRead.Should().BeTrue();
        updated.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByMatchIdAsync_WithMultipleMatches_OnlyReturnsSpecifiedMatch()
    {
        // Create another match for comparison
        // FIXED: MovieId is string, not int
        var match2 = new Match { User1Id = _testUser1Id, User2Id = _testUser2Id, MovieId = "77777" };
        _context.Matches.Add(match2);
        await _context.SaveChangesAsync();
        
        await _repository.CreateAsync(new Message { Id = "1", MatchId = _testMatchId, SenderId = _testUser1Id, Text = "M1", IsRead = false });
        await _repository.CreateAsync(new Message { Id = "2", MatchId = match2.Id, SenderId = _testUser1Id, Text = "M2", IsRead = false });
        await _repository.CreateAsync(new Message { Id = "3", MatchId = _testMatchId, SenderId = _testUser2Id, Text = "M3", IsRead = false });

        var result = await _repository.GetByMatchIdAsync(_testMatchId);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(m => m.MatchId == _testMatchId);
    }

    [Fact]
    public async Task CreateAsync_WithSpecialCharacters_PreservesText()
    {
        var specialText = "Hello! 👋 How are you? 😊 Let's watch <movie> @ 7pm!";
        var message = new Message { Id = "m1", MatchId = _testMatchId, SenderId = _testUser1Id, Text = specialText, IsRead = false };

        await _repository.CreateAsync(message);

        var saved = await _repository.GetByIdAsync("m1");
        saved!.Text.Should().Be(specialText);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}