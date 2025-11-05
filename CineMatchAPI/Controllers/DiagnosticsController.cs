using CineMatchAPI.Application.Services;
using CineMatchAPI.Domain.Interfaces;
using CineMatchAPI.Infrastructure.Data;
using CineMatchAPI.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineMatchAPI.Controllers;

/// <summary>
/// Diagnostic Controller - Your Complete System Health Check Tool
/// 
/// This controller provides comprehensive diagnostics for your CineMatch application.
/// It helps you understand exactly what's happening with your database, TMDb API,
/// and movie availability at every step of the data flow.
/// 
/// Use this to troubleshoot why movies aren't showing up on your frontend.
/// </summary>
[ApiController]
[Route("api/diagnostics")]
public class DiagnosticsController : ControllerBase
{
    private readonly CineMatchDbContext _db;
    private readonly IMovieRepository _movieRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISwipeRepository _swipeRepository;
    private readonly ITMDbService _tmdbService;
    private readonly IMovieService _movieService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(
        CineMatchDbContext db,
        IMovieRepository movieRepository,
        IUserRepository userRepository,
        ISwipeRepository swipeRepository,
        ITMDbService tmdbService,
        IMovieService movieService,
        IConfiguration configuration,
        ILogger<DiagnosticsController> logger)
    {
        _db = db;
        _movieRepository = movieRepository;
        _userRepository = userRepository;
        _swipeRepository = swipeRepository;
        _tmdbService = tmdbService;
        _movieService = movieService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// STEP 1: Complete System Health Check
    /// 
    /// This endpoint checks EVERYTHING in your system and reports back detailed status.
    /// Call this FIRST to get a complete picture of what's working and what's not.
    /// 
    /// GET: api/diagnostics/health
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> HealthCheck()
    {
        var report = new
        {
            timestamp = DateTime.UtcNow,
            databaseConnection = await CheckDatabaseConnection(),
            tmdbConfiguration = CheckTMDbConfiguration(),
            movieData = await CheckMovieData(),
            userData = await CheckUserData(),
            swipeData = await CheckSwipeData()
        };

        _logger.LogInformation("Health check completed: {Report}", System.Text.Json.JsonSerializer.Serialize(report));

        return Ok(report);
    }

    /// <summary>
    /// STEP 2: Detailed Movie Statistics
    /// 
    /// Shows you exactly what movies exist in your database, broken down by genre.
    /// This helps you understand if seeding worked and what movies are available.
    /// 
    /// GET: api/diagnostics/movies/stats
    /// </summary>
    [HttpGet("movies/stats")]
    public async Task<IActionResult> GetMovieStatistics()
    {
        try
        {
            var totalMovies = await _db.Movies.CountAsync();
            
            // Group movies by genre to see distribution
            var moviesByGenre = await _db.Movies
                .GroupBy(m => m.Genre)
                .Select(g => new
                {
                    genre = g.Key,
                    count = g.Count(),
                    averageRating = g.Average(m => m.Rating),
                    sampleTitles = g.Take(3).Select(m => m.Title).ToList()
                })
                .OrderByDescending(g => g.count)
                .ToListAsync();

            // Check runtime distribution
            // Note: Using @ prefix for 'short' and 'long' because they're C# reserved keywords
            var runtimeStats = new
            {
                @short = await _db.Movies.CountAsync(m => m.Runtime < 90),
                medium = await _db.Movies.CountAsync(m => m.Runtime >= 90 && m.Runtime <= 130),
                @long = await _db.Movies.CountAsync(m => m.Runtime > 130)
            };

            var report = new
            {
                totalMovies,
                moviesByGenre,
                runtimeDistribution = runtimeStats,
                oldestCached = await _db.Movies.OrderBy(m => m.CachedAt).Select(m => m.CachedAt).FirstOrDefaultAsync(),
                newestCached = await _db.Movies.OrderByDescending(m => m.CachedAt).Select(m => m.CachedAt).FirstOrDefaultAsync()
            };

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting movie statistics");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// STEP 3: Test Movie Fetching for a Specific User
    /// 
    /// Simulates exactly what happens when a user tries to get movies on the swipe page.
    /// This helps you see if the problem is in the query logic or the data itself.
    /// 
    /// GET: api/diagnostics/movies/test?userId=xxx&genre=Action&length=medium
    /// </summary>
    [HttpGet("movies/test")]
    public async Task<IActionResult> TestMovieFetching(
        [FromQuery] string? userId = null,
        [FromQuery] string? genre = "Action",
        [FromQuery] string? length = "medium")
    {
        try
        {
            // Use a test user ID if none provided
            userId ??= "test-diagnostic-user";

            _logger.LogInformation(
                "Testing movie fetch for userId: {UserId}, genre: {Genre}, length: {Length}",
                userId, genre, length);

            // Step 1: Check if user exists
            var userExists = await _userRepository.ExistsAsync(userId);
            _logger.LogInformation("User exists: {UserExists}", userExists);

            // Step 2: Get movies using the same method your MovieController uses
            var movies = await _movieService.GetMoviesByPreferencesAsync(
                userId, 
                genre!, 
                length!, 
                page: 1, 
                pageSize: 20);

            var movieList = movies.ToList();
            
            _logger.LogInformation("Movies fetched: {Count}", movieList.Count);

            // Step 3: Check what movies exist in DB for this genre (before filtering)
            var allGenreMovies = await _db.Movies
                .Where(m => m.Genre.ToLower() == genre!.ToLower())
                .CountAsync();

            // Step 4: Check how many movies user has already swiped
            var userSwipes = await _db.Swipes
                .Where(s => s.UserId == userId)
                .CountAsync();

            var report = new
            {
                request = new { userId, genre, length },
                results = new
                {
                    moviesReturned = movieList.Count,
                    movies = movieList.Take(5).Select(m => new
                    {
                        m.Id,
                        m.Title,
                        m.Genre,
                        m.Runtime,
                        m.Rating
                    }).ToList()
                },
                databaseState = new
                {
                    totalMoviesInGenre = allGenreMovies,
                    userSwipeCount = userSwipes,
                    userHasSwipedAllMovies = userSwipes >= allGenreMovies
                },
                possibleIssues = GeneratePossibleIssues(movieList.Count, allGenreMovies, userSwipes)
            };

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing movie fetching");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// STEP 4: Force Re-Seed Database
    /// 
    /// This deletes all movies and re-fetches them from TMDb.
    /// Use this if you suspect your database has bad or incomplete data.
    /// 
    /// WARNING: This will delete all existing movies and swipes!
    /// 
    /// POST: api/diagnostics/reseed
    /// </summary>
    [HttpPost("reseed")]
    public async Task<IActionResult> ReseedDatabase([FromQuery] bool confirm = false)
    {
        if (!confirm)
        {
            return BadRequest(new
            {
                message = "This action will delete ALL movies and swipes from your database!",
                instruction = "Add ?confirm=true to the URL if you really want to do this",
                example = "POST api/diagnostics/reseed?confirm=true"
            });
        }

        try
        {
            _logger.LogWarning("Starting database reseed - this will delete all movies and swipes!");

            // Delete all swipes first (foreign key constraint)
            var swipesDeleted = await _db.Swipes.ExecuteDeleteAsync();
            _logger.LogInformation("Deleted {Count} swipes", swipesDeleted);

            // Delete all matches (foreign key constraint)
            var matchesDeleted = await _db.Matches.ExecuteDeleteAsync();
            _logger.LogInformation("Deleted {Count} matches", matchesDeleted);

            // Delete all movies
            var moviesDeleted = await _db.Movies.ExecuteDeleteAsync();
            _logger.LogInformation("Deleted {Count} movies", moviesDeleted);

            // Save changes
            await _db.SaveChangesAsync();

            _logger.LogInformation("Starting TMDb seeding...");
            
            // Seed fresh data from TMDb
            await _movieService.SeedMoviesAsync();

            // Check results
            var newMovieCount = await _db.Movies.CountAsync();
            _logger.LogInformation("Seeding complete! New movie count: {Count}", newMovieCount);

            return Ok(new
            {
                message = "Database reseeded successfully!",
                deleted = new
                {
                    movies = moviesDeleted,
                    swipes = swipesDeleted,
                    matches = matchesDeleted
                },
                created = new
                {
                    movies = newMovieCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reseeding database");
            return StatusCode(500, new
            {
                error = ex.Message,
                stackTrace = ex.StackTrace,
                message = "Failed to reseed database - check logs for details"
            });
        }
    }

    /// <summary>
    /// STEP 5: Test TMDb API Connection
    /// 
    /// Directly tests if you can connect to TheMovieDB API and fetch movies.
    /// This helps you verify your API key is valid and TMDb is accessible.
    /// 
    /// GET: api/diagnostics/tmdb/test
    /// </summary>
    [HttpGet("tmdb/test")]
    public async Task<IActionResult> TestTMDbConnection()
    {
        try
        {
            _logger.LogInformation("Testing TMDb API connection...");

            // Try to fetch a small sample of Action movies
            var movies = await _tmdbService.FetchAndCacheMoviesByGenreAsync("Action", pagesToFetch: 1);

            return Ok(new
            {
                success = true,
                message = "Successfully connected to TMDb API!",
                moviesFetched = movies.Count,
                sampleMovies = movies.Take(3).Select(m => new
                {
                    m.Id,
                    m.Title,
                    m.Genre,
                    m.Runtime,
                    m.Year
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TMDb API connection test failed");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to connect to TMDb API",
                error = ex.Message,
                possibleCauses = new[]
                {
                    "Invalid or expired API key",
                    "Network connectivity issues",
                    "TMDb service is down",
                    "Rate limiting (too many requests)"
                }
            });
        }
    }

    /// <summary>
    /// STEP 6: Check Database File
    /// 
    /// Verifies your SQLite database file exists and provides information about it.
    /// 
    /// GET: api/diagnostics/database/info
    /// </summary>
    [HttpGet("database/info")]
    public IActionResult GetDatabaseInfo()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            // Guard against null connection string
            if (connectionString == null)
            {
                return Ok(new
                {
                    connectionString = "NOT CONFIGURED",
                    databaseFile = "Unknown - no connection string found",
                    exists = false,
                    sizeBytes = 0,
                    sizeMB = 0.0,
                    message = "CRITICAL: Database connection string is not configured in appsettings.json!"
                });
            }
            
            // Extract database file path from connection string
            // Note: We use ! (null-forgiving operator) here because we've already checked for null above
            // and returned early if it was null. The compiler's flow analysis doesn't always recognize
            // this pattern, so we explicitly tell it "this is definitely not null at this point"
            var dbFile = "cinematch.db"; // Default name
            if (connectionString!.Contains("Data Source="))
            {
                var start = connectionString.IndexOf("Data Source=") + "Data Source=".Length;
                var end = connectionString.IndexOf(";", start);
                if (end == -1) end = connectionString.Length;
                dbFile = connectionString.Substring(start, end - start);
            }

            var fullPath = Path.GetFullPath(dbFile);
            var exists = System.IO.File.Exists(fullPath);
            var size = exists ? new FileInfo(fullPath).Length : 0;

            return Ok(new
            {
                connectionString,
                databaseFile = fullPath,
                exists,
                sizeBytes = size,
                sizeMB = Math.Round(size / 1024.0 / 1024.0, 2),
                message = exists
                    ? "Database file exists and is accessible"
                    : "Database file does not exist! This is a critical problem."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database info");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ==================== PRIVATE HELPER METHODS ====================

    private async Task<object> CheckDatabaseConnection()
    {
        try
        {
            await _db.Database.EnsureCreatedAsync();
            await _db.Database.CanConnectAsync();
            
            return new
            {
                status = "Connected",
                provider = _db.Database.ProviderName,
                message = "Database connection is working"
            };
        }
        catch (Exception ex)
        {
            return new
            {
                status = "Failed",
                error = ex.Message,
                message = "Cannot connect to database!"
            };
        }
    }

    private object CheckTMDbConfiguration()
    {
        var apiKey = _configuration["TMDb:ApiKey"];
        var baseUrl = _configuration["TMDb:BaseUrl"];

        var hasApiKey = !string.IsNullOrWhiteSpace(apiKey);
        // Using null-forgiving operator because IsNullOrWhiteSpace guarantees non-null if it returns false
        var apiKeyPreview = hasApiKey ? $"{apiKey!.Substring(0, 8)}..." : "NOT SET";

        return new
        {
            configured = hasApiKey,
            apiKey = apiKeyPreview,
            baseUrl = baseUrl ?? "NOT SET",
            message = hasApiKey
                ? "TMDb API key is configured"
                : "TMDb API key is MISSING! This is a critical problem."
        };
    }

    private async Task<object> CheckMovieData()
    {
        try
        {
            var totalMovies = await _db.Movies.CountAsync();
            var genres = await _db.Movies.Select(m => m.Genre).Distinct().ToListAsync();

            return new
            {
                totalMovies,
                uniqueGenres = genres.Count,
                genres,
                message = totalMovies > 0
                    ? $"Database has {totalMovies} movies across {genres.Count} genres"
                    : "WARNING: No movies in database! Seeding may have failed."
            };
        }
        catch (Exception ex)
        {
            return new
            {
                error = ex.Message,
                message = "Error reading movie data"
            };
        }
    }

    private async Task<object> CheckUserData()
    {
        try
        {
            var totalUsers = await _db.Users.CountAsync();
            var usersWithPreferences = await _db.Users
                .Where(u => u.Preferences != null && u.Preferences != "")
                .CountAsync();

            return new
            {
                totalUsers,
                usersWithPreferences,
                message = $"Database has {totalUsers} users, {usersWithPreferences} with preferences set"
            };
        }
        catch (Exception ex)
        {
            return new
            {
                error = ex.Message,
                message = "Error reading user data"
            };
        }
    }

    private async Task<object> CheckSwipeData()
    {
        try
        {
            var totalSwipes = await _db.Swipes.CountAsync();
            var usersWhoSwiped = await _db.Swipes.Select(s => s.UserId).Distinct().CountAsync();

            return new
            {
                totalSwipes,
                usersWhoSwiped,
                message = $"Database has {totalSwipes} swipes from {usersWhoSwiped} users"
            };
        }
        catch (Exception ex)
        {
            return new
            {
                error = ex.Message,
                message = "Error reading swipe data"
            };
        }
    }

    private List<string> GeneratePossibleIssues(int moviesReturned, int totalInGenre, int userSwipes)
    {
        var issues = new List<string>();

        if (moviesReturned == 0 && totalInGenre == 0)
        {
            issues.Add("No movies exist in this genre in the database - seeding may have failed");
        }
        else if (moviesReturned == 0 && totalInGenre > 0 && userSwipes >= totalInGenre)
        {
            issues.Add("User has already swiped on all available movies in this genre");
        }
        else if (moviesReturned == 0 && totalInGenre > 0)
        {
            issues.Add("Movies exist but query is returning 0 - possible filter/query issue");
        }
        else if (moviesReturned < 5 && totalInGenre > 20)
        {
            issues.Add("Low number of movies returned despite many being available - check filters");
        }

        if (issues.Count == 0)
        {
            issues.Add("No obvious issues detected - movies should be displaying correctly");
        }

        return issues;
    }
}