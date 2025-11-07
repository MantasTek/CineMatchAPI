using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using CineMatchAPI.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace CineMatch.Tests.Services;

/// <summary>
/// Comprehensive tests for TMDbService to improve coverage from 13.3%
/// Tests cover API interactions, error handling, retry logic, and data processing
/// </summary>
public class TMDbServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IMovieRepository> _movieRepositoryMock;
    private readonly Mock<ILogger<TMDbService>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly TMDbService _service;
    private const string TestApiKey = "test_api_key_12345";

    public TMDbServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
     _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
     _movieRepositoryMock = new Mock<IMovieRepository>();
        _loggerMock = new Mock<ILogger<TMDbService>>();

        var inMemorySettings = new Dictionary<string, string>
        {
            {"TMDb:ApiKey", TestApiKey}
        };

        _configuration = new ConfigurationBuilder()
       .AddInMemoryCollection(inMemorySettings!)
            .Build();

        _service = new TMDbService(
_httpClient,
          _movieRepositoryMock.Object,
            _configuration,
      _loggerMock.Object
   );
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithMissingApiKey_ThrowsInvalidOperationException()
 {
        // Arrange
  var emptyConfig = new ConfigurationBuilder().Build();

        // Act & Assert
   var act = () => new TMDbService(
 _httpClient,
            _movieRepositoryMock.Object,
        emptyConfig,
            _loggerMock.Object
    );

     act.Should().Throw<InvalidOperationException>()
 .WithMessage("*TMDb API key not configured*");
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
// Act & Assert
        _service.Should().NotBeNull();
    }

    #endregion

    #region FetchAndCacheMoviesByGenreAsync - Valid Genre Tests

    [Theory]
    [InlineData("Action")]
    [InlineData("Comedy")]
    [InlineData("Drama")]
[InlineData("Horror")]
    [InlineData("Romance")]
    [InlineData("Sci-Fi")]
    [InlineData("Thriller")]
    [InlineData("Documentary")]
    [InlineData("Animation")]
  [InlineData("Mystery")]
    public async Task FetchAndCacheMoviesByGenreAsync_WithValidGenre_ReturnsMovies(string genre)
    {
        // Arrange
        var tmdbResponse = CreateTMDbResponse(new[]
        {
  CreateTMDbMovie(123, "Test Movie", "Test Overview", "/poster.jpg", 8.5, "2024-01-01")
        });

        SetupHttpResponse(HttpStatusCode.OK, tmdbResponse);
  SetupMovieDetailsResponse(123, 120);
        _movieRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
      _movieRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Movie>()))
            .ReturnsAsync((Movie m) => m);

    // Act
        var result = await _service.FetchAndCacheMoviesByGenreAsync(genre, 1);

        // Assert
      result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Genre.Should().Be(genre);
    }

 [Fact]
    public async Task FetchAndCacheMoviesByGenreAsync_WithUnknownGenre_ReturnsEmptyList()
    {
        // Arrange
        var unknownGenre = "UnknownGenre";

   // Act
        var result = await _service.FetchAndCacheMoviesByGenreAsync(unknownGenre, 1);

 // Assert
 result.Should().BeEmpty();
        VerifyLogWarning("Unknown genre requested: {Genre}");
    }

    [Fact]
    public async Task FetchAndCacheMoviesByGenreAsync_FetchesMultiplePages()
    {
  // Arrange
      var tmdbResponse = CreateTMDbResponse(new[]
        {
    CreateTMDbMovie(1, "Movie 1", "Overview 1", "/poster1.jpg", 8.0, "2024-01-01")
  });

 SetupHttpResponse(HttpStatusCode.OK, tmdbResponse);
  SetupMovieDetailsResponse(1, 90);
        _movieRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
 _movieRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Movie>()))
 .ReturnsAsync((Movie m) => m);

// Act
 var result = await _service.FetchAndCacheMoviesByGenreAsync("Action", 2);

    // Assert
 result.Should().NotBeNull();
        _httpMessageHandlerMock.Protected()
            .Verify("SendAsync", Times.AtLeast(2), 
        ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/discover/movie")),
  ItExpr.IsAny<CancellationToken>()
       );
    }

    #endregion

    #region FetchAndCacheMoviesByGenreAsync - Error Handling Tests

    [Fact]
  public async Task FetchAndCacheMoviesByGenreAsync_WithHttpError_ContinuesProcessing()
    {
     // Arrange
        SetupHttpResponse(HttpStatusCode.InternalServerError, "");

   // Act
   var result = await _service.FetchAndCacheMoviesByGenreAsync("Action", 1);

        // Assert
      result.Should().BeEmpty();
        VerifyLogWarning("Failed to fetch page {Page} for genre {Genre}. Status: {Status}");
    }

    [Fact]
    public async Task FetchAndCacheMoviesByGenreAsync_WithEmptyResults_ReturnsEmptyList()
  {
        // Arrange
        var emptyResponse = CreateTMDbResponse(Array.Empty<object>());
     SetupHttpResponse(HttpStatusCode.OK, emptyResponse);

      // Act
        var result = await _service.FetchAndCacheMoviesByGenreAsync("Action", 1);

        // Assert
        result.Should().BeEmpty();
        VerifyLogInformation("No results on page {Page} for genre {Genre}");
    }

    [Fact]
  public async Task FetchAndCacheMoviesByGenreAsync_WithNullResults_ReturnsEmptyList()
    {
        // Arrange
        var responseWithNullResults = "{\"results\":null}";
    SetupHttpResponse(HttpStatusCode.OK, responseWithNullResults);

        // Act
        var result = await _service.FetchAndCacheMoviesByGenreAsync("Action", 1);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FetchAndCacheMoviesByGenreAsync_WithHttpException_HandlesGracefully()
    {
  // Arrange
    _httpMessageHandlerMock.Protected()
          .Setup<Task<HttpResponseMessage>>("SendAsync",
          ItExpr.IsAny<HttpRequestMessage>(),
    ItExpr.IsAny<CancellationToken>())
        .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _service.FetchAndCacheMoviesByGenreAsync("Action", 1);

        // Assert
     result.Should().BeEmpty();
     VerifyLogError("Error fetching page {Page} for genre {Genre}");
    }

    #endregion

    #region FetchAndCacheMoviesByGenreAsync - Movie Processing Tests

    [Fact]
    public async Task FetchAndCacheMoviesByGenreAsync_SkipsExistingMovies()
    {
// Arrange
        var tmdbResponse = CreateTMDbResponse(new[]
        {
     CreateTMDbMovie(123, "Existing Movie", "Overview", "/poster.jpg", 7.5, "2023-01-01")
        });

        SetupHttpResponse(HttpStatusCode.OK, tmdbResponse);
     _movieRepositoryMock.Setup(x => x.ExistsAsync("123")).ReturnsAsync(true);

        // Act
        var result = await _service.FetchAndCacheMoviesByGenreAsync("Action", 1);

      // Assert
      result.Should().BeEmpty();
      _movieRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Movie>()), Times.Never);
 VerifyLogDebug("Movie {Id} ({Title}) already exists, skipping");
    }

    [Fact]
    public async Task FetchAndCacheMoviesByGenreAsync_CreatesMovieWithCorrectData()
    {
        // Arrange
     var tmdbMovie = CreateTMDbMovie(456, "New Movie", "Great movie", "/poster.jpg", 9.0, "2024-06-15");
        var tmdbResponse = CreateTMDbResponse(new[] { tmdbMovie });

   SetupHttpResponse(HttpStatusCode.OK, tmdbResponse);
   SetupMovieDetailsResponse(456, 150);
     _movieRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

        Movie? capturedMovie = null;
    _movieRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Movie>()))
  .Callback<Movie>(m => capturedMovie = m)
     .ReturnsAsync((Movie m) => m);

        // Act
        var result = await _service.FetchAndCacheMoviesByGenreAsync("Drama", 1);

        // Assert
        result.Should().HaveCount(1);
      capturedMovie.Should().NotBeNull();
   capturedMovie!.Id.Should().Be("456");
        capturedMovie.Title.Should().Be("New Movie");
   capturedMovie.Description.Should().Be("Great movie");
capturedMovie.Genre.Should().Be("Drama");
    capturedMovie.Rating.Should().Be(9.0);
      capturedMovie.Year.Should().Be(2024);
  capturedMovie.Runtime.Should().Be(150);
    capturedMovie.ImageUrl.Should().Contain("/poster.jpg");
    capturedMovie.CachedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task FetchAndCacheMoviesByGenreAsync_WithNullReleaseDate_UsesDefaultYear()
    {
    // Arrange
   var tmdbMovie = CreateTMDbMovie(789, "Movie Without Date", "Description", "/poster.jpg", 7.0, null);
 var tmdbResponse = CreateTMDbResponse(new[] { tmdbMovie });

        SetupHttpResponse(HttpStatusCode.OK, tmdbResponse);
    SetupMovieDetailsResponse(789, 100);
        _movieRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

        Movie? capturedMovie = null;
    _movieRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Movie>()))
    .Callback<Movie>(m => capturedMovie = m)
  .ReturnsAsync((Movie m) => m);

        // Act
        await _service.FetchAndCacheMoviesByGenreAsync("Comedy", 1);

        // Assert
   capturedMovie.Should().NotBeNull();
 capturedMovie!.Year.Should().Be(2000);
    }

    [Fact]
    public async Task FetchAndCacheMoviesByGenreAsync_WithProcessingError_ContinuesWithOtherMovies()
    {
      // Arrange
        var tmdbResponse = CreateTMDbResponse(new[]
{
 CreateTMDbMovie(1, "Good Movie", "Overview", "/poster.jpg", 8.0, "2024-01-01"),
     CreateTMDbMovie(2, "Problem Movie", "Overview", "/poster.jpg", 7.0, "2024-01-01")
        });

  SetupHttpResponse(HttpStatusCode.OK, tmdbResponse);
   SetupMovieDetailsResponse(1, 120);
        SetupMovieDetailsResponse(2, 90);

  _movieRepositoryMock.Setup(x => x.ExistsAsync("1")).ReturnsAsync(false);
      _movieRepositoryMock.Setup(x => x.ExistsAsync("2")).ReturnsAsync(false);

     // First movie succeeds, second throws exception
        var firstMovie = new Movie { Id = "1", Title = "Good Movie" };
        _movieRepositoryMock.SetupSequence(x => x.CreateAsync(It.IsAny<Movie>()))
 .ReturnsAsync(firstMovie)
      .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.FetchAndCacheMoviesByGenreAsync("Action", 1);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Good Movie");
        VerifyLogError("Error processing movie {Title} ({Id}) from genre {Genre}");
    }

    #endregion

    #region Runtime Fetching Tests

    [Fact]
    public async Task FetchAndCacheMoviesByGenreAsync_FetchesRuntimeSuccessfully()
    {
        // Arrange
        var tmdbResponse = CreateTMDbResponse(new[]
        {
            CreateTMDbMovie(100, "Movie", "Overview", "/poster.jpg", 8.0, "2024-01-01")
    });

        SetupHttpResponse(HttpStatusCode.OK, tmdbResponse);
   SetupMovieDetailsResponse(100, 135);
    _movieRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

     Movie? capturedMovie = null;
        _movieRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Movie>()))
       .Callback<Movie>(m => capturedMovie = m)
 .ReturnsAsync((Movie m) => m);

        // Act
   await _service.FetchAndCacheMoviesByGenreAsync("Action", 1);

        // Assert
        capturedMovie.Should().NotBeNull();
        capturedMovie!.Runtime.Should().Be(135);
    }

    [Fact]
    public async Task FetchAndCacheMoviesByGenreAsync_WithRuntimeFetchError_UsesDefaultRuntime()
    {
   // Arrange
    var tmdbResponse = CreateTMDbResponse(new[]
        {
            CreateTMDbMovie(200, "Movie", "Overview", "/poster.jpg", 8.0, "2024-01-01")
    });

  SetupHttpResponse(HttpStatusCode.OK, tmdbResponse);
      SetupMovieDetailsError(200, HttpStatusCode.InternalServerError);
      _movieRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

   Movie? capturedMovie = null;
        _movieRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Movie>()))
      .Callback<Movie>(m => capturedMovie = m)
    .ReturnsAsync((Movie m) => m);

      // Act
        await _service.FetchAndCacheMoviesByGenreAsync("Drama", 1);

      // Assert
   capturedMovie.Should().NotBeNull();
        capturedMovie!.Runtime.Should().Be(120); // Default runtime
}

    [Fact]
    public async Task FetchAndCacheMoviesByGenreAsync_WithSuspiciousRuntime_UsesDefault()
    {
        // Arrange
      var tmdbResponse = CreateTMDbResponse(new[]
     {
            CreateTMDbMovie(300, "Movie", "Overview", "/poster.jpg", 8.0, "2024-01-01")
        });

        SetupHttpResponse(HttpStatusCode.OK, tmdbResponse);
        SetupMovieDetailsResponse(300, 600); // Suspicious: over 500 minutes
 _movieRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

        Movie? capturedMovie = null;
        _movieRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Movie>()))
       .Callback<Movie>(m => capturedMovie = m)
 .ReturnsAsync((Movie m) => m);

        // Act
    await _service.FetchAndCacheMoviesByGenreAsync("Horror", 1);

    // Assert
        capturedMovie.Should().NotBeNull();
        capturedMovie!.Runtime.Should().Be(120); // Default runtime
    }

    [Fact]
    public async Task FetchAndCacheMoviesByGenreAsync_WithZeroRuntime_UsesDefault()
    {
 // Arrange
  var tmdbResponse = CreateTMDbResponse(new[]
 {
    CreateTMDbMovie(400, "Movie", "Overview", "/poster.jpg", 8.0, "2024-01-01")
   });

      SetupHttpResponse(HttpStatusCode.OK, tmdbResponse);
        SetupMovieDetailsResponse(400, 0);
        _movieRepositoryMock.Setup(x => x.ExistsAsync("400")).ReturnsAsync(false);

    Movie? capturedMovie = null;
_movieRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Movie>()))
        .Callback<Movie>(m => capturedMovie = m)
      .Returns<Movie>(m => Task.FromResult(m));

        // Act
  await _service.FetchAndCacheMoviesByGenreAsync("Romance", 1);

        // Assert
        capturedMovie.Should().NotBeNull();
        capturedMovie!.Runtime.Should().Be(120);
    }

    [Fact]
public async Task FetchAndCacheMoviesByGenreAsync_WithRateLimitError_RetriesAndSucceeds()
    {
        // Arrange
        var tmdbResponse = CreateTMDbResponse(new[]
        {
     CreateTMDbMovie(500, "Movie", "Overview", "/poster.jpg", 8.0, "2024-01-01")
     });

SetupHttpResponse(HttpStatusCode.OK, tmdbResponse);

        // First attempt: rate limited, second attempt: success
      var runtimeResponses = new Queue<HttpResponseMessage>(new[]
        {
new HttpResponseMessage(HttpStatusCode.TooManyRequests),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
 Content = new StringContent("{\"runtime\":95}")
  }
        });

        _httpMessageHandlerMock.Protected()
         .Setup<Task<HttpResponseMessage>>("SendAsync",
          ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains($"/movie/500?")),
  ItExpr.IsAny<CancellationToken>())
       .ReturnsAsync(() => runtimeResponses.Dequeue());

        _movieRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

        Movie? capturedMovie = null;
        _movieRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Movie>()))
            .Callback<Movie>(m => capturedMovie = m)
            .Returns<Movie>(m => Task.FromResult(m));

   // Act
        await _service.FetchAndCacheMoviesByGenreAsync("Thriller", 1);

   // Assert
        capturedMovie.Should().NotBeNull();
        capturedMovie!.Runtime.Should().Be(95);
    }

    #endregion

    #region SeedDatabaseAsync Tests

    [Fact]
    public async Task SeedDatabaseAsync_ProcessesAllGenres()
    {
   // Arrange
        var tmdbResponse = CreateTMDbResponse(new[]
        {
  CreateTMDbMovie(1, "Movie", "Overview", "/poster.jpg", 8.0, "2024-01-01")
        });

    SetupHttpResponse(HttpStatusCode.OK, tmdbResponse);
    SetupMovieDetailsResponse(1, 120);
 _movieRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
    _movieRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Movie>()))
      .ReturnsAsync((Movie m) => m);

        // Act
        await _service.SeedDatabaseAsync();

     // Assert
     VerifyLogInformation("Starting complete database seed from TMDb");
  VerifyLogInformation("This will fetch 5 pages per genre for {Count} genres");
        VerifyLogInformation("Database seeding completed in {Duration}. Check database for total movie count.");
 }

    [Fact]
  public async Task SeedDatabaseAsync_LogsProgress()
    {
 // Arrange
        var tmdbResponse = CreateTMDbResponse(Array.Empty<object>());
        SetupHttpResponse(HttpStatusCode.OK, tmdbResponse);

      // Act
        await _service.SeedDatabaseAsync();

  // Assert
        VerifyLogInformation("Seeding genre {Current}/{Total}: {Genre}", Times.AtLeast(10));
    }

    #endregion

    #region TMDbResponse and TMDbMovie Tests

    [Fact]
    public void TMDbResponse_CanBeDeserialized()
    {
        // Arrange
        var json = "{\"results\":[{\"id\":123,\"title\":\"Test\",\"overview\":\"Desc\",\"poster_path\":\"/poster.jpg\",\"vote_average\":8.5,\"release_date\":\"2024-01-01\"}]}";

      // Act
        var response = JsonSerializer.Deserialize<TMDbResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new NullableDateTimeConverter() }
});

        // Assert
        response.Should().NotBeNull();
        response!.Results.Should().HaveCount(1);
        response.Results[0].Id.Should().Be(123);
        response.Results[0].Title.Should().Be("Test");
    }

    [Fact]
    public void TMDbMovieDetails_CanBeDeserialized()
    {
     // Arrange
        var json = "{\"runtime\":142}";

        // Act
        var details = JsonSerializer.Deserialize<TMDbMovieDetails>(json);

 // Assert
        details.Should().NotBeNull();
        details!.Runtime.Should().Be(142);
    }

    #endregion

    #region NullableDateTimeConverter Tests

    [Theory]
    [InlineData("2024-01-15", 2024, 1, 15)]
    [InlineData("2023-12-31", 2023, 12, 31)]
    [InlineData("2022-06-01", 2022, 6, 1)]
    public void NullableDateTimeConverter_WithValidDate_ParsesCorrectly(string dateString, int year, int month, int day)
    {
// Arrange
        var json = $"{{\"release_date\":\"{dateString}\"}}";
     var options = new JsonSerializerOptions
      {
       PropertyNameCaseInsensitive = true,
Converters = { new NullableDateTimeConverter() }
        };

        // Act
        var movie = JsonSerializer.Deserialize<TMDbMovie>(json, options);

        // Assert
        movie.Should().NotBeNull();
        movie!.ReleaseDate.Should().NotBeNull();
        movie.ReleaseDate!.Value.Year.Should().Be(year);
        movie.ReleaseDate.Value.Month.Should().Be(month);
        movie.ReleaseDate.Value.Day.Should().Be(day);
    }

    [Theory]
    [InlineData("")]
[InlineData("   ")]
    [InlineData("invalid-date")]
    public void NullableDateTimeConverter_WithInvalidDate_ReturnsNull(string dateString)
    {
        // Arrange
        var json = $"{{\"release_date\":\"{dateString}\"}}";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
       Converters = { new NullableDateTimeConverter() }
        };

        // Act
        var movie = JsonSerializer.Deserialize<TMDbMovie>(json, options);

        // Assert
    movie.Should().NotBeNull();
      movie!.ReleaseDate.Should().BeNull();
    }

    [Fact]
    public void NullableDateTimeConverter_WithNullValue_ReturnsNull()
    {
        // Arrange
        var json = "{\"release_date\":null}";
        var options = new JsonSerializerOptions
    {
            PropertyNameCaseInsensitive = true,
 Converters = { new NullableDateTimeConverter() }
  };

 // Act
      var movie = JsonSerializer.Deserialize<TMDbMovie>(json, options);

        // Assert
      movie.Should().NotBeNull();
    movie!.ReleaseDate.Should().BeNull();
    }

    [Fact]
  public void NullableDateTimeConverter_Write_SerializesCorrectly()
    {
        // Arrange
        var movie = new TMDbMovie
        {
         ReleaseDate = new DateTime(2024, 6, 15)
        };
 var options = new JsonSerializerOptions
        {
     Converters = { new NullableDateTimeConverter() }
        };

    // Act
        var json = JsonSerializer.Serialize(movie, options);

    // Assert
        json.Should().Contain("2024-06-15");
    }

    [Fact]
  public void NullableDateTimeConverter_Write_WithNull_SerializesAsNull()
    {
        // Arrange
 var movie = new TMDbMovie
        {
 ReleaseDate = null
        };
    var options = new JsonSerializerOptions
        {
        Converters = { new NullableDateTimeConverter() }
        };

   // Act
        var json = JsonSerializer.Serialize(movie, options);

    // Assert
        json.Should().Contain("null");
    }

    #endregion

    #region Helper Methods

    private void SetupHttpResponse(HttpStatusCode statusCode, object responseBody)
    {
        var responseContent = responseBody is string str 
            ? str 
     : JsonSerializer.Serialize(responseBody);

     _httpMessageHandlerMock.Protected()
 .Setup<Task<HttpResponseMessage>>("SendAsync",
       ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/discover/movie")),
     ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage
    {
           StatusCode = statusCode,
    Content = new StringContent(responseContent)
  });
    }

    private void SetupMovieDetailsResponse(int movieId, int runtime)
    {
    var detailsJson = $"{{\"runtime\":{runtime}}}";

   _httpMessageHandlerMock.Protected()
      .Setup<Task<HttpResponseMessage>>("SendAsync",
   ItExpr.Is<HttpRequestMessage>(req => 
 req.RequestUri!.ToString().Contains($"/movie/{movieId}?") &&
  !req.RequestUri.ToString().Contains("/discover/")),
            ItExpr.IsAny<CancellationToken>())
.ReturnsAsync(new HttpResponseMessage
  {
      StatusCode = HttpStatusCode.OK,
         Content = new StringContent(detailsJson)
  });
    }

    private void SetupMovieDetailsError(int movieId, HttpStatusCode statusCode)
    {
      _httpMessageHandlerMock.Protected()
   .Setup<Task<HttpResponseMessage>>("SendAsync",
    ItExpr.Is<HttpRequestMessage>(req => 
    req.RequestUri!.ToString().Contains($"/movie/{movieId}?") &&
  !req.RequestUri.ToString().Contains("/discover/")),
   ItExpr.IsAny<CancellationToken>())
     .ReturnsAsync(new HttpResponseMessage
    {
    StatusCode = statusCode,
        Content = new StringContent("")
      });
  }

    private static object CreateTMDbResponse(object[] movies)
    {
        return new { results = movies };
    }

    private static object CreateTMDbMovie(int id, string title, string overview, string posterPath, double voteAverage, string? releaseDate)
    {
        return new
        {
 id = id,
    title = title,
          overview = overview,
 poster_path = posterPath,
            vote_average = voteAverage,
release_date = releaseDate
  };
    }

    private void VerifyLogWarning(string messageTemplate, Times? times = null)
    {
        _loggerMock.Verify(
    x => x.Log(
        It.Is<LogLevel>(l => l == LogLevel.Warning),
    It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
   It.IsAny<Exception>(),
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times ?? Times.AtLeastOnce());
    }

    private void VerifyLogInformation(string messageTemplate, Times? times = null)
    {
   _loggerMock.Verify(
      x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Information),
    It.IsAny<EventId>(),
 It.IsAny<It.IsAnyType>(),
        It.IsAny<Exception>(),
     It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
      times ?? Times.AtLeastOnce());
    }

    private void VerifyLogDebug(string messageTemplate)
    {
        _loggerMock.Verify(
            x => x.Log(
      It.Is<LogLevel>(l => l == LogLevel.Debug),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
       It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
      Times.AtLeastOnce());
    }

    private void VerifyLogError(string messageTemplate)
    {
        _loggerMock.Verify(
         x => x.Log(
        It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
         It.IsAny<Exception>(),
     It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
     Times.AtLeastOnce());
 }

    #endregion
}
