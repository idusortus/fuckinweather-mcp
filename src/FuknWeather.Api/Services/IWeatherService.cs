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
    
    /// <summary>
    /// Gets weather information for the specified zip code with a specific rating.
    /// </summary>
    /// <param name="zipCode">5-digit US zip code.</param>
    /// <param name="rating">Content rating for the description.</param>
    /// <returns>Weather response with temperature and description matching the rating.</returns>
    Task<WeatherResponse> GetWeatherAsync(string zipCode, Rating rating);
    
    /// <summary>
    /// Gets a weather description for a specific temperature and rating.
    /// </summary>
    /// <param name="temperature">Temperature in Fahrenheit.</param>
    /// <param name="rating">Content rating for the description.</param>
    /// <returns>A weather description.</returns>
    string GetDescriptionForTemperature(decimal temperature, Rating rating);
    
    /// <summary>
    /// Generates a random temperature and provides descriptions for all ratings.
    /// </summary>
    /// <returns>Random weather response with descriptions for each rating.</returns>
    RandomWeatherResponse GetRandomWeather();
}
