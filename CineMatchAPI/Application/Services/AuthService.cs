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
        if (await _userRepository.EmailExistsAsync(dto.Email))
        {
            return null;
        }

        // Create new user with NULL preferences (user must complete onboarding)
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = _passwordService.HashPassword(dto.Password),
            Location = dto.Location,
            Preferences = null, // NULL = onboarding not completed
            MovieLength = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);

        var token = _jwtTokenService.GenerateToken(user.Id, user.Email);

        return new AuthResponseDto(token, MapToUserDto(user));
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
        {
            return null;
        }

        if (!_passwordService.VerifyPassword(dto.Password, user.PasswordHash))
        {
            return null;
        }

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
        // Return null for preferences if not yet set (onboarding incomplete)
        List<string>? preferences = null;
        
        if (!string.IsNullOrEmpty(user.Preferences))
        {
            try
            {
                preferences = JsonSerializer.Deserialize<List<string>>(user.Preferences);
            }
            catch (JsonException)
            {
                // If JSON is invalid, treat as not set
                preferences = null;
            }
        }
        
        return new UserDto(
            user.Id,
            user.Name,
            user.Email,
            user.Location,
            user.Bio,
            user.AvatarUrl,
            preferences, // null if not set, List<string> if set
            user.MovieLength
        );
    }
}