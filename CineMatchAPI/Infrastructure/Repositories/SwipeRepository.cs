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
            .ToListAsync();
    }

    public async Task<Swipe?> GetUserSwipeForMovieAsync(string userId, string movieId)
    {
        return await _context.Swipes
            .FirstOrDefaultAsync(s => s.UserId == userId && s.MovieId == movieId);
    }

    public async Task<IEnumerable<Swipe>> GetStarredMoviesByUserAsync(string userId)
    {
        return await _context.Swipes
            .Include(s => s.Movie)
            .Where(s => s.UserId == userId && s.Liked)
            .OrderByDescending(s => s.SwipedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Swipe>> GetSwipesByMovieIdAsync(string movieId)
    {
        return await _context.Swipes
            .Where(s => s.MovieId == movieId)
            .ToListAsync();
    }

    public async Task<Swipe> CreateAsync(Swipe swipe)
    {
        _context.Swipes.Add(swipe);
        await _context.SaveChangesAsync();
        return swipe;
    }

    public async Task<Swipe> UpdateAsync(Swipe swipe)
    {
        _context.Entry(swipe).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return swipe;
    }

    public async Task DeleteAsync(string id)
    {
        var swipe = await GetByIdAsync(id);
        if (swipe != null)
        {
            _context.Swipes.Remove(swipe);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAllByUserIdAsync(string userId)
    {
        var swipes = await _context.Swipes
            .Where(s => s.UserId == userId)
            .ToListAsync();
        
        _context.Swipes.RemoveRange(swipes);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<object>> GetUserLikesAsync(string userId)
    {
        // Return liked swipes as objects (to match current interface)
        var likes = await _context.Swipes
            .Where(s => s.UserId == userId && s.Liked)
            .ToListAsync();
        return likes.Cast<object>();
    }
}