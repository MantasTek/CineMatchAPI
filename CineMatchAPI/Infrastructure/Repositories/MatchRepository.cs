using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using CineMatchAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CineMatchAPI.Infrastructure.Repositories;

public class MatchRepository : IMatchRepository
{
    private readonly CineMatchDbContext _context;

    public MatchRepository(CineMatchDbContext context)
    {
        _context = context;
    }

    public async Task<Match?> GetByIdAsync(string id)
    {
        // Include related data so we can show match details
        return await _context.Matches
            .Include(m => m.User1)
            .Include(m => m.User2)
            .Include(m => m.Movie)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<Match>> GetByUserIdAsync(string userId)
    {
        // A user could be either User1 or User2 in a match
        // We need to check both columns
        return await _context.Matches
            .Include(m => m.User1)
            .Include(m => m.User2)
            .Include(m => m.Movie)
            .Where(m => m.User1Id == userId || m.User2Id == userId)
            .OrderByDescending(m => m.MatchedAt)
            .ToListAsync();
    }

    public async Task<Match?> GetMatchBetweenUsersForMovieAsync(string user1Id, string user2Id, string movieId)
    {
        // Check if a match already exists between these users for this movie
        // The users could be in either order (User1/User2), so we check both
        return await _context.Matches
            .FirstOrDefaultAsync(m => 
                m.MovieId == movieId &&
                ((m.User1Id == user1Id && m.User2Id == user2Id) ||
                 (m.User1Id == user2Id && m.User2Id == user1Id)));
    }

    public async Task<Match> CreateAsync(Match match)
    {
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();
        return match;
    }

    public async Task DeleteAllByUserIdAsync(string userId)
    {
        // For the reset feature - delete all matches involving this user
        var matches = await _context.Matches
            .Where(m => m.User1Id == userId || m.User2Id == userId)
            .ToListAsync();

        _context.Matches.RemoveRange(matches);
        await _context.SaveChangesAsync();
    }

    public Task<IEnumerable<object>> GetMatchesByUserIdAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(object id)
    {
        throw new NotImplementedException();
    }
}