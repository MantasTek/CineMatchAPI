using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Application.Services;
using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

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
        _messageService = new MessageService(_messageRepositoryMock.Object, _matchRepositoryMock.Object);
    }

    [Fact]
    public async Task GetMessagesAsync_ReturnsAllMessages()
    {
        // Arrange
        var messages = new List<Message>
        {
            new Message { Id = "1", MatchId = "match1", SenderId = "user1", Text = "Hello", SentAt = DateTime.UtcNow, IsRead = false },
            new Message { Id = "2", MatchId = "match1", SenderId = "user2", Text = "Hi", SentAt = DateTime.UtcNow, IsRead = true }
        };
        _messageRepositoryMock.Setup(x => x.GetByMatchIdAsync("match1")).ReturnsAsync(messages);

        // Act
        var result = await _messageService.GetMessagesAsync("match1");

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SendMessageAsync_WithValidMatch_CreatesMessage()
    {
        // Arrange
        var match = new CineMatchAPI.Domain.Entities.Match { Id = "match1", User1Id = "user1", User2Id = "user2", MovieId = "movie1" };
        var dto = new SendMessageDto("match1", "Test message");
        _matchRepositoryMock.Setup(x => x.GetByIdAsync("match1")).ReturnsAsync(match);
        _messageRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Message>())).ReturnsAsync((Message m) => m);

        // Act
        var result = await _messageService.SendMessageAsync("user1", dto);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be("Test message");
        result.SenderId.Should().Be("user1");
    }

    [Fact]
    public async Task SendMessageAsync_WithNonExistentMatch_ThrowsException()
    {
        // Arrange
        var dto = new SendMessageDto("nonexistent", "Test");
        _matchRepositoryMock.Setup(x => x.GetByIdAsync("nonexistent")).ReturnsAsync((CineMatchAPI.Domain.Entities.Match?)null);

        // Act
        Func<Task> act = async () => await _messageService.SendMessageAsync("user1", dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Match not found");
    }

    [Fact]
    public async Task SendMessageAsync_WithUnauthorizedUser_ThrowsException()
    {
        // Arrange
        var match = new CineMatchAPI.Domain.Entities.Match { Id = "match1", User1Id = "user1", User2Id = "user2", MovieId = "movie1" };
        var dto = new SendMessageDto("match1", "Test");
        _matchRepositoryMock.Setup(x => x.GetByIdAsync("match1")).ReturnsAsync(match);

        // Act
        Func<Task> act = async () => await _messageService.SendMessageAsync("user3", dto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task MarkAsReadAsync_WithValidMessage_UpdatesStatus()
    {
        // Arrange
        var message = new Message { Id = "msg1", MatchId = "match1", SenderId = "user1", Text = "Test", IsRead = false };
        _messageRepositoryMock.Setup(x => x.GetByIdAsync("msg1")).ReturnsAsync(message);
        _messageRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Message>())).ReturnsAsync((Message m) => m);

        // Act
        await _messageService.MarkAsReadAsync("msg1");

        // Assert
        _messageRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Message>(m => m.IsRead == true)), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_WithNonExistentMessage_ThrowsException()
    {
        // Arrange
        _messageRepositoryMock.Setup(x => x.GetByIdAsync("nonexistent")).ReturnsAsync((Message?)null);

        // Act
        Func<Task> act = async () => await _messageService.MarkAsReadAsync("nonexistent");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Message not found");
    }

    [Fact]
    public async Task SendMessageAsync_SetsCorrectTimestamp()
    {
        // Arrange
        var match = new CineMatchAPI.Domain.Entities.Match { Id = "match1", User1Id = "user1", User2Id = "user2", MovieId = "movie1" };
        var dto = new SendMessageDto("match1", "Test");
        var beforeSend = DateTime.UtcNow;
        _matchRepositoryMock.Setup(x => x.GetByIdAsync("match1")).ReturnsAsync(match);
        _messageRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Message>())).ReturnsAsync((Message m) => m);

        // Act
        var result = await _messageService.SendMessageAsync("user1", dto);
        var afterSend = DateTime.UtcNow;

        // Assert
        result.SentAt.Should().BeOnOrAfter(beforeSend);
        result.SentAt.Should().BeOnOrBefore(afterSend);
    }

    [Fact]
    public async Task SendMessageAsync_MarksAsUnread()
    {
        // Arrange
        var match = new CineMatchAPI.Domain.Entities.Match { Id = "match1", User1Id = "user1", User2Id = "user2", MovieId = "movie1" };
        var dto = new SendMessageDto("match1", "Test");
        _matchRepositoryMock.Setup(x => x.GetByIdAsync("match1")).ReturnsAsync(match);
        _messageRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Message>())).ReturnsAsync((Message m) => m);

        // Act
        var result = await _messageService.SendMessageAsync("user1", dto);

        // Assert
        result.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAsReadAsync_SetsReadAtTimestamp()
    {
        // Arrange
        var message = new Message { Id = "msg1", MatchId = "match1", SenderId = "user1", Text = "Test", IsRead = false };
        _messageRepositoryMock.Setup(x => x.GetByIdAsync("msg1")).ReturnsAsync(message);
        _messageRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Message>())).ReturnsAsync((Message m) => m);

        // Act
        await _messageService.MarkAsReadAsync("msg1");

        // Assert
        _messageRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Message>(m => m.ReadAt != null)), Times.Once);
    }
}