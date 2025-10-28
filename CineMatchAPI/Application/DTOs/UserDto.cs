namespace CineMatchAPI.Application.DTOs;

public record UserDto(
    string Id,
    string Name,
    string Email,
    string Location,
    string? Bio,
    string? AvatarUrl,
    string preferences,
    List<string> Preferences,
    string? MovieLength
)   
{
    // Fix for CS8862: Add 'this' constructor initializer to call the primary constructor
    public UserDto(string id, string name, string email, string location, string? bio, string? avatarUrl, List<string> preferences, string? movieLength)
        : this(id, name, email, location, bio, avatarUrl, string.Empty, preferences, movieLength)
    {
    }

    public UserDto(string id, string email, string name, string location, string? bio, string? avatarUrl, string preferences, string? movieLength, DateTime createdAt)
        : this(id, name, email, location, bio, avatarUrl, preferences, new List<string>(), movieLength)
    {
        // 'createdAt' is not used, but if needed, you can add a property for it.
    }
}
