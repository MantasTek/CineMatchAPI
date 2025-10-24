using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Domain.Interfaces;

namespace CineMatchAPI.Application.Services;

public interface IMovieService
{
    Task<IEnumerable<MovieDto>> GetMoviesByPreferencesAsync(string genre, string movieLength, int page = 1, int pageSize = 20);
}

public class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;

    public MovieService(IMovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
    }

    public async Task<IEnumerable<MovieDto>> GetMoviesByPreferencesAsync(
        string genre, 
        string movieLength, 
        int page = 1, 
        int pageSize = 20)
    {
        var movies = await _movieRepository.GetByGenreAsync(genre, page, pageSize);
        
        // Filter by runtime based on length preference
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