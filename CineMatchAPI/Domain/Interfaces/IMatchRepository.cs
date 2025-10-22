using CineMatchAPI.Domain.Entities;

namespace CineMatchAPI.Domain.Interfaces;

public interface IMatchRepository
{
    Task<Match?> GetByIdAsync(string id);
    Task<IEnumerable<Match>> GetByUserIdAsync(string userId);
    Task<Match?> GetMatchBetweenUsersForMovieAsync(string user1Id, string user2Id, string movieId);
    Task<Match> CreateAsync(Match match);
    Task DeleteAllByUserIdAsync(string userId); // For reset feature
}