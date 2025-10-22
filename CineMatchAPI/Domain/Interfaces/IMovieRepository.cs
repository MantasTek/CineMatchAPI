using CineMatchAPI.Domain.Entities;

namespace CineMatchAPI.Domain.Interfaces;

public interface IMovieRepository
{
    Task<Movie?> GetByIdAsync(string id);
    Task<IEnumerable<Movie>> GetByGenreAsync(string genre, int page, int pageSize);
    Task<Movie> CreateAsync(Movie movie);
    Task<Movie> UpdateAsync(Movie movie);
    Task<bool> ExistsAsync(string id);
}