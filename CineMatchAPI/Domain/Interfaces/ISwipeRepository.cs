using CineMatchAPI.Domain.Entities;

namespace CineMatchAPI.Domain.Interfaces
{
    public interface ISwipeRepository
    {
        Task<Swipe?> GetByIdAsync(string id);
        Task<IEnumerable<Swipe>> GetByUserIdAsync(string userId);
        Task<Swipe?> GetUserSwipeForMovieAsync(string userId, string movieId);
        
        Task<IEnumerable<Swipe>> GetStarredMoviesByUserAsync(string userId);
        
        Task<Swipe> CreateAsync(Swipe swipe);
        Task DeleteAllByUserIdAsync(string userId);
    }
}