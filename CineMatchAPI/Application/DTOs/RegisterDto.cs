namespace CineMatchAPI.Application.DTOs;

public record RegisterDto(
    string Name,
    string Email,
    string Password,
    string Location
);
