using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using CineMatchAPI.Infrastructure.Data;
using CineMatchAPI.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CineMatch.Tests.Repositories;

public class MessageRepositoryTests : IDisposable
{
    private readonly CineMatchDbContext _context;
    private readonly IMessageRepository _repository;

    public MessageRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CineMatchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new CineMatchDbContext(options);
        _repository = new MessageRepository(_context);
    }

    #region Create Tests

    [Fact]
    public async Task CreateAsync_WithValidMessage_AddsToDatabase()
    {
        var message = new Message
        {
            Id = "msg1",
            MatchId = "match1",
            SenderId = "user1",
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
            MatchId = "match1",
            SenderId = "user1",
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
            MatchId = "match1",
            SenderId = "user1",
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
            MatchId = "match1",
            SenderId = "user1",
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
        await _repository.CreateAsync(new Message { Id = "1", MatchId = "m1", SenderId = "u1", Text = "Hi", IsRead = false });
        await _repository.CreateAsync(new Message { Id = "2", MatchId = "m1", SenderId = "u2", Text = "Hello", IsRead = false });
        await _repository.CreateAsync(new Message { Id = "3", MatchId = "m1", SenderId = "u1", Text = "How are you?", IsRead = false });

        var all = await _context.Messages.ToListAsync();
        all.Should().HaveCount(3);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsMessage()
    {
        var message = new Message { Id = "exists", MatchId = "m1", SenderId = "u1", Text = "Test", IsRead = false };
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
        await _repository.CreateAsync(new Message { Id = "msg1", MatchId = "m1", SenderId = "u1", Text = "Test", IsRead = false });

        var result = await _repository.GetByIdAsync("MSG1");

        result.Should().BeNull();
    }

    #endregion

    #region GetByMatchId Tests

    [Fact]
    public async Task GetByMatchIdAsync_ReturnsAllMessagesForMatch()
    {
        await _repository.CreateAsync(new Message { Id = "1", MatchId = "match1", SenderId = "u1", Text = "Msg1", IsRead = false });
        await _repository.CreateAsync(new Message { Id = "2", MatchId = "match1", SenderId = "u2", Text = "Msg2", IsRead = false });
        await _repository.CreateAsync(new Message { Id = "3", MatchId = "match2", SenderId = "u1", Text = "Msg3", IsRead = false });

        var result = await _repository.GetByMatchIdAsync("match1");

        result.Should().HaveCount(2);
        result.Should().OnlyContain(m => m.MatchId == "match1");
    }

    [Fact]
    public async Task GetByMatchIdAsync_OrdersBySentAtAscending()
    {
        await Task.Delay(10);
        var msg1 = new Message { Id = "1", MatchId = "match1", SenderId = "u1", Text = "First", IsRead = false, SentAt = DateTime.UtcNow.AddMinutes(-5) };
        await _repository.CreateAsync(msg1);

        await Task.Delay(10);
        var msg2 = new Message { Id = "2", MatchId = "match1", SenderId = "u2", Text = "Second", IsRead = false, SentAt = DateTime.UtcNow };
        await _repository.CreateAsync(msg2);

        var result = (await _repository.GetByMatchIdAsync("match1")).ToList();

        result[0].Text.Should().Be("First");
        result[1].Text.Should().Be("Second");
    }

    [Fact]
    public async Task GetByMatchIdAsync_WithNoMessages_ReturnsEmpty()
    {
        var result = await _repository.GetByMatchIdAsync("nonexistent");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByMatchIdAsync_IncludesSenderData()
    {
        await SeedUser("user1");
        await _repository.CreateAsync(new Message { Id = "1", MatchId = "match1", SenderId = "user1", Text = "Test", IsRead = false });

        var result = (await _repository.GetByMatchIdAsync("match1")).ToList();

        result[0].Sender.Should().NotBeNull();
        result[0].Sender.Id.Should().Be("user1");
    }

    [Fact]
    public async Task GetByMatchIdAsync_HandlesReadAndUnreadMessages()
    {
        await _repository.CreateAsync(new Message { Id = "1", MatchId = "match1", SenderId = "u1", Text = "Read", IsRead = true });
        await _repository.CreateAsync(new Message { Id = "2", MatchId = "match1", SenderId = "u2", Text = "Unread", IsRead = false });

        var result = await _repository.GetByMatchIdAsync("match1");

        result.Should().HaveCount(2);
        result.Should().Contain(m => m.IsRead);
        result.Should().Contain(m => !m.IsRead);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateAsync_UpdatesMessage()
    {
        var message = new Message { Id = "m1", MatchId = "match1", SenderId = "u1", Text = "Original", IsRead = false };
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
        var message = new Message { Id = "m1", MatchId = "match1", SenderId = "u1", Text = "Test", IsRead = false };
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
        await _repository.CreateAsync(new Message { Id = "1", MatchId = "m1", SenderId = "u1", Text = "M1", IsRead = false });
        var message2 = new Message { Id = "2", MatchId = "m1", SenderId = "u2", Text = "M2", IsRead = false };
        await _repository.CreateAsync(message2);

        message2.IsRead = true;
        await _repository.UpdateAsync(message2);

        var msg1 = await _repository.GetByIdAsync("1");
        msg1!.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_CanUpdateTextContent()
    {
        var message = new Message { Id = "m1", MatchId = "match1", SenderId = "u1", Text = "Original", IsRead = false };
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
        var message = new Message { Id = "m1", MatchId = "match1", SenderId = "u1", Text = "", IsRead = false };

        await _repository.CreateAsync(message);

        var saved = await _repository.GetByIdAsync("m1");
        saved!.Text.Should().Be("");
    }

    [Fact]
    public async Task CreateAsync_WithLongText_PreservesContent()
    {
        var longText = new string('x', 1000);
        var message = new Message { Id = "m1", MatchId = "match1", SenderId = "u1", Text = longText, IsRead = false };

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
                MatchId = "match1",
                SenderId = i % 2 == 0 ? "user1" : "user2",
                Text = $"Message {i}",
                IsRead = false,
                SentAt = DateTime.UtcNow
            });
        }

        var result = (await _repository.GetByMatchIdAsync("match1")).ToList();

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
        var message = new Message { Id = $"m{isRead}", MatchId = "m1", SenderId = "u1", Text = "Test", IsRead = isRead };

        await _repository.CreateAsync(message);

        var saved = await _repository.GetByIdAsync($"m{isRead}");
        saved!.IsRead.Should().Be(isRead);
    }

    [Fact]
    public async Task UpdateAsync_FromUnreadToRead_UpdatesBothFields()
    {
        var message = new Message { Id = "m1", MatchId = "match1", SenderId = "u1", Text = "Test", IsRead = false, ReadAt = null };
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
        await _repository.CreateAsync(new Message { Id = "1", MatchId = "match1", SenderId = "u1", Text = "M1", IsRead = false });
        await _repository.CreateAsync(new Message { Id = "2", MatchId = "match2", SenderId = "u1", Text = "M2", IsRead = false });
        await _repository.CreateAsync(new Message { Id = "3", MatchId = "match1", SenderId = "u2", Text = "M3", IsRead = false });

        var result = await _repository.GetByMatchIdAsync("match1");

        result.Should().HaveCount(2);
        result.Should().OnlyContain(m => m.MatchId == "match1");
    }

    [Fact]
    public async Task CreateAsync_WithSpecialCharacters_PreservesText()
    {
        var specialText = "Hello! 👋 How are you? 😊 Let's watch <movie> @ 7pm!";
        var message = new Message { Id = "m1", MatchId = "match1", SenderId = "u1", Text = specialText, IsRead = false };

        await _repository.CreateAsync(message);

        var saved = await _repository.GetByIdAsync("m1");
        saved!.Text.Should().Be(specialText);
    }

    #endregion

    private async Task SeedUser(string userId)
    {
        _context.Users.Add(new User
        {
            Id = userId,
            Name = $"User {userId}",
            Email = $"{userId}@test.com",
            PasswordHash = "hash",
            Location = "Test City"
        });
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}