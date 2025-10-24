using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CineMatchAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessageController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    [HttpGet("{matchId}")]
    public async Task<IActionResult> GetMessages(string matchId)
    {
        var messages = await _messageService.GetMessagesAsync(matchId);
        return Ok(messages);
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        try
        {
            var message = await _messageService.SendMessageAsync(GetUserId(), dto);
            return Ok(message);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Not authorized to send messages in this match" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{messageId}/read")]
    public async Task<IActionResult> MarkAsRead(string messageId)
    {
        try
        {
            await _messageService.MarkAsReadAsync(messageId);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}