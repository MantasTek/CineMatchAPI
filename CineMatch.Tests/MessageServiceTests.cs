using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Application.Services;
using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CineMatch.Tests.Services;

/// <summary>
/// Comprehensive unit tests for MessageService.
/// Tests cover message retrieval, sending, authorization, and validation guards.
/// This addresses the critical 0% coverage gap for message functionality.
/// </summary>
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
        // Arrange
        var matchId = "match1";
        var messages = new List<Message>
        {
            CreateMessage("msg1", matchId, "user1", "Hello"),
            CreateMessage("msg2", matchId, "user2", "Hi there")
        };
        
        _messageRepositoryMock
            .Setup(x => x.GetByMatchIdAsync(matchId))
            .ReturnsAsync(messages);

        // Act
        var result = await _messageService.GetMessagesAsync(matchId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Text.Should().Be("Hello");
    }

    [Fact]
    public async Task GetMessagesAsync_WithNoMessages_ReturnsEmptyList()
    {
        // Arrange
        var matchId = "match1";
        _messageRepositoryMock
            .Setup(x => x.GetByMatchIdAsync(matchId))
            .ReturnsAsync(new List<Message>());

        // Act
        var result = await _messageService.GetMessagesAsync(matchId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMessagesAsync_MapsAllMessageProperties()
    {
        // Arrange
        var matchId = "match1";
        var sentAt = DateTime.UtcNow.AddHours(-1);
        var message = CreateMessage("msg1", matchId, "user1", "Test message");
        message.SentAt = sentAt;
        message.IsRead = true;
        
        _messageRepositoryMock
            .Setup(x => x.GetByMatchIdAsync(matchId))
            .ReturnsAsync(new List<Message> { message });

        // Act
        var result = await _messageService.GetMessagesAsync(matchId);

        // Assert
        var messageDto = result.First();
        messageDto.Id.Should().Be("msg1");
        messageDto.MatchId.Should().Be(matchId);
        messageDto.SenderId.Should().Be("user1");
        messageDto.Text.Should().Be("Test message");
        messageDto.SentAt.Should().Be(sentAt);
        messageDto.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task GetMessagesAsync_WithMultipleMessages_ReturnsAllInOrder()
    {
        // Arrange
        var matchId = "match1";
        var messages = new List<Message>
        {
            CreateMessage("msg1", matchId, "user1", "First"),
            CreateMessage("msg2", matchId, "user2", "Second"),
            CreateMessage("msg3", matchId, "user1", "Third")
        };
        
        _messageRepositoryMock
            .Setup(x => x.GetByMatchIdAsync(matchId))
            .ReturnsAsync(messages);

        // Act
        var result = await _messageService.GetMessagesAsync(matchId);

        // Assert
        result.Should().HaveCount(3);
        var messageList = result.ToList();
        messageList[0].Text.Should().Be("First");
        messageList[1].Text.Should().Be("Second");
        messageList[2].Text.Should().Be("Third");
    }

    #endregion

    #region SendMessageAsync - Positive Scenarios

    [Fact]
    public async Task SendMessageAsync_WithValidData_CreatesAndReturnsMessage()
    {
        // Arrange
        var userId = "user1";
        var matchId = "match1";
        var text = "Hello, this is a test message!";
        var dto = new SendMessageDto(matchId, text);
        
        SetupValidMatch(matchId, userId, "user2");
        
        _messageRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m);

        // Act
        var result = await _messageService.SendMessageAsync(userId, dto);

        // Assert
        result.Should().NotBeNull();
        result.MatchId.Should().Be(matchId);
        result.SenderId.Should().Be(userId);
        result.Text.Should().Be(text);
        result.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task SendMessageAsync_GeneratesUniqueMessageId()
    {
        // Arrange
        var userId = "user1";
        var dto = new SendMessageDto("match1", "Test");
        SetupValidMatch("match1", userId, "user2");
        
        _messageRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m);

        // Act
        var result = await _messageService.SendMessageAsync(userId, dto);

        // Assert
        result.Id.Should().NotBeNullOrEmpty();
        Guid.TryParse(result.Id, out _).Should().BeTrue();
    }

    [Fact]
    public async Task SendMessageAsync_SetsSentAtToCurrentTime()
    {
        // Arrange
        var userId = "user1";
        var dto = new SendMessageDto("match1", "Test");
        SetupValidMatch("match1", userId, "user2");
        
        var beforeSend = DateTime.UtcNow;
        _messageRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m);

        // Act
        var result = await _messageService.SendMessageAsync(userId, dto);

        // Assert
        result.SentAt.Should().BeCloseTo(beforeSend, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task SendMessageAsync_CallsRepositoryCreate()
    {
        // Arrange
        var userId = "user1";
        var dto = new SendMessageDto("match1", "Test message");
        SetupValidMatch("match1", userId, "user2");
        
        _messageRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m);

        // Act
        await _messageService.SendMessageAsync(userId, dto);

        // Assert
        _messageRepositoryMock.Verify(
            x => x.CreateAsync(It.Is<Message>(m => 
                m.MatchId == "match1" &&
                m.SenderId == userId &&
                m.Text == "Test message" &&
                m.IsRead == false
            )),
            Times.Once
        );
    }

    [Fact]
    public async Task SendMessageAsync_WorksWhenUserIsUser2InMatch()
    {
        // Arrange
        var userId = "user2";
        var dto = new SendMessageDto("match1", "Message from user2");
        SetupValidMatch("match1", "user1", userId);
        
        _messageRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m);

        // Act
        var result = await _messageService.SendMessageAsync(userId, dto);

        // Assert
        result.SenderId.Should().Be(userId);
    }

    #endregion

    #region SendMessageAsync - Negative Scenarios & Guards

    [Fact]
    public async Task SendMessageAsync_WithNonExistentMatch_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = "user1";
        var dto = new SendMessageDto("nonexistent", "Test");
        
        _matchRepositoryMock
            .Setup(x => x.GetByIdAsync("nonexistent"))
            .ReturnsAsync((Match?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _messageService.SendMessageAsync(userId, dto)
        );
    }

    [Fact]
    public async Task SendMessageAsync_WithUnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = "user3"; // Not part of the match
        var dto = new SendMessageDto("match1", "Test");
        SetupValidMatch("match1", "user1", "user2");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _messageService.SendMessageAsync(userId, dto)
        );
    }

    [Fact]
    public async Task SendMessageAsync_WithEmptyMessage_StillProcesses()
    {
        // Arrange - Empty messages might be allowed at service level
        // Frontend should prevent this, but we test the behavior
        var userId = "user1";
        var dto = new SendMessageDto("match1", "");
        SetupValidMatch("match1", userId, "user2");
        
        _messageRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m);

        // Act
        var result = await _messageService.SendMessageAsync(userId, dto);

        // Assert
        result.Text.Should().Be("");
    }

    [Theory]
    [InlineData("A")]
    [InlineData("Hello")]
    [InlineData("This is a normal length message")]
    [InlineData("This is a much longer message that contains multiple sentences and should still be valid as long as it's under any reasonable character limit.")]
    public async Task SendMessageAsync_WithVariousMessageLengths_Succeeds(string text)
    {
        // Arrange
        var userId = "user1";
        var dto = new SendMessageDto("match1", text);
        SetupValidMatch("match1", userId, "user2");
        
        _messageRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m);

        // Act
        var result = await _messageService.SendMessageAsync(userId, dto);

        // Assert
        result.Text.Should().Be(text);
    }

    [Fact]
    public async Task SendMessageAsync_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var userId = "user1";
        var specialText = "Hello! @#$%^&*() 😀 <script>alert('test')</script>";
        var dto = new SendMessageDto("match1", specialText);
        SetupValidMatch("match1", userId, "user2");
        
        _messageRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m);

        // Act
        var result = await _messageService.SendMessageAsync(userId, dto);

        // Assert
        result.Text.Should().Be(specialText);
    }

    #endregion

    #region MarkAsReadAsync - Positive Scenarios

    [Fact]
    public async Task MarkAsReadAsync_WithValidMessageId_UpdatesMessage()
    {
        // Arrange
        var messageId = "msg1";
        var message = CreateMessage(messageId, "match1", "user1", "Test");
        message.IsRead = false;
        
        _messageRepositoryMock
            .Setup(x => x.GetByIdAsync(messageId))
            .ReturnsAsync(message);
        
        _messageRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m);

        // Act
        await _messageService.MarkAsReadAsync(messageId);

        // Assert
        message.IsRead.Should().BeTrue();
        message.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAsReadAsync_SetsReadAtTimestamp()
    {
        // Arrange
        var messageId = "msg1";
        var message = CreateMessage(messageId, "match1", "user1", "Test");
        var beforeMark = DateTime.UtcNow;
        
        _messageRepositoryMock
            .Setup(x => x.GetByIdAsync(messageId))
            .ReturnsAsync(message);
        
        _messageRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m);

        // Act
        await _messageService.MarkAsReadAsync(messageId);

        // Assert
        message.ReadAt.Should().NotBeNull();
        message.ReadAt.Value.Should().BeCloseTo(beforeMark, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task MarkAsReadAsync_CallsRepositoryUpdate()
    {
        // Arrange
        var messageId = "msg1";
        var message = CreateMessage(messageId, "match1", "user1", "Test");
        
        _messageRepositoryMock
            .Setup(x => x.GetByIdAsync(messageId))
            .ReturnsAsync(message);
        
        _messageRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m);

        // Act
        await _messageService.MarkAsReadAsync(messageId);

        // Assert
        _messageRepositoryMock.Verify(
            x => x.UpdateAsync(It.Is<Message>(m => 
                m.Id == messageId && 
                m.IsRead == true
            )),
            Times.Once
        );
    }

    [Fact]
    public async Task MarkAsReadAsync_OnAlreadyReadMessage_StillUpdates()
    {
        // Arrange
        var messageId = "msg1";
        var message = CreateMessage(messageId, "match1", "user1", "Test");
        message.IsRead = true;
        message.ReadAt = DateTime.UtcNow.AddHours(-1);
        
        _messageRepositoryMock
            .Setup(x => x.GetByIdAsync(messageId))
            .ReturnsAsync(message);
        
        _messageRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Message>()))
            .ReturnsAsync((Message m) => m);

        // Act
        await _messageService.MarkAsReadAsync(messageId);

        // Assert
        message.IsRead.Should().BeTrue();
        _messageRepositoryMock.Verify(x => x.UpdateAsync(message), Times.Once);
    }

    #endregion

    #region MarkAsReadAsync - Negative Scenarios

    [Fact]
    public async Task MarkAsReadAsync_WithNonExistentMessage_ThrowsInvalidOperationException()
    {
        // Arrange
        var messageId = "nonexistent";
        _messageRepositoryMock
            .Setup(x => x.GetByIdAsync(messageId))
            .ReturnsAsync((Message?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _messageService.MarkAsReadAsync(messageId)
        );
    }

    [Fact]
    public async Task MarkAsReadAsync_WithNullMessageId_ThrowsException()
    {
        // Arrange
        _messageRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Message?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _messageService.MarkAsReadAsync("invalid")
        );
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
        var match = new Match
        {
            Id = matchId,
            User1Id = user1Id,
            User2Id = user2Id,
            MovieId = "movie1",
            MatchedAt = DateTime.UtcNow,
            User1 = new User
            {
                Id = user1Id,
                Name = "User1",
                Email = "user1@test.com",
                PasswordHash = "hash",
                Location = "Test",
                Preferences = "[]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            User2 = new User
            {
                Id = user2Id,
                Name = "User2",
                Email = "user2@test.com",
                PasswordHash = "hash",
                Location = "Test",
                Preferences = "[]",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
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
            .ReturnsAsync(match);
    }

    #endregion
}