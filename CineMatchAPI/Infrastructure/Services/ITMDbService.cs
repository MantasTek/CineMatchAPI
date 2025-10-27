using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CineMatchAPI.Infrastructure.Services;

public interface ITMDbService
{
    Task<List<Movie>> FetchAndCacheMoviesByGenreAsync(string genre);
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

    public async Task<List<Movie>> FetchAndCacheMoviesByGenreAsync(string genre)
    {
        if (!_genreMap.TryGetValue(genre, out int genreId))
        {
            return new List<Movie>();
        }

        var url = $"{_baseUrl}/discover/movie?api_key={_apiKey}&with_genres={genreId}&sort_by=popularity.desc&page=1";
        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"TMDb API request failed: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Raw API response (first 500 chars): {content.Substring(0, Math.Min(500, content.Length))}");
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var result = JsonSerializer.Deserialize<TMDbResponse>(content, options);

        Console.WriteLine($"Fetched {result?.Results?.Count ?? 0} movies for genre {genre}");

        var movies = new List<Movie>();
        
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

        return movies;
    }

    public async Task SeedDatabaseAsync()
    {
        foreach (var genre in _genreMap.Keys)
        {
            await FetchAndCacheMoviesByGenreAsync(genre);
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
                var movie = JsonSerializer.Deserialize<TMDbMovieDetails>(content);
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