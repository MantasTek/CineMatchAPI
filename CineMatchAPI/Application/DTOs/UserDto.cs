namespace CineMatchAPI.Application.DTOs;

public record UserDto(
    string Id,
    string Name,
    string Email,
    string Location,
    string? Bio,
    string? AvatarUrl,
    List<string>? Preferences, // Nullable - null means onboarding not completed
    string? MovieLength
);