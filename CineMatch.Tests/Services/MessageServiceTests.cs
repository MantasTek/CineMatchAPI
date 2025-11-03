using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Application.Services;
using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

// Resolve the ambiguity between Moq.Match and CineMatchAPI.Domain.Entities.Match
using DomainMatch = CineMatchAPI.Domain.Entities.Match;

namespace CineMatch.Tests.Services;

public class MessageServiceTests
{
    private readonly Mock<IMessageRepository> _messageRepositoryMock;
    private readonly Mock<IMatchRepository> _matchRepositoryMock;
    private readonly MessageService _messageService;

    public MessageServiceTests()
    {
        _messageRepositoryMock = new Mock<IMessageRepository>();
        _matchRepositoryMock = new Mock<IMatchRepository>();
        _messageService = new MessageService(
            _messageRepositoryMock.Object,
            _matchRepositoryMock.Object
        );
    }

    #region GetMessagesAsync - Positive Scenarios

    [Fact]
    public async Task GetMessagesAsync_WithValidMatchId_ReturnsMessages()
    {
        var matchId = "match1";
        var messages = new List<Message>
        {
            CreateMessage("msg1", matchId, "user1", "Hello"),
            CreateMessage("msg2", matchId, "user2", "Hi there")
        };

        _messageRepositoryMock
            .Setup(x => x.GetByMatchIdAsync(matchId))
            .ReturnsAsync(messages);

        var result = await _messageService.GetMessagesAsync(matchId);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Text.Should().Be("Hello");
    }

    [Fact]
    public async Task GetMessagesAsync_WithNoMessages_ReturnsEmptyList()
    {
        var matchId = "match1";
        _messageRepositoryMock
            .Setup(x => x.GetByMatchIdAsync(matchId))
            .ReturnsAsync(new List<Message>());

        var result = await _messageService.GetMessagesAsync(matchId);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region SendMessageAsync - Positive Scenarios

    [Fact]
    public async Task SendMessageAsync_WithValidData_CreatesAndReturnsMessage()
    {
        var userId = "user1";
        var matchId = "match1";
        var text = "Hello!";
        var dto = new SendMessageDto(matchId, text);

        SetupValidMatch(matchId, userId, "user2");

        _messageRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m);

        var result = await _messageService.SendMessageAsync(userId, dto);

        result.Should().NotBeNull();
        result.MatchId.Should().Be(matchId);
        result.SenderId.Should().Be(userId);
        result.Text.Should().Be(text);
    }

    #endregion

    #region SendMessageAsync - Negative Scenarios

    [Fact]
    public async Task SendMessageAsync_WithNonExistentMatch_ThrowsInvalidOperationException()
    {
        var userId = "user1";
        var dto = new SendMessageDto("nonexistent", "Test");

        _matchRepositoryMock
            .Setup(x => x.GetByIdAsync("nonexistent"))
            .ReturnsAsync((DomainMatch?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _messageService.SendMessageAsync(userId, dto)
        );
    }

    [Fact]
    public async Task SendMessageAsync_WithUnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        var userId = "user3";
        var dto = new SendMessageDto("match1", "Test");
        SetupValidMatch("match1", "user1", "user2");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _messageService.SendMessageAsync(userId, dto)
        );
    }

    #endregion

    #region MarkAsReadAsync

    [Fact]
    public async Task MarkAsReadAsync_WithValidMessageId_UpdatesMessage()
    {
        var messageId = "msg1";
        var message = CreateMessage(messageId, "match1", "user1", "Test");
        message.IsRead = false;

        _messageRepositoryMock
            .Setup(x => x.GetByIdAsync(messageId))
            .ReturnsAsync(message);

        _messageRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m);

        await _messageService.MarkAsReadAsync(messageId);

        message.IsRead.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private Message CreateMessage(string id, string matchId, string senderId, string text)
    {
        return new Message
        {
            Id = id,
            MatchId = matchId,
            SenderId = senderId,
            Text = text,
            SentAt = DateTime.UtcNow,
            IsRead = false,
            Sender = new User
            {
                Id = senderId,
                Name = $"User{senderId}",
                Email = $"{senderId}@test.com",
                PasswordHash = "hash",
                Location = "Test",
                Preferences = "[]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
    }

    private void SetupValidMatch(string matchId, string user1Id, string user2Id)
    {
        var match = new DomainMatch
        {
            Id = matchId,
            User1Id = user1Id,
            User2Id = user2Id,
            MovieId = "movie1",
            MatchedAt = DateTime.UtcNow,
            User1 = new User { Id = user1Id, Name = "User1" },
            User2 = new User { Id = user2Id, Name = "User2" },
            Movie = new Movie
            {
                Id = "movie1",
                Title = "Test Movie",
                Genre = "Action",
                Rating = 8.0,
                Year = 2024,
                ImageUrl = "url",
                Description = "desc",
                Runtime = 120
            }
        };

        _matchRepositoryMock
            .Setup(x => x.GetByIdAsync(matchId))
            .ReturnsAsync((DomainMatch?)match);
    }

    #endregion
}
