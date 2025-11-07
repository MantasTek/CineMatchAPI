using CineMatchAPI.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace CineMatch.Tests.Services;

/// <summary>
/// Extended tests for JwtTokenService
/// Target: Improve coverage for token validation and edge cases
/// </summary>
public class JwtTokenServiceExtendedTests
{
    private readonly IJwtTokenService _tokenService;
    private readonly IConfiguration _configuration;

    public JwtTokenServiceExtendedTests()
    {
        var configDict = new Dictionary<string, string?>
      {
     { "Jwt:Key", "ThisIsAVerySecureKeyThatIsAtLeast32CharactersLong123456" },
   { "Jwt:Issuer", "TestIssuer" },
  { "Jwt:Audience", "TestAudience" }
    };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
      .Build();

        _tokenService = new JwtTokenService(_configuration);
    }

    #region GenerateToken Tests

    [Fact]
    public void GenerateToken_WithValidInputs_GeneratesValidToken()
    {
        // Arrange
        var userId = "test-user-123";
        var email = "test@example.com";

        // Act
  var token = _tokenService.GenerateToken(userId, email);

   // Assert
        token.Should().NotBeNullOrEmpty();
   var handler = new JwtSecurityTokenHandler();
     handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateToken_ContainsUserIdClaim()
 {
        // Arrange
        var userId = "user-456";
        var email = "user@test.com";

        // Act
    var token = _tokenService.GenerateToken(userId, email);

        // Assert
     var handler = new JwtSecurityTokenHandler();
      var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId);
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId);
    }

  [Fact]
    public void GenerateToken_ContainsEmailClaim()
    {
        // Arrange
        var userId = "user-789";
        var email = "email@domain.com";

        // Act
var token = _tokenService.GenerateToken(userId, email);

        // Assert
 var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
     
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
    }

    [Fact]
 public void GenerateToken_ContainsJtiClaim()
    {
  // Arrange
        var userId = "user-101";
  var email = "jti@test.com";

        // Act
        var token = _tokenService.GenerateToken(userId, email);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
  
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        var jtiClaim = jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti);
        Guid.TryParse(jtiClaim.Value, out _).Should().BeTrue("JTI should be a valid GUID");
    }

    [Fact]
    public void GenerateToken_ContainsIatClaim()
    {
        // Arrange
     var userId = "user-202";
     var email = "iat@test.com";

     // Act
      var token = _tokenService.GenerateToken(userId, email);

        // Assert
        var handler = new JwtSecurityTokenHandler();
      var jwtToken = handler.ReadJwtToken(token);
  
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
    var iatClaim = jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Iat);
        long.TryParse(iatClaim.Value, out _).Should().BeTrue("IAT should be a valid Unix timestamp");
    }

    [Fact]
    public void GenerateToken_HasCorrectIssuer()
    {
        // Arrange
        var userId = "user-303";
        var email = "issuer@test.com";

        // Act
        var token = _tokenService.GenerateToken(userId, email);

 // Assert
   var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.Issuer.Should().Be("TestIssuer");
    }

    [Fact]
    public void GenerateToken_HasCorrectAudience()
    {
        // Arrange
     var userId = "user-404";
        var email = "audience@test.com";

        // Act
        var token = _tokenService.GenerateToken(userId, email);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
   jwtToken.Audiences.Should().Contain("TestAudience");
 }

    [Fact]
    public void GenerateToken_HasValidExpirationTime()
    {
     // Arrange
        var userId = "user-505";
        var email = "expiry@test.com";
        var beforeGeneration = DateTime.UtcNow;

      // Act
        var token = _tokenService.GenerateToken(userId, email);
        var afterGeneration = DateTime.UtcNow.AddHours(24);

      // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.ValidTo.Should().BeAfter(beforeGeneration.AddHours(23).AddMinutes(59));
        jwtToken.ValidTo.Should().BeBefore(afterGeneration.AddMinutes(1));
    }

    [Fact]
    public void GenerateToken_WithSpecialCharactersInEmail_GeneratesValidToken()
    {
    // Arrange
        var userId = "user-606";
        var email = "user+test@sub-domain.example.com";

        // Act
        var token = _tokenService.GenerateToken(userId, email);

        // Assert
      token.Should().NotBeNullOrEmpty();
 var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
        
        var jwtToken = handler.ReadJwtToken(token);
   jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
    }

    [Fact]
    public void GenerateToken_TwiceWithSameInputs_GeneratesDifferentTokens()
    {
 // Arrange
   var userId = "user-707";
        var email = "duplicate@test.com";

     // Act
        var token1 = _tokenService.GenerateToken(userId, email);
        System.Threading.Thread.Sleep(10); // Ensure different timestamps
  var token2 = _tokenService.GenerateToken(userId, email);

        // Assert
    token1.Should().NotBe(token2, "Each token should have unique JTI and IAT claims");
    }

    #endregion

    #region ValidateToken Tests

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsPrincipal()
    {
        // Arrange
      var userId = "user-808";
        var email = "valid@test.com";
     var token = _tokenService.GenerateToken(userId, email);

        // Act
      var principal = _tokenService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
    }

  [Fact]
    public void ValidateToken_WithValidToken_ContainsCorrectClaims()
  {
        // Arrange
        var userId = "user-909";
        var email = "claims@test.com";
   var token = _tokenService.GenerateToken(userId, email);

        // Act
  var principal = _tokenService.ValidateToken(token);

        // Assert
     principal.Should().NotBeNull();
  // During JWT validation, the claims are often mapped/transformed
 // Check for NameIdentifier which is the mapped claim type
 principal!.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId);
  // Email claim might be mapped to emailaddress
  var emailClaim = principal.Claims.FirstOrDefault(c => c.Value == email);
        emailClaim.Should().NotBeNull("Email should be present in claims");
    }

    [Fact]
    public void ValidateToken_WithInvalidSignature_ReturnsNull()
    {
        // Arrange
 var userId = "user-010";
        var email = "invalid@test.com";
        var token = _tokenService.GenerateToken(userId, email);
     
        // Tamper with the token by changing the last character
        var tamperedToken = token.Substring(0, token.Length - 1) + "X";

        // Act
        var principal = _tokenService.ValidateToken(tamperedToken);

     // Assert
        principal.Should().BeNull();
    }

 [Fact]
    public void ValidateToken_WithMalformedToken_ReturnsNull()
    {
        // Arrange
   var malformedToken = "not.a.valid.jwt.token";

   // Act
        var principal = _tokenService.ValidateToken(malformedToken);

        // Assert
        principal.Should().BeNull();
  }

    [Fact]
 public void ValidateToken_WithEmptyToken_ReturnsNull()
    {
    // Arrange
var emptyToken = "";

     // Act
        var principal = _tokenService.ValidateToken(emptyToken);

  // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithTokenFromDifferentKey_ReturnsNull()
    {
        // Arrange
        var differentConfig = new Dictionary<string, string?>
        {
        { "Jwt:Key", "ADifferentSecureKeyThatIsAtLeast32CharactersLong987654" },
            { "Jwt:Issuer", "TestIssuer" },
    { "Jwt:Audience", "TestAudience" }
        };

        var differentConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(differentConfig)
            .Build();

 var differentTokenService = new JwtTokenService(differentConfiguration);
     var token = differentTokenService.GenerateToken("user-111", "different@test.com");

        // Act
var principal = _tokenService.ValidateToken(token);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
 public void ValidateToken_WithWrongIssuer_ReturnsNull()
    {
 // Arrange
        var wrongIssuerConfig = new Dictionary<string, string?>
        {
            { "Jwt:Key", "ThisIsAVerySecureKeyThatIsAtLeast32CharactersLong123456" },
            { "Jwt:Issuer", "WrongIssuer" },
  { "Jwt:Audience", "TestAudience" }
        };

        var wrongConfiguration = new ConfigurationBuilder()
          .AddInMemoryCollection(wrongIssuerConfig)
      .Build();

    var wrongIssuerService = new JwtTokenService(wrongConfiguration);
        var token = wrongIssuerService.GenerateToken("user-222", "wrong@test.com");

     // Act
        var principal = _tokenService.ValidateToken(token);

    // Assert
    principal.Should().BeNull();
    }

    [Fact]
  public void ValidateToken_WithWrongAudience_ReturnsNull()
    {
        // Arrange
        var wrongAudienceConfig = new Dictionary<string, string?>
        {
     { "Jwt:Key", "ThisIsAVerySecureKeyThatIsAtLeast32CharactersLong123456" },
         { "Jwt:Issuer", "TestIssuer" },
     { "Jwt:Audience", "WrongAudience" }
  };

        var wrongConfiguration = new ConfigurationBuilder()
    .AddInMemoryCollection(wrongAudienceConfig)
   .Build();

        var wrongAudienceService = new JwtTokenService(wrongConfiguration);
        var token = wrongAudienceService.GenerateToken("user-333", "audience@test.com");

  // Act
        var principal = _tokenService.ValidateToken(token);

        // Assert
     principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithGarbageString_ReturnsNull()
    {
    // Arrange
        var garbageToken = "!@#$%^&*()_+{}|:\"<>?[];',./";

        // Act
        var principal = _tokenService.ValidateToken(garbageToken);

      // Assert
        principal.Should().BeNull();
    }

    #endregion

  #region Round-Trip Tests

    [Fact]
    public void RoundTrip_GenerateAndValidate_PreservesUserId()
    {
        // Arrange
      var userId = "roundtrip-user-123";
        var email = "roundtrip@test.com";

// Act
        var token = _tokenService.GenerateToken(userId, email);
        var principal = _tokenService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        var userIdClaim = principal!.FindFirst(ClaimTypes.NameIdentifier);
  userIdClaim.Should().NotBeNull();
        userIdClaim!.Value.Should().Be(userId);
    }

    [Fact]
    public void RoundTrip_GenerateAndValidate_PreservesEmail()
    {
        // Arrange
        var userId = "roundtrip-user-456";
        var email = "preserve@email.com";

    // Act
        var token = _tokenService.GenerateToken(userId, email);
        var principal = _tokenService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        // Email claims can be mapped to different claim types during validation
    // Check for any claim containing the email value
   var emailClaim = principal!.Claims.FirstOrDefault(c => c.Value == email);
        emailClaim.Should().NotBeNull("Email should be present in claims");
        emailClaim!.Value.Should().Be(email);
    }

    #endregion
}
