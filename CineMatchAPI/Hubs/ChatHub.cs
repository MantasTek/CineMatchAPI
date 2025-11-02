using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using CineMatchAPI.Application.Services;
using CineMatchAPI.Application.DTOs;
using System.Security.Claims;

namespace CineMatchAPI.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;

    public ChatHub(IMessageService messageService)
    {
        _messageService = messageService;
    }

    private string GetUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
    }

    public async Task JoinMatch(string matchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, matchId);
        Console.WriteLine($"User {GetUserId()} joined match {matchId}");
    }

    public async Task LeaveMatch(string matchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, matchId);
        Console.WriteLine($"User {GetUserId()} left match {matchId}");
    }

    public async Task SendMessage(string matchId, string text)
    {
        try
        {
            var userId = GetUserId();
            var message = await _messageService.SendMessageAsync(userId, new SendMessageDto(matchId, text));

            await Clients.Group(matchId).SendAsync("ReceiveMessage", message);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task MarkAsRead(string messageId)
    {
        try
        {
            await _messageService.MarkAsReadAsync(messageId);
            await Clients.Caller.SendAsync("MessageRead", messageId);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task UserTyping(string matchId)
    {
        var userId = GetUserId();
        await Clients.OthersInGroup(matchId).SendAsync("UserTyping", userId);
    }

    public async Task UserStoppedTyping(string matchId)
    {
        var userId = GetUserId();
        await Clients.OthersInGroup(matchId).SendAsync("UserStoppedTyping", userId);
    }
}