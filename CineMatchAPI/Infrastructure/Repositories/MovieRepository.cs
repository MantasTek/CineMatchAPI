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
        var skip = (page - 1) * pageSize;

        // Load movies into memory first, then shuffle
        var allMovies = await _context.Movies
            .Where(m => m.Genre == genre)
            .ToListAsync();

        // Shuffle in-memory using Random
        var random = new Random();
        var shuffled = allMovies.OrderBy(x => random.Next()).ToList();

        // Apply pagination
        return shuffled
            .Skip(skip)
            .Take(pageSize)
            .ToList();
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