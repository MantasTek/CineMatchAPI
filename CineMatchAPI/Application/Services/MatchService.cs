using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;

namespace CineMatchAPI.Application.Services;

public interface IMatchService
{
    Task<IEnumerable<MatchDto>> GetUserMatchesAsync(string userId);
    Task<IEnumerable<MatchDto>> FindPotentialMatchesAsync(string userId);
}

public class MatchService : IMatchService
{
    private readonly IMatchRepository _matchRepository;
    private readonly ISwipeRepository _swipeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMovieRepository _movieRepository;

    public MatchService(
        IMatchRepository matchRepository,
        ISwipeRepository swipeRepository,
        IUserRepository userRepository,
        IMovieRepository movieRepository)
    {
        _matchRepository = matchRepository;
        _swipeRepository = swipeRepository;
        _userRepository = userRepository;
        _movieRepository = movieRepository;
    }

    public async Task<IEnumerable<MatchDto>> GetUserMatchesAsync(string userId)
    {
        var matches = await _matchRepository.GetByUserIdAsync(userId);
        
        return matches.Select(m => new MatchDto(
            m.Id,
            GetOtherUser(m, userId),
            new MovieDto(
                m.Movie.Id,
                m.Movie.Title,
                m.Movie.Genre,
                m.Movie.Rating,
                m.Movie.Year,
                m.Movie.ImageUrl,
                m.Movie.Description,
                m.Movie.Runtime
            ),
            m.MatchedAt
        ));
    }

    public async Task<IEnumerable<MatchDto>> FindPotentialMatchesAsync(string userId)
    {
        // Get user's liked movies
        var userSwipes = await _swipeRepository.GetStarredMoviesByUserAsync(userId);
        var likedMovieIds = userSwipes.Select(s => s.MovieId).ToList();

        if (!likedMovieIds.Any())
        {
            return Enumerable.Empty<MatchDto>();
        }

        var potentialMatches = new List<MatchDto>();

        // For each liked movie, find other users who also liked it
        foreach (var movieId in likedMovieIds)
        {
            var existingMatch = await _matchRepository.GetMatchBetweenUsersForMovieAsync(userId, userId, movieId);
            if (existingMatch != null) continue;

            // This is a simplified version - in production you'd query the database more efficiently
            var allUsers = await _userRepository.GetAllAsync();
            
            foreach (var otherUser in allUsers.Where(u => u.Id != userId))
            {
                var otherUserSwipe = await _swipeRepository.GetUserSwipeForMovieAsync(otherUser.Id, movieId);
                
                if (otherUserSwipe?.Liked == true)
                {
                    // Check if match already exists
                    var matchExists = await _matchRepository.GetMatchBetweenUsersForMovieAsync(userId, otherUser.Id, movieId);
                    if (matchExists == null)
                    {
                        var movie = await _movieRepository.GetByIdAsync(movieId);
                        if (movie != null)
                        {
                            potentialMatches.Add(new MatchDto(
                                Guid.NewGuid().ToString(),
                                new UserDto(
                                    otherUser.Id,
                                    otherUser.Name,
                                    otherUser.Email,
                                    otherUser.Location,
                                    otherUser.Bio,
                                    otherUser.AvatarUrl,
                                    new List<string>(),
                                    otherUser.MovieLength
                                ),
                                new MovieDto(
                                    movie.Id,
                                    movie.Title,
                                    movie.Genre,
                                    movie.Rating,
                                    movie.Year,
                                    movie.ImageUrl,
                                    movie.Description,
                                    movie.Runtime
                                ),
                                DateTime.UtcNow
                            ));
                        }
                    }
                }
            }
        }

        return potentialMatches;
    }

    private UserDto GetOtherUser(Match match, string currentUserId)
    {
        var otherUser = match.User1Id == currentUserId ? match.User2 : match.User1;
        return new UserDto(
            otherUser.Id,
            otherUser.Name,
            otherUser.Email,
            otherUser.Location,
            otherUser.Bio,
            otherUser.AvatarUrl,
            new List<string>(),
            otherUser.MovieLength
        );
    }
}