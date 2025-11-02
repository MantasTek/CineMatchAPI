using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using CineMatchAPI.Infrastructure.Data;
using CineMatchAPI.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CineMatch.Tests.Repositories;

public class MovieRepositoryTests : IDisposable
{
    private readonly CineMatchDbContext _context;
    private readonly IMovieRepository _repository;

    public MovieRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CineMatchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new CineMatchDbContext(options);
        _repository = new MovieRepository(_context);
    }

    #region Create Tests

    [Fact]
    public async Task CreateAsync_WithValidMovie_AddsToDatabase()
    {
        var movie = new Movie
        {
            Id = "m1",
            Title = "Test Movie",
            Genre = "Action",
            Rating = 8.5,
            Year = 2023,
            Runtime = 120,
            ImageUrl = "https://example.com/image.jpg",
            Description = "A test movie"
        };

        var result = await _repository.CreateAsync(movie);

        result.Should().NotBeNull();
        result.Id.Should().Be("m1");
        var saved = await _context.Movies.FindAsync("m1");
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_PreservesAllProperties()
    {
        var movie = new Movie
        {
            Id = "m2",
            Title = "Full Props",
            Genre = "Drama",
            Rating = 9.0,
            Year = 2020,
            Runtime = 150,
            ImageUrl = "url",
            Description = "desc"
        };

        await _repository.CreateAsync(movie);

        var saved = await _context.Movies.FindAsync("m2");
        saved!.Title.Should().Be("Full Props");
        saved.Genre.Should().Be("Drama");
        saved.Rating.Should().Be(9.0);
        saved.Year.Should().Be(2020);
        saved.Runtime.Should().Be(150);
    }

    [Fact]
    public async Task CreateAsync_WithMultipleMovies_AllAdded()
    {
        await _repository.CreateAsync(new Movie { Id = "1", Title = "M1", Genre = "Action", Rating = 7, Year = 2020, Runtime = 90, ImageUrl = "u", Description = "d" });
        await _repository.CreateAsync(new Movie { Id = "2", Title = "M2", Genre = "Comedy", Rating = 8, Year = 2021, Runtime = 100, ImageUrl = "u", Description = "d" });
        await _repository.CreateAsync(new Movie { Id = "3", Title = "M3", Genre = "Drama", Rating = 9, Year = 2022, Runtime = 110, ImageUrl = "u", Description = "d" });

        var all = await _context.Movies.ToListAsync();
        all.Should().HaveCount(3);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsMovie()
    {
        var movie = new Movie { Id = "exists", Title = "Movie", Genre = "Action", Rating = 7, Year = 2020, Runtime = 90, ImageUrl = "u", Description = "d" };
        await _repository.CreateAsync(movie);

        var result = await _repository.GetByIdAsync("exists");

        result.Should().NotBeNull();
        result!.Id.Should().Be("exists");
        result.Title.Should().Be("Movie");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_IsCaseSensitive()
    {
        await _repository.CreateAsync(new Movie { Id = "movie1", Title = "M", Genre = "A", Rating = 7, Year = 2020, Runtime = 90, ImageUrl = "u", Description = "d" });

        var result = await _repository.GetByIdAsync("MOVIE1");

        result.Should().BeNull();
    }

    #endregion

    #region GetByGenre Tests

    [Fact]
    public async Task GetByGenreAsync_WithMatchingGenre_ReturnsMovies()
    {
        await _repository.CreateAsync(new Movie { Id = "1", Title = "Action1", Genre = "Action", Rating = 7, Year = 2020, Runtime = 90, ImageUrl = "u", Description = "d" });
        await _repository.CreateAsync(new Movie { Id = "2", Title = "Action2", Genre = "Action", Rating = 8, Year = 2021, Runtime = 100, ImageUrl = "u", Description = "d" });
        await _repository.CreateAsync(new Movie { Id = "3", Title = "Drama1", Genre = "Drama", Rating = 9, Year = 2022, Runtime = 110, ImageUrl = "u", Description = "d" });

        var result = await _repository.GetByGenreAsync("Action", 1, 20);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(m => m.Genre == "Action");
    }

    [Fact]
    public async Task GetByGenreAsync_WithNoMatches_ReturnsEmpty()
    {
        await SeedMovies(5);

        var result = await _repository.GetByGenreAsync("Western", 1, 20);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByGenreAsync_RespectsPagination()
    {
        for (int i = 1; i <= 25; i++)
        {
            await _repository.CreateAsync(new Movie { Id = $"m{i}", Title = $"Movie{i}", Genre = "Action", Rating = 7, Year = 2020, Runtime = 90, ImageUrl = "u", Description = "d" });
        }

        var page1 = await _repository.GetByGenreAsync("Action", 1, 10);
        var page2 = await _repository.GetByGenreAsync("Action", 2, 10);

        page1.Should().HaveCount(10);
        page2.Should().HaveCount(10);
        page1.Select(m => m.Id).Should().NotIntersectWith(page2.Select(m => m.Id));
    }

    [Fact]
    public async Task GetByGenreAsync_IsCaseInsensitive()
    {
        await _repository.CreateAsync(new Movie { Id = "1", Title = "M", Genre = "Action", Rating = 7, Year = 2020, Runtime = 90, ImageUrl = "u", Description = "d" });

        var result = await _repository.GetByGenreAsync("action", 1, 20);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByGenreAsync_OrdersByRatingDescending()
    {
        await _repository.CreateAsync(new Movie { Id = "1", Title = "Low", Genre = "Action", Rating = 6.0, Year = 2020, Runtime = 90, ImageUrl = "u", Description = "d" });
        await _repository.CreateAsync(new Movie { Id = "2", Title = "High", Genre = "Action", Rating = 9.0, Year = 2021, Runtime = 100, ImageUrl = "u", Description = "d" });
        await _repository.CreateAsync(new Movie { Id = "3", Title = "Mid", Genre = "Action", Rating = 7.5, Year = 2022, Runtime = 110, ImageUrl = "u", Description = "d" });

        var result = (await _repository.GetByGenreAsync("Action", 1, 20)).ToList();

        result[0].Rating.Should().Be(9.0);
        result[1].Rating.Should().Be(7.5);
        result[2].Rating.Should().Be(6.0);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateAsync_UpdatesMovieProperties()
    {
        var movie = new Movie { Id = "m1", Title = "Original", Genre = "Action", Rating = 7, Year = 2020, Runtime = 90, ImageUrl = "u", Description = "d" };
        await _repository.CreateAsync(movie);

        movie.Title = "Updated";
        movie.Rating = 8.5;
        await _repository.UpdateAsync(movie);

        var updated = await _repository.GetByIdAsync("m1");
        updated!.Title.Should().Be("Updated");
        updated.Rating.Should().Be(8.5);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotAffectOtherMovies()
    {
        await _repository.CreateAsync(new Movie { Id = "1", Title = "Movie1", Genre = "Action", Rating = 7, Year = 2020, Runtime = 90, ImageUrl = "u", Description = "d" });
        var movie2 = new Movie { Id = "2", Title = "Movie2", Genre = "Comedy", Rating = 8, Year = 2021, Runtime = 100, ImageUrl = "u", Description = "d" };
        await _repository.CreateAsync(movie2);

        movie2.Title = "Updated2";
        await _repository.UpdateAsync(movie2);

        var movie1 = await _repository.GetByIdAsync("1");
        movie1!.Title.Should().Be("Movie1");
    }

    #endregion

    #region Exists Tests

    [Fact]
    public async Task ExistsAsync_WithExistingId_ReturnsTrue()
    {
        await _repository.CreateAsync(new Movie { Id = "exists", Title = "M", Genre = "Action", Rating = 7, Year = 2020, Runtime = 90, ImageUrl = "u", Description = "d" });

        var result = await _repository.ExistsAsync("exists");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentId_ReturnsFalse()
    {
        var result = await _repository.ExistsAsync("nonexistent");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_IsCaseSensitive()
    {
        await _repository.CreateAsync(new Movie { Id = "movie", Title = "M", Genre = "Action", Rating = 7, Year = 2020, Runtime = 90, ImageUrl = "u", Description = "d" });

        var result = await _repository.ExistsAsync("MOVIE");

        result.Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetByGenreAsync_WithZeroPageSize_ReturnsEmpty()
    {
        await SeedMovies(5);

        var result = await _repository.GetByGenreAsync("Action", 1, 0);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByGenreAsync_WithNegativePage_ReturnsEmpty()
    {
        await SeedMovies(5);

        var result = await _repository.GetByGenreAsync("Action", -1, 10);

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(90)]
    [InlineData(120)]
    [InlineData(200)]
    public async Task CreateAsync_WithVariousRuntimes_WorksCorrectly(int runtime)
    {
        var movie = new Movie { Id = $"m{runtime}", Title = "M", Genre = "Action", Rating = 7, Year = 2020, Runtime = runtime, ImageUrl = "u", Description = "d" };

        await _repository.CreateAsync(movie);

        var saved = await _repository.GetByIdAsync($"m{runtime}");
        saved!.Runtime.Should().Be(runtime);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(5.0)]
    [InlineData(7.5)]
    [InlineData(10.0)]
    public async Task CreateAsync_WithVariousRatings_WorksCorrectly(double rating)
    {
        var movie = new Movie { Id = $"m{rating}", Title = "M", Genre = "Action", Rating = rating, Year = 2020, Runtime = 90, ImageUrl = "u", Description = "d" };

        await _repository.CreateAsync(movie);

        var saved = await _repository.GetByIdAsync($"m{rating}");
        saved!.Rating.Should().Be(rating);
    }

    [Fact]
    public async Task GetByGenreAsync_WithEmptyGenre_ReturnsEmpty()
    {
        await SeedMovies(5);

        var result = await _repository.GetByGenreAsync("", 1, 20);

        result.Should().BeEmpty();
    }

    #endregion

    private async Task SeedMovies(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            await _repository.CreateAsync(new Movie
            {
                Id = $"movie{i}",
                Title = $"Movie {i}",
                Genre = "Action",
                Rating = 7.0 + (i * 0.1),
                Year = 2020 + i,
                Runtime = 90 + (i * 10),
                ImageUrl = $"url{i}",
                Description = $"Description {i}"
            });
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}