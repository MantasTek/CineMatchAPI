using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using CineMatchAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CineMatchAPI.Infrastructure.Repositories;

public class SwipeRepository : ISwipeRepository
{
    private readonly CineMatchDbContext _context;

    public SwipeRepository(CineMatchDbContext context)
    {
        _context = context;
    }

    public async Task<Swipe?> GetByIdAsync(string id)
    {
        return await _context.Swipes.FindAsync(id);
    }

    public async Task<IEnumerable<Swipe>> GetByUserIdAsync(string userId)
    {
        return await _context.Swipes
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SwipedAt)
            .ToListAsync();
    }

    public async Task<Swipe?> GetUserSwipeForMovieAsync(string userId, string movieId)
    {
        // This prevents users from swiping on the same movie twice
        return await _context.Swipes
            .FirstOrDefaultAsync(s => s.UserId == userId && s.MovieId == movieId);
    }

    public async Task<IEnumerable<Swipe>> GetStarredMoviesByUserAsync(string userId)
    {
        // Here we filter for Liked == true in the database query
        // but the method name uses "starred" to align with frontend terminology
        // We also include the Movie data so we can return complete information
        return await _context.Swipes
            .Include(s => s.Movie)  // Eagerly load related movie data
            .Where(s => s.UserId == userId && s.Liked == true)
            .OrderByDescending(s => s.SwipedAt)
            .ToListAsync();
    }

    public async Task<Swipe> CreateAsync(Swipe swipe)
    {
        _context.Swipes.Add(swipe);
        await _context.SaveChangesAsync();
        return swipe;
    }

    public async Task DeleteAllByUserIdAsync(string userId)
    {
        // This supports the reset functionality
        // Find all swipes by this user and remove them
        var swipes = await _context.Swipes
            .Where(s => s.UserId == userId)
            .ToListAsync();

        _context.Swipes.RemoveRange(swipes);
        await _context.SaveChangesAsync();
    }
}