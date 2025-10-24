namespace CineMatchAPI.Application.DTOs;

public record MessageDto(
    string Id,
    string MatchId,
    string SenderId,
    string Text,
    DateTime SentAt,
    bool IsRead
);
