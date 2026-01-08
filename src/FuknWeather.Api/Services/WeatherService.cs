using System.Text.Json;
using FuknWeather.Api.Models;
using FuknWeather.Api.Configuration;
using Microsoft.Extensions.Options;

namespace FuknWeather.Api.Services;

/// <summary>
/// Weather service that integrates with external weather APIs.
/// </summary>
public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly WeatherApiSettings _settings;
    private readonly WeatherDescriptionService _descriptionService;

    /// <summary>
    /// Initializes a new instance of the WeatherService.
    /// </summary>
    public WeatherService(
        HttpClient httpClient,
        IOptions<WeatherApiSettings> settings,
        WeatherDescriptionService descriptionService)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _descriptionService = descriptionService;
    }

    /// <inheritdoc />
    public async Task<WeatherResponse> GetWeatherAsync(string zipCode)
    {
        // Validate zip code format
        if (string.IsNullOrEmpty(zipCode) || zipCode.Length != 5 || !zipCode.All(char.IsDigit))
        {
            throw new ArgumentException("Invalid zip code format. Must be 5 digits.", nameof(zipCode));
        }

        // Example using OpenWeatherMap API
        var url = $"{_settings.BaseUrl}/weather?zip={zipCode},US&appid={_settings.ApiKey}&units=imperial";
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var weatherData = JsonSerializer.Deserialize<ExternalWeatherData>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        if (weatherData == null)
        {
            throw new InvalidOperationException("Failed to parse weather data");
        }

        var temperature = weatherData.Main.Temp;
        var description = _descriptionService.GetColorfulDescription(temperature);

        return new WeatherResponse
        {
            ZipCode = zipCode,
            TemperatureFahrenheit = temperature,
            Description = description,
            Location = weatherData.Name
        };
    }
}
