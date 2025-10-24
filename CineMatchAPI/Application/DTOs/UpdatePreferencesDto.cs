namespace CineMatchAPI.Application.DTOs;

public record UpdatePreferencesDto(
    List<string> Genres,
    string MovieLength
);
