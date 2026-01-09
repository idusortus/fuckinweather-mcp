using FuknWeather.Api.Services;
using FuknWeather.Api.Models;
using FluentAssertions;
using Xunit;

namespace FuknWeather.Tests.Services;

/// <summary>
/// Unit tests for WeatherDescriptionService.
/// </summary>
public class WeatherDescriptionServiceTests
{
    private readonly WeatherDescriptionService _service;

    public WeatherDescriptionServiceTests()
    {
        _service = new WeatherDescriptionService();
    }

    [Fact]
    public void GetColorfulDescription_XRating_ReturnsEdgyContent()
    {
        // Arrange
        var temperatures = new[] { -10m, 0m, 32m, 50m, 75m, 95m, 110m };

        // Act & Assert
        foreach (var temp in temperatures)
        {
            var result = _service.GetColorfulDescription(temp);
            result.Should().NotBeNullOrEmpty("should return colorful X-rated description");
            // X-rated should be longer/more descriptive than G-rated
            result.Length.Should().BeGreaterThan(10, "X-rated descriptions should be substantial");
        }
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(35)]
    [InlineData(55)]
    [InlineData(72)]
    [InlineData(88)]
    [InlineData(92)]
    [InlineData(102)]
    [InlineData(120)]
    public void GetColorfulDescription_AlwaysReturnsNonEmptyString(decimal temp)
    {
        // Act
        var result = _service.GetColorfulDescription(temp);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(Rating.G)]
    [InlineData(Rating.PG)]
    [InlineData(Rating.PG13)]
    [InlineData(Rating.R)]
    [InlineData(Rating.X)]
    [InlineData(Rating.BLAND)]
    public void GetDescription_ReturnsDescriptionForAllRatings(Rating rating)
    {
        // Arrange
        var temperature = 72m;

        // Act
        var result = _service.GetDescription(temperature, rating);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(Rating.G, 72, "fuck|shit|ass|damn")]
    [InlineData(Rating.PG, 72, "fuck|shit")]
    [InlineData(Rating.BLAND, 72, "fuck|shit|ass|damn")]
    public void GetDescription_RespectsProfanityLevels(Rating rating, decimal temp, string forbiddenWords)
    {
        // Act
        var result = _service.GetDescription(temp, rating);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        
        if (rating == Rating.G)
        {
            result.Should().NotMatchRegex(forbiddenWords, "G-rated should not contain profanity");
        }
        else if (rating == Rating.PG)
        {
            result.Should().NotMatchRegex(forbiddenWords, "PG-rated should not contain strong profanity");
        }
        else if (rating == Rating.BLAND)
        {
            result.Should().NotMatchRegex(forbiddenWords, "BLAND should not contain profanity");
        }
    }

    [Theory]
    [InlineData(-50)]
    [InlineData(-25)]
    [InlineData(0)]
    [InlineData(32)]
    [InlineData(72)]
    [InlineData(95)]
    [InlineData(110)]
    [InlineData(140)]
    public void GetDescription_HandlesFullTemperatureRange(decimal temp)
    {
        // Act
        var result = _service.GetDescription(temp, Rating.PG);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetDescription_ReturnsRandomDescriptions()
    {
        // Arrange
        var temperature = 72m;
        var descriptions = new HashSet<string>();

        // Act - call multiple times to check randomization
        for (int i = 0; i < 20; i++)
        {
            var result = _service.GetDescription(temperature, Rating.PG);
            descriptions.Add(result);
        }

        // Assert - should get at least 2 different descriptions (randomization works)
        descriptions.Count.Should().BeGreaterThan(1, "service should return random descriptions");
    }

    [Theory]
    [InlineData(-51)]
    [InlineData(141)]
    [InlineData(150)]
    [InlineData(-100)]
    public void GetDescription_HandlesOutOfRangeTemperatures(decimal temp)
    {
        // Act
        var result = _service.GetDescription(temp, Rating.PG);

        // Assert - should still return something (clamped to valid range)
        result.Should().NotBeNullOrWhiteSpace();
    }
}
