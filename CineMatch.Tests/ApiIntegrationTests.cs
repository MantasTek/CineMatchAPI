using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CineMatch.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory to configure in-memory database for testing
/// </summary>
public class CineMatchWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            services.RemoveAll(typeof(DbContextOptions<CineMatchDbContext>));

            // Add DbContext using in-memory database for testing
            services.AddDbContext<CineMatchDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid());
            });

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<CineMatchDbContext>();

            // Ensure the database is created
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

#region DTOs for Tests

public record AuthResponse(string Token, UserResponse User);
public record UserResponse(string Id, string Name, string Email);

#endregion