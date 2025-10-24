using CineMatchAPI.Application.Services;
using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CineMatch.Tests.Services;

public class MovieServiceTests
{
    private readonly Mock<IMovieRepository> _movieRepositoryMock;
    private readonly MovieService _movieService;

    public MovieServiceTests()
    {
        _movieRepositoryMock = new Mock<IMovieRepository>();
        _movieService = new MovieService(_movieRepositoryMock.Object);
    }

    [Fact]
    public async Task GetMoviesByPreferences_WithShortLength_ReturnsOnlyShortMovies()
    {
        // Arrange
        var movies = new List<Movie>
        {
            new Movie { Id = "1", Title = "Short Movie", Genre = "Action", Runtime = 80, Rating = 7.5, Year = 2020, ImageUrl = "url", Description = "desc" },
            new Movie { Id = "2", Title = "Medium Movie", Genre = "Action", Runtime = 110, Rating = 8.0, Year = 2021, ImageUrl = "url", Description = "desc" },
            new Movie { Id = "3", Title = "Long Movie", Genre = "Action", Runtime = 150, Rating = 8.5, Year = 2022, ImageUrl = "url", Description = "desc" }
        };
        _movieRepositoryMock.Setup(x => x.GetByGenreAsync("Action", 1, 20)).ReturnsAsync(movies);

        // Act
        var result = await _movieService.GetMoviesByPreferencesAsync("Action", "short", 1, 20);

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Short Movie");
        result.All(m => m.Runtime < 90).Should().BeTrue();
    }

    [Fact]
    public async Task GetMoviesByPreferences_WithMediumLength_ReturnsMediumMovies()
    {
        // Arrange
        var movies = new List<Movie>
        {
            new Movie { Id = "1", Title = "Short", Runtime = 80, Genre = "Comedy", Rating = 7, Year = 2020, ImageUrl = "url", Description = "desc" },
            new Movie { Id = "2", Title = "Medium1", Runtime = 100, Genre = "Comedy", Rating = 7, Year = 2020, ImageUrl = "url", Description = "desc" },
            new Movie { Id = "3", Title = "Medium2", Runtime = 120, Genre = "Comedy", Rating = 7, Year = 2020, ImageUrl = "url", Description = "desc" },
            new Movie { Id = "4", Title = "Long", Runtime = 140, Genre = "Comedy", Rating = 7, Year = 2020, ImageUrl = "url", Description = "desc" }
        };
        _movieRepositoryMock.Setup(x => x.GetByGenreAsync("Comedy", 1, 20)).ReturnsAsync(movies);

        // Act
        var result = await _movieService.GetMoviesByPreferencesAsync("Comedy", "medium", 1, 20);

        // Assert
        result.Should().HaveCount(2);
        result.All(m => m.Runtime >= 90 && m.Runtime <= 130).Should().BeTrue();
    }

    [Fact]
    public async Task GetMoviesByPreferences_WithLongLength_ReturnsOnlyLongMovies()
    {
        // Arrange
        var movies = new List<Movie>
        {
            new Movie { Id = "1", Title = "Short", Runtime = 85, Genre = "Drama", Rating = 7, Year = 2020, ImageUrl = "url", Description = "desc" },
            new Movie { Id = "2", Title = "Long1", Runtime = 140, Genre = "Drama", Rating = 7, Year = 2020, ImageUrl = "url", Description = "desc" },
            new Movie { Id = "3", Title = "Long2", Runtime = 180, Genre = "Drama", Rating = 7, Year = 2020, ImageUrl = "url", Description = "desc" }
        };
        _movieRepositoryMock.Setup(x => x.GetByGenreAsync("Drama", 1, 20)).ReturnsAsync(movies);

        // Act
        var result = await _movieService.GetMoviesByPreferencesAsync("Drama", "long", 1, 20);

        // Assert
        result.Should().HaveCount(2);
        result.All(m => m.Runtime > 130).Should().BeTrue();
    }

    [Fact]
    public async Task GetMoviesByPreferences_WithInvalidLength_ReturnsAllMovies()
    {
        // Arrange
        var movies = new List<Movie>
        {
            new Movie { Id = "1", Title = "M1", Runtime = 80, Genre = "Horror", Rating = 7, Year = 2020, ImageUrl = "url", Description = "desc" },
            new Movie { Id = "2", Title = "M2", Runtime = 120, Genre = "Horror", Rating = 7, Year = 2020, ImageUrl = "url", Description = "desc" }
        };
        _movieRepositoryMock.Setup(x => x.GetByGenreAsync("Horror", 1, 20)).ReturnsAsync(movies);

        // Act
        var result = await _movieService.GetMoviesByPreferencesAsync("Horror", "invalid", 1, 20);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMoviesByPreferences_ReturnsMovieDtos()
    {
        // Arrange
        var movies = new List<Movie>
        {
            new Movie { Id = "1", Title = "Test", Runtime = 100, Genre = "Action", Rating = 8.5, Year = 2023, ImageUrl = "test.jpg", Description = "Test desc" }
        };
        _movieRepositoryMock.Setup(x => x.GetByGenreAsync("Action", 1, 20)).ReturnsAsync(movies);

        // Act
        var result = (await _movieService.GetMoviesByPreferencesAsync("Action", "medium", 1, 20)).ToList();

        // Assert
        result.Should().HaveCount(1);
        var dto = result.First();
        dto.Title.Should().Be("Test");
        dto.Runtime.Should().Be(100);
        dto.Genre.Should().Be("Action");
        dto.Rating.Should().Be(8.5);
    }

    [Fact]
    public async Task GetMoviesByPreferences_WithNoMovies_ReturnsEmpty()
    {
        // Arrange
        _movieRepositoryMock.Setup(x => x.GetByGenreAsync("Sci-Fi", 1, 20)).ReturnsAsync(new List<Movie>());

        // Act
        var result = await _movieService.GetMoviesByPreferencesAsync("Sci-Fi", "short", 1, 20);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMoviesByPreferences_PassesCorrectParametersToRepository()
    {
        // Arrange
        _movieRepositoryMock.Setup(x => x.GetByGenreAsync("Thriller", 2, 10)).ReturnsAsync(new List<Movie>());

        // Act
        await _movieService.GetMoviesByPreferencesAsync("Thriller", "medium", 2, 10);

        // Assert
        _movieRepositoryMock.Verify(x => x.GetByGenreAsync("Thriller", 2, 10), Times.Once);
    }

    [Theory]
    [InlineData("short", 89, true)]
    [InlineData("short", 90, false)]
    [InlineData("medium", 89, false)]
    [InlineData("medium", 90, true)]
    [InlineData("medium", 130, true)]
    [InlineData("medium", 131, false)]
    [InlineData("long", 130, false)]
    [InlineData("long", 131, true)]
    public async Task GetMoviesByPreferences_LengthBoundaries_CorrectFiltering(string length, int runtime, bool shouldInclude)
    {
        // Arrange
        var movies = new List<Movie>
        {
            new Movie { Id = "1", Title = "Test", Runtime = runtime, Genre = "Action", Rating = 7, Year = 2020, ImageUrl = "url", Description = "desc" }
        };
        _movieRepositoryMock.Setup(x => x.GetByGenreAsync("Action", 1, 20)).ReturnsAsync(movies);

        // Act
        var result = await _movieService.GetMoviesByPreferencesAsync("Action", length, 1, 20);

        // Assert
        if (shouldInclude)
            result.Should().HaveCount(1);
        else
            result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMoviesByPreferences_CaseInsensitiveLength_WorksCorrectly()
    {
        // Arrange
        var movies = new List<Movie>
        {
            new Movie { Id = "1", Title = "Short", Runtime = 80, Genre = "Action", Rating = 7, Year = 2020, ImageUrl = "url", Description = "desc" }
        };
        _movieRepositoryMock.Setup(x => x.GetByGenreAsync("Action", 1, 20)).ReturnsAsync(movies);

        // Act
        var result1 = await _movieService.GetMoviesByPreferencesAsync("Action", "SHORT", 1, 20);
        var result2 = await _movieService.GetMoviesByPreferencesAsync("Action", "Short", 1, 20);

        // Assert
        result1.Should().HaveCount(1);
        result2.Should().HaveCount(1);
    }
}