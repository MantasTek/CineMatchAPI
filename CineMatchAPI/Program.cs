using CineMatchAPI.Domain.Interfaces;
using CineMatchAPI.Infrastructure.Data;
using CineMatchAPI.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models; // Add this using directive at the top of the file
using Swashbuckle.AspNetCore.SwaggerGen; // Add this using directive at the top of the file
using Swashbuckle.AspNetCore.SwaggerUI; // Add this using directive at the top of the file

var builder = WebApplication.CreateBuilder(args);

// Add services to the dependency injection container
// This section registers all the services our application needs

// Register our database context with SQLite
// The connection string is read from appsettings.json
// This follows the configuration pattern where environment-specific settings
// live in configuration files rather than code
builder.Services.AddDbContext<CineMatchDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register all our repository implementations
// When a class asks for IUserRepository, it will receive UserRepository
// The Scoped lifetime means one instance per HTTP request
// This is important for database contexts which should not be shared across requests
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<ISwipeRepository, SwipeRepository>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();

// Add controller support
// This enables our API controllers to handle HTTP requests
builder.Services.AddControllers();

// Add API documentation with Swagger/OpenAPI
// Swagger generates interactive API documentation that we can use for testing
// This is incredibly useful during development
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS (Cross-Origin Resource Sharing)
// This allows our React frontend (running on a different port) to call our API
// In production, you would restrict this to specific origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Vite's default port
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Required for authentication cookies/headers
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// This section defines how incoming requests are processed

// Enable Swagger only in development
// We do not want to expose our API documentation in production
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use HTTPS redirection to ensure secure communication
app.UseHttpsRedirection();

// Enable CORS with the policy we defined earlier
app.UseCors("AllowReactApp");

// Enable authentication and authorization middleware
// These will be used once we add JWT authentication
app.UseAuthentication();
app.UseAuthorization();

// Map controller routes
// This tells ASP.NET Core to route requests to our controller methods
app.MapControllers();

app.Run();