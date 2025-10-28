using CineMatchAPI.Domain.Entities;

namespace CineMatchAPI.Domain.Interfaces;

public interface ISwipeRepository
{
    Task<Swipe?> GetByIdAsync(string id);
    Task<IEnumerable<Swipe>> GetByUserIdAsync(string userId);
    Task<Swipe?> GetUserSwipeForMovieAsync(string userId, string movieId);
    Task<IEnumerable<Swipe>> GetStarredMoviesByUserAsync(string userId);
    Task<IEnumerable<Swipe>> GetSwipesByMovieIdAsync(string movieId);
    Task<Swipe> CreateAsync(Swipe swipe);
    Task<Swipe> UpdateAsync(Swipe swipe);
    Task DeleteAsync(string id);
    Task DeleteAllByUserIdAsync(string userId);
}