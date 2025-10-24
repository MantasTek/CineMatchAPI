using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;

namespace CineMatchAPI.Application.Services;

public interface ISwipeService
{
    Task<bool> SwipeMovieAsync(string userId, SwipeDto dto);
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

    public async Task<bool> SwipeMovieAsync(string userId, SwipeDto dto)
    {
        // Check if already swiped
        var existing = await _swipeRepository.GetUserSwipeForMovieAsync(userId, dto.MovieId);
        if (existing != null) return false;

        // Create swipe
        var swipe = new Swipe
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            MovieId = dto.MovieId,
            Liked = dto.Stared,
            SwipedAt = DateTime.UtcNow
        };

        await _swipeRepository.CreateAsync(swipe);

        // If liked, check for potential matches
        if (dto.Stared)
        {
            await CheckAndCreateMatchesAsync(userId, dto.MovieId);
        }

        return true;
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

    private async Task CheckAndCreateMatchesAsync(string userId, string movieId)
    {
        // Find other users who liked the same movie
        var allSwipes = await _swipeRepository.GetByUserIdAsync(userId); // This gets all by user, need to query differently
        
        // For now, we'll implement a simple version
        // In production, you'd query the database for other users who liked this specific movie
        // and aren't already matched with this user
    }
}