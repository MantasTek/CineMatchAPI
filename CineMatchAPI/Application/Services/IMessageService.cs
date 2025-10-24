using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;

namespace CineMatchAPI.Application.Services;

public interface IMessageService
{
    Task<IEnumerable<MessageDto>> GetMessagesAsync(string matchId);
    Task<MessageDto> SendMessageAsync(string userId, SendMessageDto dto);
    Task MarkAsReadAsync(string messageId);
}

public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IMatchRepository _matchRepository;

    public MessageService(IMessageRepository messageRepository, IMatchRepository matchRepository)
    {
        _messageRepository = messageRepository;
        _matchRepository = matchRepository;
    }

    public async Task<IEnumerable<MessageDto>> GetMessagesAsync(string matchId)
    {
        var messages = await _messageRepository.GetByMatchIdAsync(matchId);
        return messages.Select(m => new MessageDto(
            m.Id,
            m.MatchId,
            m.SenderId,
            m.Text,
            m.SentAt,
            m.IsRead
        ));
    }

    public async Task<MessageDto> SendMessageAsync(string userId, SendMessageDto dto)
    {
        var match = await _matchRepository.GetByIdAsync(dto.MatchId);
        if (match == null)
        {
            throw new InvalidOperationException("Match not found");
        }

        if (match.User1Id != userId && match.User2Id != userId)
        {
            throw new UnauthorizedAccessException("User not part of this match");
        }

        var message = new Message
        {
            Id = Guid.NewGuid().ToString(),
            MatchId = dto.MatchId,
            SenderId = userId,
            Text = dto.Text,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        await _messageRepository.CreateAsync(message);

        return new MessageDto(
            message.Id,
            message.MatchId,
            message.SenderId,
            message.Text,
            message.SentAt,
            message.IsRead
        );
    }

    public async Task MarkAsReadAsync(string messageId)
    {
        var message = await _messageRepository.GetByIdAsync(messageId);
        if (message == null)
        {
            throw new InvalidOperationException("Message not found");
        }

        message.IsRead = true;
        message.ReadAt = DateTime.UtcNow;
        await _messageRepository.UpdateAsync(message);
    }
}