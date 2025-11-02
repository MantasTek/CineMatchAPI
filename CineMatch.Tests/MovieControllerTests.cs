using CineMatchAPI.Application.DTOs;
using CineMatchAPI.Application.Services;
using CineMatchAPI.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CineMatch.Tests.Controllers;

/// <summary>
/// Tests for MovieController endpoints.
/// Verifies HTTP response codes and data flow.
/// </summary>
public class MovieControllerTests
{
    private readonly Mock<IMovieService> _movieServiceMock;
    private readonly Mock<ILogger<MovieController>> _loggerMock;
    private readonly MovieController _controller;

    public MovieControllerTests()
    {
        _movieServiceMock = new Mock<IMovieService>();
        _loggerMock = new Mock<ILogger<MovieController>>();
        _controller = new MovieController(_movieServiceMock.Object, _loggerMock.Object);

        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "user1")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }
}