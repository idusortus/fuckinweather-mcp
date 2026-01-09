using FuknWeather.Api.Services;
using FuknWeather.Api.Models;
using FuknWeather.Api.Configuration;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using Xunit;

namespace FuknWeather.Tests.Services;

/// <summary>
/// Unit tests for WeatherService.
/// </summary>
public class WeatherServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IOptions<WeatherApiSettings>> _settingsMock;
    private readonly WeatherDescriptionService _descriptionService;
    private readonly WeatherService _service;

    public WeatherServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        
        _settingsMock = new Mock<IOptions<WeatherApiSettings>>();
        _settingsMock.Setup(s => s.Value).Returns(new WeatherApiSettings
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://api.test.com"
        });

        _descriptionService = new WeatherDescriptionService();
        _service = new WeatherService(_httpClient, _settingsMock.Object, _descriptionService);
    }

    [Fact]
    public async Task GetWeatherAsync_WithValidZipCode_ReturnsWeatherResponse()
    {
        // Arrange
        var zipCode = "10001";
        var weatherData = new ExternalWeatherData
        {
            Name = "New York",
            Main = new Main { Temp = 72.5m }
        };

        SetupHttpResponse(weatherData);

        // Act
        var result = await _service.GetWeatherAsync(zipCode);

        // Assert
        result.Should().NotBeNull();
        result.ZipCode.Should().Be(zipCode);
        result.TemperatureFahrenheit.Should().Be(72.5m);
        result.Location.Should().Be("New York");
        result.Description.Should().NotBeNullOrWhiteSpace();
        result.Rating.Should().Be("X"); // Default rating
    }

    [Fact]
    public async Task GetWeatherAsync_WithRating_ReturnsAppropriateDescription()
    {
        // Arrange
        var zipCode = "10001";
        var weatherData = new ExternalWeatherData
        {
            Name = "New York",
            Main = new Main { Temp = 72.5m }
        };

        SetupHttpResponse(weatherData);

        // Act
        var result = await _service.GetWeatherAsync(zipCode, Rating.G);

        // Assert
        result.Should().NotBeNull();
        result.Rating.Should().Be("G");
        result.Description.Should().NotMatchRegex(@"(fuck|shit|ass|damn)", "G-rated should not contain profanity");
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("abcde")]
    [InlineData("123456")]
    public async Task GetWeatherAsync_WithInvalidZipCode_ThrowsArgumentException(string invalidZipCode)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetWeatherAsync(invalidZipCode));
    }

    [Theory]
    [InlineData(72.5, Rating.G)]
    [InlineData(32.0, Rating.PG)]
    [InlineData(95.0, Rating.R)]
    [InlineData(-10.0, Rating.BLAND)]
    public void GetDescriptionForTemperature_ReturnsAppropriateDescription(decimal temperature, Rating rating)
    {
        // Act
        var result = _service.GetDescriptionForTemperature(temperature, rating);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetRandomWeather_ReturnsValidResponse()
    {
        // Act
        var result = _service.GetRandomWeather();

        // Assert
        result.Should().NotBeNull();
        result.TemperatureFahrenheit.Should().BeInRange(-50, 140);
        result.DescriptionsByRating.Should().HaveCount(6); // All 6 ratings
        result.DescriptionsByRating.Should().ContainKey("G");
        result.DescriptionsByRating.Should().ContainKey("PG");
        result.DescriptionsByRating.Should().ContainKey("PG-13");
        result.DescriptionsByRating.Should().ContainKey("R");
        result.DescriptionsByRating.Should().ContainKey("X");
        result.DescriptionsByRating.Should().ContainKey("BLAND");

        foreach (var description in result.DescriptionsByRating.Values)
        {
            description.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void GetRandomWeather_GeneratesDifferentTemperatures()
    {
        // Arrange
        var temperatures = new HashSet<decimal>();

        // Act - call multiple times
        for (int i = 0; i < 50; i++)
        {
            var result = _service.GetRandomWeather();
            temperatures.Add(result.TemperatureFahrenheit);
        }

        // Assert - should get multiple different temperatures
        temperatures.Count.Should().BeGreaterThan(10, "random weather should generate varied temperatures");
    }

    private void SetupHttpResponse(ExternalWeatherData weatherData)
    {
        var json = JsonSerializer.Serialize(weatherData);
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
}
