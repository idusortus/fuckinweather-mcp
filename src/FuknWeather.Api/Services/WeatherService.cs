using System.Text.Json;
using FuknWeather.Api.Models;
using FuknWeather.Api.Configuration;
using FuknWeather.Api.Utilities;
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
    private readonly Random _random;

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
        _random = new Random();
    }

    /// <inheritdoc />
    public async Task<WeatherResponse> GetWeatherAsync(string zipCode)
    {
        return await GetWeatherAsync(zipCode, Rating.X);
    }

    /// <inheritdoc />
    public async Task<WeatherResponse> GetWeatherAsync(string zipCode, Rating rating)
    {
        // Validate zip code format
        if (!ValidationHelper.IsValidZipCode(zipCode, out var errorMessage))
        {
            throw new ArgumentException(errorMessage, nameof(zipCode));
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
        var description = _descriptionService.GetDescription(temperature, rating);

        return new WeatherResponse
        {
            ZipCode = zipCode,
            TemperatureFahrenheit = temperature,
            Description = description,
            Location = weatherData.Name,
            Rating = rating.ToString()
        };
    }

    /// <inheritdoc />
    public string GetDescriptionForTemperature(decimal temperature, Rating rating)
    {
        return _descriptionService.GetDescription(temperature, rating);
    }

    /// <inheritdoc />
    public RandomWeatherResponse GetRandomWeather()
    {
        // Generate random temperature between -50 and 140
        var randomTemp = _random.Next(-50, 141);
        
        var response = new RandomWeatherResponse
        {
            TemperatureFahrenheit = randomTemp
        };

        // Get descriptions for all ratings
        foreach (Rating rating in Enum.GetValues(typeof(Rating)))
        {
            var ratingKey = rating == Rating.PG13 ? "PG-13" : rating.ToString();
            var description = _descriptionService.GetDescription(randomTemp, rating);
            response.DescriptionsByRating[ratingKey] = description;
        }

        return response;
    }
}
