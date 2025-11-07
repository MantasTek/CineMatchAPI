using CineMatchAPI.Infrastructure.Services;
using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace CineMatch.Tests.Models;

/// <summary>
/// Comprehensive tests for TMDb model classes (TMDbResponse, TMDbMovie, TMDbMovieDetails)
/// to improve coverage from 0% to 100%
/// </summary>
public class TMDbModelsTests
{
    #region TMDbResponse Tests

    [Fact]
    public void TMDbResponse_DefaultConstructor_InitializesResultsAsEmptyList()
    {
        // Act
        var response = new TMDbResponse();

// Assert
        response.Should().NotBeNull();
        response.Results.Should().NotBeNull();
        response.Results.Should().BeEmpty();
    }

    [Fact]
    public void TMDbResponse_Results_CanBeSet()
    {
        // Arrange
        var response = new TMDbResponse();
      var movies = new List<TMDbMovie>
        {
            new TMDbMovie { Id = 1, Title = "Movie 1" },
         new TMDbMovie { Id = 2, Title = "Movie 2" }
        };

        // Act
        response.Results = movies;

        // Assert
        response.Results.Should().HaveCount(2);
   response.Results[0].Id.Should().Be(1);
        response.Results[1].Title.Should().Be("Movie 2");
    }

    [Fact]
    public void TMDbResponse_CanDeserializeFromJson()
    {
        // Arrange
        var json = @"{
            ""results"": [
                {
    ""id"": 550,
 ""title"": ""Fight Club"",
      ""overview"": ""An insomniac office worker..."",
       ""poster_path"": ""/pB8BM7pdSp6B6Ih7QZ4DrQ3PmJK.jpg"",
 ""vote_average"": 8.4,
    ""release_date"": ""1999-10-15""
                }
       ]
        }";

        var options = new JsonSerializerOptions
    {
     PropertyNameCaseInsensitive = true,
 Converters = { new NullableDateTimeConverter() }
        };

      // Act
        var response = JsonSerializer.Deserialize<TMDbResponse>(json, options);

        // Assert
        response.Should().NotBeNull();
   response!.Results.Should().HaveCount(1);
        response.Results[0].Id.Should().Be(550);
        response.Results[0].Title.Should().Be("Fight Club");
  }

    [Fact]
    public void TMDbResponse_CanDeserializeEmptyResults()
    {
        // Arrange
        var json = @"{""results"": []}";

        // Act
        var response = JsonSerializer.Deserialize<TMDbResponse>(json);

        // Assert
response.Should().NotBeNull();
        response!.Results.Should().BeEmpty();
    }

    [Fact]
    public void TMDbResponse_WithMultipleMovies_DeserializesAll()
    {
        // Arrange
        var json = @"{
 ""results"": [
 {""id"": 1, ""title"": ""Movie 1"", ""overview"": ""Overview 1"", ""poster_path"": ""/path1.jpg"", ""vote_average"": 7.5, ""release_date"": ""2020-01-01""},
     {""id"": 2, ""title"": ""Movie 2"", ""overview"": ""Overview 2"", ""poster_path"": ""/path2.jpg"", ""vote_average"": 8.0, ""release_date"": ""2021-02-02""},
     {""id"": 3, ""title"": ""Movie 3"", ""overview"": ""Overview 3"", ""poster_path"": ""/path3.jpg"", ""vote_average"": 6.5, ""release_date"": ""2022-03-03""}
            ]
   }";

 var options = new JsonSerializerOptions
        {
        PropertyNameCaseInsensitive = true,
         Converters = { new NullableDateTimeConverter() }
        };

        // Act
        var response = JsonSerializer.Deserialize<TMDbResponse>(json, options);

        // Assert
 response.Should().NotBeNull();
      response!.Results.Should().HaveCount(3);
        response.Results[0].Id.Should().Be(1);
        response.Results[1].Id.Should().Be(2);
      response.Results[2].Id.Should().Be(3);
    }

    [Fact]
    public void TMDbResponse_CanSerializeToJson()
    {
        // Arrange
      var response = new TMDbResponse
     {
      Results = new List<TMDbMovie>
            {
          new TMDbMovie { Id = 123, Title = "Test Movie" }
       }
        };

      // Act
        var json = JsonSerializer.Serialize(response);

        // Assert
        json.Should().Contain("\"Results\"");
        json.Should().Contain("123");
      json.Should().Contain("Test Movie");
    }

    #endregion

    #region TMDbMovie Tests

    [Fact]
    public void TMDbMovie_DefaultConstructor_InitializesWithDefaultValues()
    {
        // Act
        var movie = new TMDbMovie();

        // Assert
        movie.Should().NotBeNull();
        movie.Id.Should().Be(0);
        movie.Title.Should().Be(string.Empty);
    movie.Overview.Should().Be(string.Empty);
        movie.PosterPath.Should().Be(string.Empty);
        movie.VoteAverage.Should().Be(0);
        movie.ReleaseDate.Should().BeNull();
    }

    [Fact]
    public void TMDbMovie_Id_CanBeSetAndRetrieved()
    {
        // Arrange
        var movie = new TMDbMovie();

        // Act
        movie.Id = 550;

    // Assert
  movie.Id.Should().Be(550);
    }

  [Fact]
    public void TMDbMovie_Title_CanBeSetAndRetrieved()
    {
  // Arrange
     var movie = new TMDbMovie();

        // Act
        movie.Title = "The Shawshank Redemption";

        // Assert
        movie.Title.Should().Be("The Shawshank Redemption");
    }

    [Fact]
  public void TMDbMovie_Overview_CanBeSetAndRetrieved()
    {
        // Arrange
        var movie = new TMDbMovie();

    // Act
        movie.Overview = "Two imprisoned men bond over a number of years...";

        // Assert
        movie.Overview.Should().Be("Two imprisoned men bond over a number of years...");
    }

    [Fact]
    public void TMDbMovie_PosterPath_CanBeSetAndRetrieved()
    {
      // Arrange
        var movie = new TMDbMovie();

      // Act
   movie.PosterPath = "/9cqNxx0GxF0bflZmeSMuL5tnGzr.jpg";

        // Assert
        movie.PosterPath.Should().Be("/9cqNxx0GxF0bflZmeSMuL5tnGzr.jpg");
    }

    [Fact]
    public void TMDbMovie_VoteAverage_CanBeSetAndRetrieved()
    {
        // Arrange
        var movie = new TMDbMovie();

      // Act
   movie.VoteAverage = 8.7;

        // Assert
        movie.VoteAverage.Should().Be(8.7);
    }

    [Fact]
  public void TMDbMovie_ReleaseDate_CanBeSetAndRetrieved()
    {
        // Arrange
        var movie = new TMDbMovie();
        var date = new DateTime(1994, 9, 23);

        // Act
        movie.ReleaseDate = date;

        // Assert
  movie.ReleaseDate.Should().Be(date);
    }

    [Fact]
    public void TMDbMovie_ReleaseDate_CanBeNull()
    {
        // Arrange
  var movie = new TMDbMovie();

     // Act
        movie.ReleaseDate = null;

        // Assert
   movie.ReleaseDate.Should().BeNull();
    }

    [Fact]
    public void TMDbMovie_AllProperties_CanBeSetTogether()
    {
        // Arrange & Act
        var movie = new TMDbMovie
        {
            Id = 278,
            Title = "The Shawshank Redemption",
            Overview = "Framed in the 1940s for the double murder of his wife...",
      PosterPath = "/9cqNxx0GxF0bflZmeSMuL5tnGzr.jpg",
        VoteAverage = 8.7,
        ReleaseDate = new DateTime(1994, 9, 23)
 };

// Assert
        movie.Id.Should().Be(278);
     movie.Title.Should().Be("The Shawshank Redemption");
     movie.Overview.Should().Be("Framed in the 1940s for the double murder of his wife...");
        movie.PosterPath.Should().Be("/9cqNxx0GxF0bflZmeSMuL5tnGzr.jpg");
        movie.VoteAverage.Should().Be(8.7);
     movie.ReleaseDate.Should().Be(new DateTime(1994, 9, 23));
  }

    [Fact]
    public void TMDbMovie_DeserializesFromJson_WithSnakeCaseProperties()
    {
        // Arrange
        var json = @"{
    ""id"": 550,
  ""title"": ""Fight Club"",
   ""overview"": ""An insomniac office worker..."",
            ""poster_path"": ""/pB8BM7pdSp6B6Ih7QZ4DrQ3PmJK.jpg"",
            ""vote_average"": 8.4,
            ""release_date"": ""1999-10-15""
    }";

  var options = new JsonSerializerOptions
  {
         PropertyNameCaseInsensitive = true,
            Converters = { new NullableDateTimeConverter() }
        };

        // Act
      var movie = JsonSerializer.Deserialize<TMDbMovie>(json, options);

        // Assert
      movie.Should().NotBeNull();
    movie!.Id.Should().Be(550);
        movie.Title.Should().Be("Fight Club");
        movie.Overview.Should().Be("An insomniac office worker...");
        movie.PosterPath.Should().Be("/pB8BM7pdSp6B6Ih7QZ4DrQ3PmJK.jpg");
        movie.VoteAverage.Should().Be(8.4);
        movie.ReleaseDate.Should().NotBeNull();
movie.ReleaseDate!.Value.Year.Should().Be(1999);
        movie.ReleaseDate.Value.Month.Should().Be(10);
        movie.ReleaseDate.Value.Day.Should().Be(15);
    }

    [Fact]
    public void TMDbMovie_DeserializesFromJson_WithNullReleaseDate()
    {
        // Arrange
        var json = @"{
            ""id"": 100,
            ""title"": ""No Date Movie"",
            ""overview"": ""A movie without a date"",
            ""poster_path"": ""/poster.jpg"",
    ""vote_average"": 7.0,
   ""release_date"": null
 }";

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
    public void TMDbMovie_DeserializesFromJson_WithEmptyReleaseDate()
    {
        // Arrange
        var json = @"{
        ""id"": 101,
            ""title"": ""Empty Date Movie"",
  ""overview"": ""A movie with empty date"",
    ""poster_path"": ""/poster.jpg"",
            ""vote_average"": 6.5,
      ""release_date"": """"
        }";

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
    public void TMDbMovie_SerializesToJson_WithAllProperties()
    {
        // Arrange
        var movie = new TMDbMovie
{
    Id = 550,
   Title = "Fight Club",
          Overview = "An insomniac office worker...",
   PosterPath = "/pB8BM7pdSp6B6Ih7QZ4DrQ3PmJK.jpg",
      VoteAverage = 8.4,
       ReleaseDate = new DateTime(1999, 10, 15)
      };

var options = new JsonSerializerOptions
        {
       Converters = { new NullableDateTimeConverter() }
        };

        // Act
        var json = JsonSerializer.Serialize(movie, options);

  // Assert
        json.Should().Contain("550");
        json.Should().Contain("Fight Club");
      json.Should().Contain("An insomniac office worker...");
        json.Should().Contain("poster_path"); // Snake case due to JsonPropertyName attribute
  json.Should().Contain("8.4");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(5.5)]
    [InlineData(7.8)]
    [InlineData(9.0)]
    [InlineData(10.0)]
    public void TMDbMovie_VoteAverage_AcceptsVariousRatings(double rating)
    {
        // Arrange
  var movie = new TMDbMovie();

    // Act
  movie.VoteAverage = rating;

        // Assert
        movie.VoteAverage.Should().Be(rating);
    }

    [Theory]
    [InlineData(1, "Movie 1")]
    [InlineData(999999, "Very Popular Movie")]
    [InlineData(12345, "Average Movie")]
    public void TMDbMovie_WithVariousIdsAndTitles_StoresCorrectly(int id, string title)
    {
    // Arrange & Act
     var movie = new TMDbMovie
        {
       Id = id,
    Title = title
    };

        // Assert
     movie.Id.Should().Be(id);
  movie.Title.Should().Be(title);
    }

    [Fact]
    public void TMDbMovie_WithEmptyStrings_StoresEmptyStrings()
    {
        // Arrange & Act
        var movie = new TMDbMovie
        {
 Title = "",
            Overview = "",
     PosterPath = ""
      };

        // Assert
        movie.Title.Should().BeEmpty();
        movie.Overview.Should().BeEmpty();
   movie.PosterPath.Should().BeEmpty();
    }

    [Fact]
    public void TMDbMovie_WithLongOverview_StoresCompletely()
    {
        // Arrange
        var longOverview = new string('A', 1000);
     var movie = new TMDbMovie();

     // Act
        movie.Overview = longOverview;

   // Assert
        movie.Overview.Should().HaveLength(1000);
   movie.Overview.Should().Be(longOverview);
    }

    [Fact]
 public void TMDbMovie_PosterPath_WithSpecialCharacters_StoresCorrectly()
    {
    // Arrange
        var specialPath = "/p@th/with-special_characters/123.jpg";
        var movie = new TMDbMovie();

      // Act
        movie.PosterPath = specialPath;

        // Assert
        movie.PosterPath.Should().Be(specialPath);
    }

    #endregion

    #region TMDbMovieDetails Tests

    [Fact]
    public void TMDbMovieDetails_DefaultConstructor_InitializesWithDefaultValues()
    {
        // Act
    var details = new TMDbMovieDetails();

        // Assert
        details.Should().NotBeNull();
     details.Runtime.Should().Be(0);
    }

    [Fact]
    public void TMDbMovieDetails_Runtime_CanBeSetAndRetrieved()
    {
 // Arrange
  var details = new TMDbMovieDetails();

    // Act
        details.Runtime = 142;

        // Assert
  details.Runtime.Should().Be(142);
    }

 [Theory]
    [InlineData(60)]
    [InlineData(90)]
    [InlineData(120)]
    [InlineData(150)]
    [InlineData(180)]
    [InlineData(240)]
    public void TMDbMovieDetails_Runtime_AcceptsVariousValues(int runtime)
    {
    // Arrange
  var details = new TMDbMovieDetails();

  // Act
        details.Runtime = runtime;

        // Assert
        details.Runtime.Should().Be(runtime);
    }

    [Fact]
    public void TMDbMovieDetails_DeserializesFromJson()
    {
        // Arrange
        var json = @"{""runtime"": 142}";

        // Act
        var details = JsonSerializer.Deserialize<TMDbMovieDetails>(json);

        // Assert
        details.Should().NotBeNull();
        details!.Runtime.Should().Be(142);
    }

    [Fact]
    public void TMDbMovieDetails_DeserializesFromJson_WithZeroRuntime()
    {
        // Arrange
        var json = @"{""runtime"": 0}";

      // Act
      var details = JsonSerializer.Deserialize<TMDbMovieDetails>(json);

 // Assert
        details.Should().NotBeNull();
        details!.Runtime.Should().Be(0);
    }

    [Fact]
    public void TMDbMovieDetails_DeserializesFromJson_WithLargeRuntime()
    {
 // Arrange
        var json = @"{""runtime"": 500}";

        // Act
        var details = JsonSerializer.Deserialize<TMDbMovieDetails>(json);

   // Assert
        details.Should().NotBeNull();
        details!.Runtime.Should().Be(500);
    }

    [Fact]
  public void TMDbMovieDetails_DeserializesFromJson_CaseInsensitive()
  {
    // Arrange
        var json = @"{""Runtime"": 95}";
        var options = new JsonSerializerOptions
        {
        PropertyNameCaseInsensitive = true
        };

        // Act
        var details = JsonSerializer.Deserialize<TMDbMovieDetails>(json, options);

        // Assert
        details.Should().NotBeNull();
        details!.Runtime.Should().Be(95);
    }

    [Fact]
    public void TMDbMovieDetails_SerializesToJson()
    {
   // Arrange
    var details = new TMDbMovieDetails { Runtime = 148 };

        // Act
        var json = JsonSerializer.Serialize(details);

      // Assert
        json.Should().Contain("runtime"); // Lowercase due to JSON property name attribute
        json.Should().Contain("148");
    }

    [Fact]
    public void TMDbMovieDetails_SerializesToJson_WithZero()
    {
        // Arrange
        var details = new TMDbMovieDetails { Runtime = 0 };

        // Act
      var json = JsonSerializer.Serialize(details);

      // Assert
        json.Should().Contain("runtime"); // Lowercase due to JSON property name attribute
  json.Should().Contain("0");
    }

    [Fact]
    public void TMDbMovieDetails_WithCompleteMovieDetails_DeserializesRuntime()
    {
        // Arrange - Simulating actual TMDb API response structure
        var json = @"{
 ""id"": 550,
  ""title"": ""Fight Club"",
       ""runtime"": 139,
            ""budget"": 63000000,
        ""revenue"": 100853753,
        ""overview"": ""An insomniac office worker...""
        }";

    var options = new JsonSerializerOptions
      {
   PropertyNameCaseInsensitive = true
        };

        // Act
        var details = JsonSerializer.Deserialize<TMDbMovieDetails>(json, options);

        // Assert
  details.Should().NotBeNull();
        details!.Runtime.Should().Be(139);
    }

    [Fact]
  public void TMDbMovieDetails_Runtime_CanBeSetToNegative()
    {
        // Arrange
        var details = new TMDbMovieDetails();

        // Act
        details.Runtime = -1;

        // Assert
        details.Runtime.Should().Be(-1);
    }

    [Fact]
    public void TMDbMovieDetails_Runtime_CanBeSetToVeryLargeValue()
    {
        // Arrange
  var details = new TMDbMovieDetails();

     // Act
        details.Runtime = 99999;

    // Assert
        details.Runtime.Should().Be(99999);
    }

    #endregion

    #region Integration Tests - Full Workflow

 [Fact]
    public void TMDbModels_FullWorkflow_DeserializeResponseWithMultipleMoviesAndDetails()
    {
        // Arrange
  var responseJson = @"{
          ""results"": [
       {
        ""id"": 550,
     ""title"": ""Fight Club"",
            ""overview"": ""An insomniac office worker..."",
        ""poster_path"": ""/pB8BM7pdSp6B6Ih7QZ4DrQ3PmJK.jpg"",
          ""vote_average"": 8.4,
 ""release_date"": ""1999-10-15""
          },
   {
          ""id"": 278,
 ""title"": ""The Shawshank Redemption"",
   ""overview"": ""Framed in the 1940s..."",
          ""poster_path"": ""/9cqNxx0GxF0bflZmeSMuL5tnGzr.jpg"",
            ""vote_average"": 8.7,
            ""release_date"": ""1994-09-23""
    }
 ]
        }";

        var detailsJson = @"{""runtime"": 142}";

        var options = new JsonSerializerOptions
        {
 PropertyNameCaseInsensitive = true,
            Converters = { new NullableDateTimeConverter() }
        };

     // Act
        var response = JsonSerializer.Deserialize<TMDbResponse>(responseJson, options);
        var details = JsonSerializer.Deserialize<TMDbMovieDetails>(detailsJson);

        // Assert
        response.Should().NotBeNull();
        response!.Results.Should().HaveCount(2);
        
        var firstMovie = response.Results[0];
        firstMovie.Id.Should().Be(550);
  firstMovie.Title.Should().Be("Fight Club");
        firstMovie.VoteAverage.Should().Be(8.4);
        firstMovie.ReleaseDate.Should().NotBeNull();

var secondMovie = response.Results[1];
        secondMovie.Id.Should().Be(278);
      secondMovie.Title.Should().Be("The Shawshank Redemption");
        secondMovie.VoteAverage.Should().Be(8.7);

 details.Should().NotBeNull();
      details!.Runtime.Should().Be(142);
    }

    [Fact]
 public void TMDbModels_RoundTrip_SerializeAndDeserialize()
    {
    // Arrange
        var originalResponse = new TMDbResponse
        {
Results = new List<TMDbMovie>
      {
        new TMDbMovie
          {
 Id = 550,
        Title = "Test Movie",
     Overview = "Test Overview",
                    PosterPath = "/test.jpg",
VoteAverage = 7.5,
          ReleaseDate = new DateTime(2020, 5, 15)
       }
    }
        };

        var originalDetails = new TMDbMovieDetails { Runtime = 120 };

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
  Converters = { new NullableDateTimeConverter() }
      };

        // Act - Serialize
    var responseJson = JsonSerializer.Serialize(originalResponse, options);
     var detailsJson = JsonSerializer.Serialize(originalDetails);

        // Act - Deserialize
        var deserializedResponse = JsonSerializer.Deserialize<TMDbResponse>(responseJson, options);
        var deserializedDetails = JsonSerializer.Deserialize<TMDbMovieDetails>(detailsJson);

      // Assert
        deserializedResponse.Should().NotBeNull();
        deserializedResponse!.Results.Should().HaveCount(1);
     deserializedResponse.Results[0].Id.Should().Be(550);
        deserializedResponse.Results[0].Title.Should().Be("Test Movie");

  deserializedDetails.Should().NotBeNull();
    deserializedDetails!.Runtime.Should().Be(120);
    }

    #endregion
}
