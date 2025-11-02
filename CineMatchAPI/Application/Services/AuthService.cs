using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using System.Text.Json;

namespace CineMatchAPI.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task<UserDto?> GetUserByIdAsync(string userId);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(dto.Email))
        {
            return null;
        }

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = _passwordService.HashPassword(dto.Password),
            Location = dto.Location,
            Preferences = "[]", // Explicitly set default
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);

        // Generate token
        var token = _jwtTokenService.GenerateToken(user.Id, user.Email);

        return new AuthResponseDto(token, MapToUserDto(user));
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        // Find user by email
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
        {
            return null;
        }

        // Verify password
        if (!_passwordService.VerifyPassword(dto.Password, user.PasswordHash))
        {
            return null;
        }

        // Generate token
        var token = _jwtTokenService.GenerateToken(user.Id, user.Email);

        return new AuthResponseDto(token, MapToUserDto(user));
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user == null ? null : MapToUserDto(user);
    }

    private static UserDto MapToUserDto(User user)
    {
        // FIX: Handle null or empty preferences safely
        List<string> preferences;
        try
        {
            preferences = string.IsNullOrEmpty(user.Preferences)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(user.Preferences) ?? new List<string>();
        }
        catch (JsonException)
        {
            // If JSON is invalid, return empty list
            preferences = new List<string>();
        }
        
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
}