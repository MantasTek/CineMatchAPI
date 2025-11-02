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
        
        var matchDtos = new List<MatchDto>();
        
        foreach (var m in matches)
        {
            if (m.Movie == null || m.User1 == null || m.User2 == null)
                continue;
                
            matchDtos.Add(new MatchDto(
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
        
        return matchDtos;
    }

    public async Task<IEnumerable<MatchDto>> FindPotentialMatchesAsync(string userId)
    {
        var userLikes = await _swipeRepository.GetUserLikesAsync(userId);

        // Fix: Cast to the correct type if necessary
        // Assuming userLikes is IEnumerable<Swipe> and Swipe has a MovieId property
        var likedMovieIds = userLikes.Cast<Swipe>().Select(s => s.MovieId).ToHashSet();

        if (!likedMovieIds.Any())
        {
            return Enumerable.Empty<MatchDto>();
        }

        var potentialMatches = new List<MatchDto>();
        var alreadyMatched = (await _matchRepository.GetByUserIdAsync(userId))
            .Select(m => m.User1Id == userId ? m.User2Id : m.User1Id)
            .ToHashSet();

        var allUsers = await _userRepository.GetAllAsync();

        foreach (var otherUser in allUsers.Where(u => u.Id != userId && !alreadyMatched.Contains(u.Id)))
        {
            var otherUserLikes = await _swipeRepository.GetUserLikesAsync(otherUser.Id);
            var commonMovieIds = otherUserLikes.Cast<Swipe>().Select(s => s.MovieId).Intersect(likedMovieIds).ToList();

            if (commonMovieIds.Any())
            {
                var movieId = commonMovieIds.First();
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

        return potentialMatches;
    }

    private UserDto GetOtherUser(Match match, string userId)
    {
        var otherUser = match.User1Id == userId ? match.User2 : match.User1;
        
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