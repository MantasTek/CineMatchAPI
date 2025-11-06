using CineMatchAPI.Infrastructure.Services;
using FluentAssertions;
using System.Text;
using System.Text.Json;
using Xunit;

namespace CineMatch.Tests.Infrastructure;

/// <summary>
/// Tests for NullableDateTimeConverter
/// Target: Low cyclomatic complexity (Read: 8, Write: 2) with 0% coverage
/// Tests basic converter functionality independent of full JSON serialization
/// </summary>
public class NullableDateTimeConverterTests
{
    private readonly NullableDateTimeConverter _converter;

 public NullableDateTimeConverterTests()
    {
        _converter = new NullableDateTimeConverter();
  }

    #region Read Tests - Direct Converter Tests

    [Fact]
    public void Read_WithNullToken_ReturnsNull()
    {
    // Arrange
        var json = "null";
  var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read(); // Move to the token

    // Act
    var result = _converter.Read(ref reader, typeof(DateTime?), new JsonSerializerOptions());

        // Assert
        result.Should().BeNull();
}

    [Fact]
    public void Read_WithValidDateString_ReturnsDateTime()
    {
        // Arrange
        var json = "\"2024-06-15\"";
var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read(); // Move to the token

        // Act
        var result = _converter.Read(ref reader, typeof(DateTime?), new JsonSerializerOptions());

        // Assert
    result.Should().NotBeNull();
        result!.Value.Year.Should().Be(2024);
        result.Value.Month.Should().Be(6);
        result.Value.Day.Should().Be(15);
  }

    [Fact]
    public void Read_WithEmptyString_ReturnsNull()
    {
     // Arrange
        var json = "\"\"";
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read(); // Move to the token

  // Act
        var result = _converter.Read(ref reader, typeof(DateTime?), new JsonSerializerOptions());

        // Assert
  result.Should().BeNull();
    }

    [Fact]
    public void Read_WithWhitespaceString_ReturnsNull()
    {
// Arrange
 var json = "\"   \"";
    var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read(); // Move to the token

        // Act
        var result = _converter.Read(ref reader, typeof(DateTime?), new JsonSerializerOptions());

        // Assert
     result.Should().BeNull();
 }

    [Fact]
    public void Read_WithInvalidDateString_ReturnsNull()
    {
        // Arrange
 var json = "\"not-a-date\"";
    var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
   reader.Read(); // Move to the token

    // Act
        var result = _converter.Read(ref reader, typeof(DateTime?), new JsonSerializerOptions());

// Assert
 result.Should().BeNull();
    }

    [Fact]
    public void Read_WithDateTimeFormat_ReturnsDateTime()
    {
        // Arrange
        var json = "\"2023-12-25T10:30:00\"";
     var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
     reader.Read(); // Move to the token

        // Act
        var result = _converter.Read(ref reader, typeof(DateTime?), new JsonSerializerOptions());

        // Assert
        result.Should().NotBeNull();
        result!.Value.Year.Should().Be(2023);
        result.Value.Month.Should().Be(12);
     result.Value.Day.Should().Be(25);
    }

  [Fact]
    public void Read_WithLeapYearDate_ReturnsDateTime()
    {
  // Arrange
      var json = "\"2024-02-29\"";
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read(); // Move to the token

        // Act
        var result = _converter.Read(ref reader, typeof(DateTime?), new JsonSerializerOptions());

        // Assert
      result.Should().NotBeNull();
  result!.Value.Year.Should().Be(2024);
     result.Value.Month.Should().Be(2);
        result.Value.Day.Should().Be(29);
    }

    #endregion

    #region Write Tests - Direct Converter Tests

    [Fact]
    public void Write_WithNullValue_WritesNull()
    {
 // Arrange
   using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

      // Act
  _converter.Write(writer, null, new JsonSerializerOptions());
        writer.Flush();

 // Assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        json.Should().Be("null");
    }

    [Fact]
    public void Write_WithDateTimeValue_WritesFormattedString()
    {
 // Arrange
      using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
var date = new DateTime(2024, 6, 15);

        // Act
    _converter.Write(writer, date, new JsonSerializerOptions());
   writer.Flush();

        // Assert
      var json = Encoding.UTF8.GetString(stream.ToArray());
json.Should().Be("\"2024-06-15\"");
    }

    [Fact]
    public void Write_WithDateTimeIncludingTime_WritesDateOnly()
    {
     // Arrange
     using var stream = new MemoryStream();
 using var writer = new Utf8JsonWriter(stream);
var date = new DateTime(2024, 6, 15, 14, 30, 45);

        // Act
        _converter.Write(writer, date, new JsonSerializerOptions());
     writer.Flush();

        // Assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        json.Should().Be("\"2024-06-15\"");
 }

[Fact]
    public void Write_WithLeapYearDate_WritesCorrectly()
    {
  // Arrange
  using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        var date = new DateTime(2024, 2, 29);

        // Act
     _converter.Write(writer, date, new JsonSerializerOptions());
        writer.Flush();

        // Assert
     var json = Encoding.UTF8.GetString(stream.ToArray());
        json.Should().Be("\"2024-02-29\"");
    }

    [Fact]
    public void Write_WithOldDate_WritesCorrectly()
    {
// Arrange
      using var stream = new MemoryStream();
using var writer = new Utf8JsonWriter(stream);
    var date = new DateTime(1950, 1, 1);

        // Act
    _converter.Write(writer, date, new JsonSerializerOptions());
        writer.Flush();

    // Assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        json.Should().Be("\"1950-01-01\"");
    }

    [Fact]
    public void Write_WithMinDate_WritesCorrectly()
  {
   // Arrange
        using var stream = new MemoryStream();
 using var writer = new Utf8JsonWriter(stream);
        var date = DateTime.MinValue;

        // Act
     _converter.Write(writer, date, new JsonSerializerOptions());
    writer.Flush();

     // Assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
   json.Should().Be("\"0001-01-01\"");
    }

    #endregion

    #region Round-Trip Tests

 [Fact]
    public void RoundTrip_WithValidDate_PreservesValue()
    {
        // Arrange
     var originalDate = new DateTime(2024, 6, 15);

    // Write
        using var writeStream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(writeStream))
        {
            _converter.Write(writer, originalDate, new JsonSerializerOptions());
      }
        
        // Read
   var json = Encoding.UTF8.GetString(writeStream.ToArray());
var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
    var result = _converter.Read(ref reader, typeof(DateTime?), new JsonSerializerOptions());

        // Assert
        result.Should().Be(originalDate);
    }

 [Fact]
    public void RoundTrip_WithNullValue_PreservesNull()
    {
        // Arrange
        DateTime? originalDate = null;
        
   // Write
      using var writeStream = new MemoryStream();
using (var writer = new Utf8JsonWriter(writeStream))
 {
    _converter.Write(writer, originalDate, new JsonSerializerOptions());
        }
    
     // Read
        var json = Encoding.UTF8.GetString(writeStream.ToArray());
  var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
     var result = _converter.Read(ref reader, typeof(DateTime?), new JsonSerializerOptions());

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
