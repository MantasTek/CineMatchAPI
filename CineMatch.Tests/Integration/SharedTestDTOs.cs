namespace CineMatch.Tests.Integration;

// Shared DTOs for integration tests to avoid duplication
public record AuthResponse(string Token, UserDto User);

public record UserDto(
    string Id,
    string Name,
    string Email,
    string? Location = null,
    string? Bio = null,
    string? AvatarUrl = null,
    List<string>? Preferences = null,
    string? MovieLength = null
);

public record SendMessageDto(string MatchId, string Text);

public record UpdatePreferencesDto(List<string> Genres, string MovieLength);

public record MatchDto(
    string Id,
    UserDto OtherUser,
    MovieDto Movie,
    DateTime MatchedAt
);

public record MovieDto(
    string Id,
    string Title,
    string Genre,
    double Rating,
    int Year,
    string ImageUrl,
    string Description,
    int Runtime
);

public record SwipeDto(
    string UserId,
    string MovieId,
    bool Liked
);