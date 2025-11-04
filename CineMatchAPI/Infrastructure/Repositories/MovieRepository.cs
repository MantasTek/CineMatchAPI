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
        if (string.IsNullOrWhiteSpace(genre) || page <= 0 || pageSize <= 0)
        {
            return Enumerable.Empty<Movie>();
        }

        var skip = (page - 1) * pageSize;
        var genreNormalized = genre.ToLowerInvariant();

        return await _context.Movies
            .Where(m => m.Genre.ToLower() == genreNormalized)
            .OrderByDescending(m => m.Rating)
            .ThenBy(m => m.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Movie>> GetByGenreExcludingSwipedAsync(
        string genre, 
        string userId, 
        int page, 
        int pageSize)
    {
        if (string.IsNullOrWhiteSpace(genre) || page <= 0 || pageSize <= 0)
        {
            return Enumerable.Empty<Movie>();
        }

        var skip = (page - 1) * pageSize;
        var genreNormalized = genre.ToLowerInvariant();

        var swipedMovieIds = await _context.Swipes
            .Where(s => s.UserId == userId)
            .Select(s => s.MovieId)
            .ToListAsync();

        return await _context.Movies
            .Where(m => m.Genre.ToLower() == genreNormalized && !swipedMovieIds.Contains(m.Id))
            .OrderByDescending(m => m.Rating)
            .ThenBy(m => m.Id)
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