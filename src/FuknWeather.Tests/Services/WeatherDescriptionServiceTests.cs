using FuknWeather.Api.Services;
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

    [Theory]
    [InlineData(-10, "witch's tit")]
    [InlineData(15, "ball-shriveling")]
    [InlineData(30, "Freezing")]
    [InlineData(45, "damn chilly")]
    [InlineData(65, "fucking nice")]
    [InlineData(75, "Beautiful as fuck")]
    [InlineData(85, "hot as balls")]
    [InlineData(97, "Satan's asshole")]
    [InlineData(105, "Ungodly")]
    [InlineData(115, "apocalyptic")]
    public void GetColorfulDescription_ReturnsExpectedRange(decimal temp, string expectedPhrase)
    {
        // Act
        var result = _service.GetColorfulDescription(temp);

        // Assert
        result.Should().ContainEquivalentOf(expectedPhrase);
    }

    [Fact]
    public void GetColorfulDescription_AlwaysContainsProfanity()
    {
        // Arrange
        var temperatures = new[] { -10m, 0m, 32m, 50m, 75m, 95m, 110m };

        // Act & Assert
        foreach (var temp in temperatures)
        {
            var result = _service.GetColorfulDescription(temp);
            result.Should().NotBeNullOrEmpty();
            // Verify it's colorful (contains profanity)
            result.Should().MatchRegex(@"(fuck|shit|ass|hell|damn|bitch)", 
                "description should be NSFW");
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
}
