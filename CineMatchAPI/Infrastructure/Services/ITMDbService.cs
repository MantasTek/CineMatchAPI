using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CineMatchAPI.Infrastructure.Services;

public interface ITMDbService
{
    Task<List<Movie>> FetchAndCacheMoviesByGenreAsync(string genre, int pagesToFetch = 1);
    Task SeedDatabaseAsync();
}

/// <summary>
/// Service for fetching movie data from TheMovieDB API.
/// 
/// CRITICAL FIX: This updated version includes proper rate limiting handling,
/// more robust runtime fetching, and diagnostic logging to help debug issues.
/// 
/// The key issue we're fixing: When seeding hundreds of movies, we need to respect
/// TheMovieDB's rate limits and handle failures gracefully. Previously, runtime
/// values were being stored incorrectly, causing all movies to appear as "short"
/// length, which prevented proper filtering by movie length preference.
/// </summary>
public class TMDbService : ITMDbService
{
    private readonly HttpClient _httpClient;
    private readonly IMovieRepository _movieRepository;
    private readonly ILogger<TMDbService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl = "https://api.themoviedb.org/3";

    // Genre mapping from user-friendly names to TMDb genre IDs
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

    public TMDbService(
        HttpClient httpClient, 
        IMovieRepository movieRepository, 
        IConfiguration configuration,
        ILogger<TMDbService> logger)
    {
        _httpClient = httpClient;
        _movieRepository = movieRepository;
        _logger = logger;
        _apiKey = configuration["TMDb:ApiKey"] 
            ?? throw new InvalidOperationException("TMDb API key not configured");
    }

    /// <summary>
    /// Fetches and caches movies from TMDb for a specific genre.
    /// 
    /// CRITICAL FIX: Added better rate limiting and runtime fetching with retries.
    /// We now wait longer between requests and have more robust error handling.
    /// </summary>
    public async Task<List<Movie>> FetchAndCacheMoviesByGenreAsync(string genre, int pagesToFetch = 1)
    {
        if (!_genreMap.TryGetValue(genre, out int genreId))
        {
            _logger.LogWarning("Unknown genre requested: {Genre}", genre);
            return new List<Movie>();
        }

        _logger.LogInformation("Fetching {Pages} pages of {Genre} movies from TMDb", pagesToFetch, genre);
        var movies = new List<Movie>();
        int successfulFetches = 0;
        int failedFetches = 0;

        for (int page = 1; page <= pagesToFetch; page++)
        {
            try
            {
                var url = $"{_baseUrl}/discover/movie?api_key={_apiKey}&with_genres={genreId}&sort_by=popularity.desc&page={page}";
                
                _logger.LogDebug("Fetching page {Page} for genre {Genre}", page, genre);
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Failed to fetch page {Page} for genre {Genre}. Status: {Status}", 
                        page, genre, response.StatusCode);
                    continue;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new NullableDateTimeConverter() }
                };
                
                var result = JsonSerializer.Deserialize<TMDbResponse>(content, options);

                if (result?.Results == null || !result.Results.Any())
                {
                    _logger.LogInformation("No results on page {Page} for genre {Genre}", page, genre);
                    break; // No more movies available
                }

                _logger.LogInformation(
                    "Processing {Count} movies from page {Page} of {Genre}", 
                    result.Results.Count, page, genre);

                foreach (var tmdbMovie in result.Results)
                {
                    try
                    {
                        // Check if movie already exists in database
                        if (await _movieRepository.ExistsAsync(tmdbMovie.Id.ToString()))
                        {
                            _logger.LogDebug("Movie {Id} ({Title}) already exists, skipping", 
                                tmdbMovie.Id, tmdbMovie.Title);
                            continue;
                        }

                        // CRITICAL FIX: Fetch runtime with retry logic and better error handling
                        var runtime = await FetchMovieRuntimeWithRetryAsync(tmdbMovie.Id, tmdbMovie.Title);
                        
                        // DIAGNOSTIC: Log what runtime we got
                        _logger.LogDebug(
                            "Movie {Title} ({Id}): runtime = {Runtime} minutes", 
                            tmdbMovie.Title, tmdbMovie.Id, runtime);

                        var movie = new Movie
                        {
                            Id = tmdbMovie.Id.ToString(),
                            Title = tmdbMovie.Title,
                            Genre = genre,
                            Rating = tmdbMovie.VoteAverage,
                            Year = tmdbMovie.ReleaseDate?.Year ?? 2000,
                            ImageUrl = $"https://image.tmdb.org/t/p/w500{tmdbMovie.PosterPath}",
                            Description = tmdbMovie.Overview,
                            Runtime = runtime,  // This is the critical field
                            CachedAt = DateTime.UtcNow
                        };

                        await _movieRepository.CreateAsync(movie);
                        movies.Add(movie);
                        successfulFetches++;

                        // CRITICAL: Delay between each movie to respect rate limits
                        // TMDb allows 50 requests per second, but we're being more conservative
                        await Task.Delay(100); // 100ms = max 10 requests/second
                    }
                    catch (Exception ex)
                    {
                        failedFetches++;
                        _logger.LogError(ex, 
                            "Error processing movie {Title} ({Id}) from genre {Genre}", 
                            tmdbMovie.Title, tmdbMovie.Id, genre);
                    }
                }

                // Delay between pages to avoid rate limiting
                if (page < pagesToFetch)
                {
                    _logger.LogDebug("Waiting before fetching next page to respect rate limits");
                    await Task.Delay(500); // 500ms between pages
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching page {Page} for genre {Genre}", page, genre);
            }
        }

        _logger.LogInformation(
            "Completed fetching {Genre} movies. Success: {Success}, Failed: {Failed}, Total: {Total}",
            genre, successfulFetches, failedFetches, movies.Count);

        return movies;
    }

    /// <summary>
    /// Seeds the database with movies from all genres.
    /// 
    /// CRITICAL FIX: Added progress logging and better delay between genres.
    /// </summary>
    public async Task SeedDatabaseAsync()
    {
        _logger.LogInformation("Starting complete database seed from TMDb");
        _logger.LogInformation("This will fetch 5 pages per genre for {Count} genres", _genreMap.Count);
        
        var startTime = DateTime.UtcNow;
        int totalGenres = _genreMap.Count;
        int currentGenre = 0;

        foreach (var genre in _genreMap.Keys)
        {
            currentGenre++;
            _logger.LogInformation(
                "Seeding genre {Current}/{Total}: {Genre}", 
                currentGenre, totalGenres, genre);

            await FetchAndCacheMoviesByGenreAsync(genre, pagesToFetch: 5);
            
            // Longer delay between genres to be extra safe with rate limiting
            if (currentGenre < totalGenres)
            {
                _logger.LogInformation("Waiting 2 seconds before next genre to respect rate limits");
                await Task.Delay(2000);
            }
        }

        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "Database seeding completed in {Duration}. Check database for total movie count.",
            duration.ToString(@"mm\:ss"));
    }

    /// <summary>
    /// Fetches runtime for a specific movie with retry logic.
    /// 
    /// CRITICAL FIX: This is the core of the runtime fetching issue.
    /// We now retry failed requests and have much better logging.
    /// </summary>
    private async Task<int> FetchMovieRuntimeWithRetryAsync(int movieId, string movieTitle, int maxRetries = 2)
    {
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var url = $"{_baseUrl}/movie/{movieId}?api_key={_apiKey}";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var movieDetails = JsonSerializer.Deserialize<TMDbMovieDetails>(content);
                    
                    if (movieDetails?.Runtime != null && movieDetails.Runtime > 0)
                    {
                        // Sanity check: runtimes should be between 1 and 500 minutes
                        if (movieDetails.Runtime < 1 || movieDetails.Runtime > 500)
                        {
                            _logger.LogWarning(
                                "Movie {Title} has suspicious runtime: {Runtime} minutes. Using default.",
                                movieTitle, movieDetails.Runtime);
                            return 120; // Default to 2 hours
                        }

                        _logger.LogTrace(
                            "Successfully fetched runtime for {Title}: {Runtime} minutes",
                            movieTitle, movieDetails.Runtime);
                        
                        return movieDetails.Runtime;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Movie {Title} ({Id}) has null or zero runtime in TMDb response",
                            movieTitle, movieId);
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    // Rate limited - wait longer and retry
                    _logger.LogWarning(
                        "Rate limited when fetching runtime for {Title}. Waiting and retrying (attempt {Attempt}/{Max})",
                        movieTitle, attempt + 1, maxRetries + 1);
                    
                    await Task.Delay(2000); // Wait 2 seconds before retry
                    continue; // Try again
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to fetch runtime for {Title} ({Id}). Status: {Status}. Attempt {Attempt}/{Max}",
                        movieTitle, movieId, response.StatusCode, attempt + 1, maxRetries + 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Exception fetching runtime for {Title} ({Id}). Attempt {Attempt}/{Max}",
                    movieTitle, movieId, attempt + 1, maxRetries + 1);
            }

            // If not the last attempt, wait before retrying
            if (attempt < maxRetries)
            {
                await Task.Delay(1000); // Wait 1 second between retries
            }
        }

        // All attempts failed - use default
        _logger.LogWarning(
            "All attempts failed to fetch runtime for {Title} ({Id}). Using default 120 minutes.",
            movieTitle, movieId);
        
        return 120; // Default to 2 hours for feature films
    }
}

// Response models for deserializing TMDb API responses
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
    [JsonPropertyName("runtime")]
    public int Runtime { get; set; }
}

/// <summary>
/// Custom JSON converter to handle TMDb's date format.
/// TMDb returns dates as strings in "YYYY-MM-DD" format, which might be empty or invalid.
/// </summary>
public class NullableDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            string? dateString = reader.GetString();
            
            if (string.IsNullOrWhiteSpace(dateString))
            {
                return null;
            }

            if (DateTime.TryParse(dateString, out DateTime date))
            {
                return date;
            }

            return null;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}