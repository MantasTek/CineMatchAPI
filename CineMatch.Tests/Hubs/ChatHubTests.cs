using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Application.Services;
using CineMatchAPI.Hubs;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CineMatch.Tests.Hubs;

public class ChatHubTests
{
    private readonly Mock<IMessageService> _messageServiceMock;
    private readonly Mock<HubCallerContext> _contextMock;
    private readonly Mock<IHubCallerClients> _clientsMock;
    private readonly Mock<IGroupManager> _groupManagerMock;
    private readonly ChatHub _chatHub;
    private const string TestUserId = "user123";
    private const string TestConnectionId = "conn123";

    public ChatHubTests()
    {
        _messageServiceMock = new Mock<IMessageService>();
        _contextMock = new Mock<HubCallerContext>();
        _clientsMock = new Mock<IHubCallerClients>();
        _groupManagerMock = new Mock<IGroupManager>();

        _chatHub = new ChatHub(_messageServiceMock.Object)
        {
            Context = _contextMock.Object,
            Clients = _clientsMock.Object,
            Groups = _groupManagerMock.Object
        };

        SetupHubContext();
    }

    #region SendMessage Tests

    [Fact]
    public async Task SendMessage_WithValidMessage_SendsToGroup()
    {
        var matchId = "match1";
        var message = "Hello!";
        var messageDto = new MessageDto(
            "msg1",
            matchId,
            TestUserId,
            message,
            DateTime.UtcNow,
            false
        );

        _messageServiceMock
            .Setup(x => x.SendMessageAsync(TestUserId, It.IsAny<SendMessageDto>()))
            .ReturnsAsync(messageDto);

        var groupClient = new Mock<IClientProxy>();
        _clientsMock.Setup(x => x.Group(matchId)).Returns(groupClient.Object);

        await _chatHub.SendMessage(matchId, message);

        _messageServiceMock.Verify(x => x.SendMessageAsync(TestUserId, It.Is<SendMessageDto>(dto =>
            dto.MatchId == matchId && dto.Text == message)), Times.Once);
    }

    [Fact]
    public async Task SendMessage_WithServiceException_SendsErrorToCaller()
    {
        var matchId = "match1";
        var message = "Test";

        _messageServiceMock
            .Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<SendMessageDto>()))
            .ThrowsAsync(new Exception("Service error"));

        var callerClient = new Mock<ISingleClientProxy>();
        _clientsMock.Setup(x => x.Caller).Returns(callerClient.Object);

        await _chatHub.SendMessage(matchId, message);

        callerClient.Verify(x => x.SendCoreAsync("Error", It.IsAny<object[]>(), default), Times.Once);
    }

    #endregion

    #region JoinMatch Tests

    [Fact]
    public async Task JoinMatch_AddsUserToGroup()
    {
        var matchId = "match1";

        await _chatHub.JoinMatch(matchId);

        _groupManagerMock.Verify(x => x.AddToGroupAsync(TestConnectionId, matchId, default), Times.Once);
    }

    #endregion

    #region LeaveMatch Tests

    [Fact]
    public async Task LeaveMatch_RemovesUserFromGroup()
    {
        var matchId = "match1";

        await _chatHub.LeaveMatch(matchId);

        _groupManagerMock.Verify(x => x.RemoveFromGroupAsync(TestConnectionId, matchId, default), Times.Once);
    }

    #endregion

    #region MarkAsRead Tests

    [Fact]
    public async Task MarkAsRead_WithValidMessage_MarksRead()
    {
        var messageId = "msg1";

        _messageServiceMock
            .Setup(x => x.MarkAsReadAsync(messageId))
            .Returns(Task.CompletedTask);

        var callerClient = new Mock<ISingleClientProxy>();
        _clientsMock.Setup(x => x.Caller).Returns(callerClient.Object);

        await _chatHub.MarkAsRead(messageId);

        _messageServiceMock.Verify(x => x.MarkAsReadAsync(messageId), Times.Once);
    }

    #endregion

    #region UserTyping Tests

    [Fact]
    public async Task UserTyping_NotifiesOthersInGroup()
    {
        var matchId = "match1";
        var othersClient = new Mock<ISingleClientProxy>();
        _clientsMock.Setup(x => x.OthersInGroup(matchId)).Returns(othersClient.Object);

        await _chatHub.UserTyping(matchId);

        othersClient.Verify(x => x.SendCoreAsync("UserTyping", It.IsAny<object[]>(), default), Times.Once);
    }

    [Fact]
    public async Task UserStoppedTyping_NotifiesOthersInGroup()
    {
        var matchId = "match1";
        var othersClient = new Mock<ISingleClientProxy>();
        _clientsMock.Setup(x => x.OthersInGroup(matchId)).Returns(othersClient.Object);

        await _chatHub.UserStoppedTyping(matchId);

        othersClient.Verify(x => x.SendCoreAsync("UserStoppedTyping", It.IsAny<object[]>(), default), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupHubContext()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId)
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        _contextMock.Setup(x => x.User).Returns(principal);
        _contextMock.Setup(x => x.ConnectionId).Returns(TestConnectionId);
    }

    #endregion
}
