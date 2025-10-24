using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using System.Text.Json;

namespace CineMatchAPI.Application.Services;

public interface IUserService
{
    Task<UserDto?> GetUserAsync(string userId);
    Task<bool> UpdatePreferencesAsync(string userId, UpdatePreferencesDto dto);
    Task<bool> ResetUserDataAsync(string userId);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ISwipeRepository _swipeRepository;
    private readonly IMatchRepository _matchRepository;

    public UserService(
        IUserRepository userRepository,
        ISwipeRepository swipeRepository,
        IMatchRepository matchRepository)
    {
        _userRepository = userRepository;
        _swipeRepository = swipeRepository;
        _matchRepository = matchRepository;
    }

    public async Task<UserDto?> GetUserAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        var preferences = JsonSerializer.Deserialize<List<string>>(user.Preferences) ?? new List<string>();
        return new UserDto(
            user.Id,
            user.Name,
            user.Email,
            user.Location,
            user.Bio,
            user.AvatarUrl,
            preferences,
            user.MovieLength
        );
    }

    public async Task<bool> UpdatePreferencesAsync(string userId, UpdatePreferencesDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        user.Preferences = JsonSerializer.Serialize(dto.Genres);
        user.MovieLength = dto.MovieLength;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<bool> ResetUserDataAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        // Delete all swipes and matches
        await _swipeRepository.DeleteAllByUserIdAsync(userId);
        await _matchRepository.DeleteAllByUserIdAsync(userId);

        // Reset preferences
        user.Preferences = "[]";
        user.MovieLength = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        return true;
    }
}