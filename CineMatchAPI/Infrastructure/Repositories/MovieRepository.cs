using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using CineMatchAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CineMatchAPI.Infrastructure.Repositories;

public class MovieRepository : IMovieRepository
{
    private readonly CineMatchDbContext _context;

    public MovieRepository(CineMatchDbContext context)
    {
        _context = context;
    }

    public async Task<Movie?> GetByIdAsync(string id)
    {
        return await _context.Movies.FindAsync(id);
    }

    public async Task<IEnumerable<Movie>> GetByGenreAsync(string genre, int page, int pageSize)
    {
        // Calculate how many records to skip based on page number
        // Page 1 skips 0, page 2 skips pageSize, page 3 skips pageSize * 2, etc.
        var skip = (page - 1) * pageSize;

        // Build a query that filters by genre and orders randomly
        // OrderBy(m => Guid.NewGuid()) gives us a random shuffle
        // This ensures users see different movies each time
        return await _context.Movies
            .Where(m => m.Genre == genre)
            .OrderBy(m => Guid.NewGuid())
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Movie> CreateAsync(Movie movie)
    {
        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();
        return movie;
    }

    public async Task<Movie> UpdateAsync(Movie movie)
    {
        _context.Entry(movie).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return movie;
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _context.Movies.AnyAsync(m => m.Id == id);
    }
}