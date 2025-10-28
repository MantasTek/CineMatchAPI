using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CineMatchAPI.Infrastructure.Services;

public interface ITMDbService
{
    Task<List<Movie>> FetchAndCacheMoviesByGenreAsync(string genre, int pagesToFetch = 5);
    Task SeedDatabaseAsync();
}

public class TMDbService : ITMDbService
{
    private readonly HttpClient _httpClient;
    private readonly IMovieRepository _movieRepository;
    private readonly string _apiKey;
    private readonly string _baseUrl = "https://api.themoviedb.org/3";

    private readonly Dictionary<string, int> _genreMap = new()
    {
        { "Action", 28 },
        { "Comedy", 35 },
        { "Drama", 18 },
        { "Horror", 27 },
        { "Romance", 10749 },
        { "Sci-Fi", 878 },
        { "Thriller", 53 },
        { "Documentary", 99 },
        { "Animation", 16 },
        { "Mystery", 9648 }
    };

    public TMDbService(HttpClient httpClient, IMovieRepository movieRepository, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _movieRepository = movieRepository;
        _apiKey = configuration["TMDb:ApiKey"] ?? throw new InvalidOperationException("TMDb API key not configured");
    }

    public async Task<List<Movie>> FetchAndCacheMoviesByGenreAsync(string genre, int pagesToFetch = 5)
    {
        if (!_genreMap.TryGetValue(genre, out int genreId))
        {
            return new List<Movie>();
        }

        var movies = new List<Movie>();

        // Fetch multiple pages to get more variety
        for (int page = 1; page <= pagesToFetch; page++)
        {
            var url = $"{_baseUrl}/discover/movie?api_key={_apiKey}&with_genres={genreId}&sort_by=popularity.desc&page={page}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var result = JsonSerializer.Deserialize<TMDbResponse>(content, options);

            foreach (var tmdbMovie in result?.Results ?? new List<TMDbMovie>())
            {
                if (await _movieRepository.ExistsAsync(tmdbMovie.Id.ToString()))
                {
                    continue;
                }

                var movie = new Movie
                {
                    Id = tmdbMovie.Id.ToString(),
                    Title = tmdbMovie.Title,
                    Genre = genre,
                    Rating = tmdbMovie.VoteAverage,
                    Year = tmdbMovie.ReleaseDate?.Year ?? 2000,
                    ImageUrl = $"https://image.tmdb.org/t/p/w500{tmdbMovie.PosterPath}",
                    Description = tmdbMovie.Overview,
                    Runtime = await FetchMovieRuntimeAsync(tmdbMovie.Id),
                    CachedAt = DateTime.UtcNow
                };

                await _movieRepository.CreateAsync(movie);
                movies.Add(movie);
            }

            // Rate limiting between pages
            if (page < pagesToFetch)
            {
                await Task.Delay(250);
            }
        }

        return movies;
    }

    public async Task SeedDatabaseAsync()
    {
        foreach (var genre in _genreMap.Keys)
        {
            await FetchAndCacheMoviesByGenreAsync(genre, pagesToFetch: 5);
            await Task.Delay(250);
        }
    }

    private async Task<int> FetchMovieRuntimeAsync(int movieId)
    {
        try
        {
            var url = $"{_baseUrl}/movie/{movieId}?api_key={_apiKey}";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var movie = JsonSerializer.Deserialize<TMDbMovieDetails>(content, options);
                return movie?.Runtime ?? 120;
            }
        }
        catch
        {
        }

        return 120;
    }
}

public class TMDbResponse
{
    [JsonPropertyName("results")]
    public List<TMDbMovie> Results { get; set; } = new();
}

public class TMDbMovie
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("overview")]
    public string Overview { get; set; } = string.Empty;
    
    [JsonPropertyName("poster_path")]
    public string PosterPath { get; set; } = string.Empty;
    
    [JsonPropertyName("vote_average")]
    public double VoteAverage { get; set; }
    
    [JsonPropertyName("release_date")]
    public DateTime? ReleaseDate { get; set; }
}

public class TMDbMovieDetails
{
    [JsonPropertyName("runtime")]
    public int Runtime { get; set; }
}