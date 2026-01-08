using FuknWeather.Api.Models;

namespace FuknWeather.Api.Services;

/// <summary>
/// Interface for weather service operations.
/// </summary>
public interface IWeatherService
{
    /// <summary>
    /// Gets weather information for the specified zip code.
    /// </summary>
    /// <param name="zipCode">5-digit US zip code.</param>
    /// <returns>Weather response with temperature and colorful description.</returns>
    Task<WeatherResponse> GetWeatherAsync(string zipCode);
}
