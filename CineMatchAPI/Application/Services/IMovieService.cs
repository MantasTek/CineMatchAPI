using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Domain.Interfaces;
using CineMatchAPI.Infrastructure.Services;

namespace CineMatchAPI.Application.Services;

public interface IMovieService
{
    Task<IEnumerable<MovieDto>> GetMoviesByPreferencesAsync(string genre, string movieLength, int page = 1, int pageSize = 20);
    Task SeedMoviesAsync();
}

public class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;
    private readonly ITMDbService _tmdbService;

    public MovieService(IMovieRepository movieRepository, ITMDbService tmdbService)
    {
        _movieRepository = movieRepository;
        _tmdbService = tmdbService;
    }

    public async Task<IEnumerable<MovieDto>> GetMoviesByPreferencesAsync(
        string genre, 
        string movieLength, 
        int page = 1, 
        int pageSize = 20)
    {
        // Try to get from cache first
        var movies = await _movieRepository.GetByGenreAsync(genre, page, pageSize);
        
        // If no movies cached, fetch from TMDb
        if (!movies.Any())
        {
            await _tmdbService.FetchAndCacheMoviesByGenreAsync(genre);
            movies = await _movieRepository.GetByGenreAsync(genre, page, pageSize);
        }
        
        // Filter by runtime
        var filtered = FilterByLength(movies, movieLength);
        
        return filtered.Select(m => new MovieDto(
            m.Id,
            m.Title,
            m.Genre,
            m.Rating,
            m.Year,
            m.ImageUrl,
            m.Description,
            m.Runtime
        ));
    }

    public async Task SeedMoviesAsync()
    {
        await _tmdbService.SeedDatabaseAsync();
    }

    private IEnumerable<Domain.Entities.Movie> FilterByLength(
        IEnumerable<Domain.Entities.Movie> movies, 
        string movieLength)
    {
        return movieLength.ToLower() switch
        {
            "short" => movies.Where(m => m.Runtime < 90),
            "medium" => movies.Where(m => m.Runtime >= 90 && m.Runtime <= 130),
            "long" => movies.Where(m => m.Runtime > 130),
            _ => movies
        };
    }
}