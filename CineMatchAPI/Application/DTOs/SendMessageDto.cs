namespace CineMatchAPI.Application.DTOs;

public record SendMessageDto(
    string MatchId,
    string Text
);