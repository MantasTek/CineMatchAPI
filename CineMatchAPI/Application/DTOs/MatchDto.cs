namespace CineMatchAPI.Application.DTOs;

public record MatchDto(
    string Id,
    UserDto OtherUser,
    MovieDto Movie,
    DateTime MatchedAt
);
