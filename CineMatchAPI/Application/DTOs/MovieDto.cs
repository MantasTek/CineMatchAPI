namespace CineMatchAPI.Application.DTOs;

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
