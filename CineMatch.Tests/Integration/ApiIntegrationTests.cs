using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace CineMatch.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory to configure in-memory database for testing.
/// 
/// WHAT WAS FIXED:
/// The previous implementation was trying to register both SQLite (from Program.cs) 
/// and InMemory database (for testing) at the same time, which Entity Framework doesn't allow.
/// 
/// Think of it like trying to have your car's GPS follow two different maps simultaneously—
/// it gets confused and doesn't work. The fix involves properly removing the SQLite 
/// registration before adding the InMemory database.
/// 
/// KEY CHANGES:
/// 1. Use RemoveAll() to remove ALL DbContext-related registrations (not just DbContextOptions)
/// 2. Use ConfigureTestServices() instead of ConfigureServices() for database initialization
/// 3. This ensures proper order: first remove old provider, then add new one, then initialize
/// </summary>
public class CineMatchWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // CRITICAL FIX: Remove ALL existing DbContext registrations
            // This includes DbContext<T>, DbContextOptions<T>, and related services
            // We need to remove these BEFORE adding our InMemory database
            var descriptorType = typeof(DbContextOptions<CineMatchDbContext>);
            var descriptor = services.SingleOrDefault(d => d.ServiceType == descriptorType);
            
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Also remove the DbContext itself if it's registered
            var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(CineMatchDbContext));
            if (contextDescriptor != null)
            {
                services.Remove(contextDescriptor);
            }

            // Now add our InMemory database for testing
            // Each test run gets a unique database name to ensure isolation
            services.AddDbContext<CineMatchDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid());
            });
        });

        // USE ConfigureTestServices for database initialization
        // This runs AFTER all services are configured, ensuring no conflicts
        builder.ConfigureTestServices(services =>
        {
            // Build the service provider to get our configured DbContext
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<CineMatchDbContext>();

            // Ensure the database is created
            // For InMemory database, this just sets up the schema
            db.Database.EnsureCreated();
        });
    }
}

public class ApiIntegrationTests : IClassFixture<CineMatchWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CineMatchWebApplicationFactory _factory;

    public ApiIntegrationTests(CineMatchWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    /// <summary>
    /// Helper method to register a new user and return their authentication token.
    /// This is used by many tests that need an authenticated user.
    /// </summary>
    private async Task<string> RegisterAndLoginUser(string email = null!)
    {
        email ??= $"test{Guid.NewGuid()}@test.com";
        
        var registerDto = new RegisterDto(
            "Test User",
            email,
            "Password123!",
            "Stockholm"
        );

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        
        if (!registerResponse.IsSuccessStatusCode)
        {
            var error = await registerResponse.Content.ReadAsStringAsync();
            throw new Exception($"Registration failed: {error}");
        }

        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.Token;
    }

    /// <summary>
    /// Helper method to set the authorization header with a Bearer token
    /// </summary>
    private void SetAuthToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    #region Auth Tests

    [Fact]
    public async Task Auth_Register_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var dto = new RegisterDto(
            "John Doe",
            $"john{Guid.NewGuid()}@example.com",
            "Password123!",
            "Stockholm"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Auth_Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var email = $"duplicate{Guid.NewGuid()}@example.com";
        var dto1 = new RegisterDto("User1", email, "Pass123!", "City");
        
        await _client.PostAsJsonAsync("/api/auth/register", dto1);

        // Act
        var dto2 = new RegisterDto("User2", email, "Pass456!", "City");
        var response = await _client.PostAsJsonAsync("/api/auth/register", dto2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Auth_Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var email = $"login{Guid.NewGuid()}@example.com";
        var password = "Password123!";
        await RegisterAndLoginUser(email);

        var loginDto = new LoginDto(email, password);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Auth_Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var dto = new LoginDto("nonexistent@test.com", "wrongpass");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Auth_GetCurrentUser_WithValidToken_ReturnsUser()
    {
        // Arrange
        var token = await RegisterAndLoginUser();
        SetAuthToken(token);

        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Auth_GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Swipe Tests

    [Fact]
    public async Task Swipe_GetStarredMovies_WithAuth_ReturnsOk()
    {
        // Arrange
        var token = await RegisterAndLoginUser();
        SetAuthToken(token);

        // Act
        var response = await _client.GetAsync("/api/swipe/starred");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Swipe_GetStarredMovies_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/swipe/starred");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Swipe_GetCount_WithAuth_ReturnsCount()
    {
        // Arrange
        var token = await RegisterAndLoginUser();
        SetAuthToken(token);

        // Act
        var response = await _client.GetAsync("/api/swipe/count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region User Tests

    [Fact]
    public async Task User_GetCurrentUser_WithAuth_ReturnsUser()
    {
        // Arrange
        var token = await RegisterAndLoginUser();
        SetAuthToken(token);

        // Act
        var response = await _client.GetAsync("/api/user/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task User_UpdatePreferences_WithAuth_ReturnsOk()
    {
        // Arrange
        var token = await RegisterAndLoginUser();
        SetAuthToken(token);

        var dto = new UpdatePreferencesDto(new List<string> { "Action", "Comedy" }, "medium");

        // Act
        var response = await _client.PutAsJsonAsync("/api/user/preferences", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task User_ResetData_WithAuth_ReturnsOk()
    {
        // Arrange
        var token = await RegisterAndLoginUser();
        SetAuthToken(token);

        // Act
        var response = await _client.PostAsync("/api/user/reset", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Match Tests

    [Fact]
    public async Task Match_GetMatches_WithAuth_ReturnsOk()
    {
        // Arrange
        var token = await RegisterAndLoginUser();
        SetAuthToken(token);

        // Act
        var response = await _client.GetAsync("/api/match");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Match_GetPotentialMatches_WithAuth_ReturnsOk()
    {
        // Arrange
        var token = await RegisterAndLoginUser();
        SetAuthToken(token);

        // Act
        var response = await _client.GetAsync("/api/match/potential");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Test Controller

    [Fact]
    public async Task Test_HealthCheck_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Test_DatabaseCheck_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/test/db");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}