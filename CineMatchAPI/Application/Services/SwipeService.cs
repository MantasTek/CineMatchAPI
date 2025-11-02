using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;

namespace CineMatchAPI.Application.Services;

public interface ISwipeService
{
    Task<SwipeResultDto> SwipeMovieAsync(string userId, SwipeDto dto);
    Task<IEnumerable<MovieDto>> GetUserStarredMoviesAsync(string userId);
    Task<int> GetSwipeCountAsync(string userId);
}

public class SwipeService : ISwipeService
{
    private readonly ISwipeRepository _swipeRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IUserRepository _userRepository;

    public SwipeService(
        ISwipeRepository swipeRepository,
        IMovieRepository movieRepository,
        IMatchRepository matchRepository,
        IUserRepository userRepository)
    {
        _swipeRepository = swipeRepository;
        _movieRepository = movieRepository;
        _matchRepository = matchRepository;
        _userRepository = userRepository;
    }

    public async Task<SwipeResultDto> SwipeMovieAsync(string userId, SwipeDto dto)
    {
        var existing = await _swipeRepository.GetUserSwipeForMovieAsync(userId, dto.MovieId);
        if (existing != null)
        {
            return new SwipeResultDto(false, false, null);
        }

        var swipe = new Swipe
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            MovieId = dto.MovieId,
            Liked = dto.Liked,
            SwipedAt = DateTime.UtcNow
        };

        await _swipeRepository.CreateAsync(swipe);

        if (dto.Liked)
        {
            var matchResult = await CheckAndCreateMatchesAsync(userId, dto.MovieId);
            if (matchResult.matched)
            {
                return new SwipeResultDto(true, true, matchResult.matchId);
            }
        }

        return new SwipeResultDto(true, false, null);
    }

    public async Task<IEnumerable<MovieDto>> GetUserStarredMoviesAsync(string userId)
    {
        var swipes = await _swipeRepository.GetStarredMoviesByUserAsync(userId);
        return swipes.Select(s => new MovieDto(
            s.Movie.Id,
            s.Movie.Title,
            s.Movie.Genre,
            s.Movie.Rating,
            s.Movie.Year,
            s.Movie.ImageUrl,
            s.Movie.Description,
            s.Movie.Runtime
        ));
    }

    public async Task<int> GetSwipeCountAsync(string userId)
    {
        var swipes = await _swipeRepository.GetByUserIdAsync(userId);
        return swipes.Count();
    }

    private async Task<(bool matched, string? matchId)> CheckAndCreateMatchesAsync(string userId, string movieId)
    {
        // Get all users who also liked this movie
        var allSwipesForMovie = await _swipeRepository.GetSwipesByMovieIdAsync(movieId);
        
        var otherUsersWhoLiked = allSwipesForMovie
            .Where(s => s.UserId != userId && s.Liked)
            .Select(s => s.UserId)
            .ToList();

        foreach (var otherUserId in otherUsersWhoLiked)
        {
            // Check if match already exists
            var existingMatch = await _matchRepository.GetMatchBetweenUsersForMovieAsync(userId, otherUserId, movieId);
            if (existingMatch != null) continue;

            // Create new match
            var match = new Match
            {
                Id = Guid.NewGuid().ToString(),
                User1Id = userId,
                User2Id = otherUserId,
                MovieId = movieId,
                MatchedAt = DateTime.UtcNow
            };

            await _matchRepository.CreateAsync(match);
            
            // Return the first match created
            return (true, match.Id);
        }

        return (false, null);
    }
}

public record SwipeResultDto(bool Success, bool Matched, string? MatchId);