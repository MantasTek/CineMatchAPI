using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CineMatchAPI.Infrastructure.Services;

public interface ITMDbService
{
    Task<List<Movie>> FetchAndCacheMoviesByGenreAsync(string genre, int pagesToFetch = 1);
    Task SeedDatabaseAsync();
}

public class TMDbService : ITMDbService
{
    private readonly HttpClient _httpClient;
    private readonly IMovieRepository _movieRepository;
    private readonly string _apiKey;
    private readonly string _baseUrl = "https://api.themoviedb.org/3";

    // TMDb genre IDs
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

    public async Task<List<Movie>> FetchAndCacheMoviesByGenreAsync(string genre, int pagesToFetch = 1)
    {
        if (!_genreMap.TryGetValue(genre, out int genreId))
        {
            return new List<Movie>();
        }

        var movies = new List<Movie>();

        // Fetch multiple pages if requested
        // This allows us to cache more movies at once, which is useful for genres with many films
        for (int page = 1; page <= pagesToFetch; page++)
        {
            var url = $"{_baseUrl}/discover/movie?api_key={_apiKey}&with_genres={genreId}&sort_by=popularity.desc&page={page}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                // Log the error but continue with what we've fetched so far
                // This prevents a single failed page from breaking the entire fetch operation
                continue;
            }

            var content = await response.Content.ReadAsStringAsync();
            
            // Configure JsonSerializerOptions to use our custom converter
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new NullableDateTimeConverter() }
            };
            
            var result = JsonSerializer.Deserialize<TMDbResponse>(content, options);

            foreach (var tmdbMovie in result?.Results ?? new List<TMDbMovie>())
            {
                // Check if already cached
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
                    // Use the year from ReleaseDate if available, otherwise default to 2000
                    Year = tmdbMovie.ReleaseDate?.Year ?? 2000,
                    ImageUrl = $"https://image.tmdb.org/t/p/w500{tmdbMovie.PosterPath}",
                    Description = tmdbMovie.Overview,
                    Runtime = await FetchMovieRuntimeAsync(tmdbMovie.Id),
                    CachedAt = DateTime.UtcNow
                };

                await _movieRepository.CreateAsync(movie);
                movies.Add(movie);
            }

            // Add a small delay between pages to respect TMDb's rate limits
            // TMDb allows 40 requests per 10 seconds, so 250ms between requests is safe
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
            await FetchAndCacheMoviesByGenreAsync(genre);
            await Task.Delay(250); // Rate limiting
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
            // Fallback to default if fetch fails
        }

        return 120; // Default runtime
    }
}

// DTOs for TMDb API responses
public class TMDbResponse
{
    public List<TMDbMovie> Results { get; set; } = new();
}

public class TMDbMovie
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    
    [JsonPropertyName("poster_path")]
    public string PosterPath { get; set; } = string.Empty;
    
    [JsonPropertyName("vote_average")]
    public double VoteAverage { get; set; }
    
    [JsonPropertyName("release_date")]
    [JsonConverter(typeof(NullableDateTimeConverter))]
    public DateTime? ReleaseDate { get; set; }
}

public class TMDbMovieDetails
{
    public int Runtime { get; set; }
}

/// <summary>
/// Custom JSON converter that handles TMDb's inconsistent date formats.
/// TMDb sometimes returns empty strings, partial dates, or unexpected formats
/// for release_date fields, which breaks standard DateTime parsing.
/// </summary>
public class NullableDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // If the value is null in JSON, return null
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        // Try to read as string
        if (reader.TokenType == JsonTokenType.String)
        {
            string? dateString = reader.GetString();
            
            // Handle empty or whitespace strings
            if (string.IsNullOrWhiteSpace(dateString))
            {
                return null;
            }

            // Try parsing with standard DateTime parser
            // This handles formats like "2023-12-25", "2023-12-25T10:00:00", etc.
            if (DateTime.TryParse(dateString, out DateTime result))
            {
                return result;
            }

            // If parsing failed, return null rather than throwing
            // This makes the API more resilient to unexpected data
            return null;
        }

        // For any other token type, return null
        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            // Write in ISO 8601 format when serializing
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}