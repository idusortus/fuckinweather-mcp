namespace FuknWeather.Api.Models;

/// <summary>
/// Response model containing weather information with colorful description.
/// </summary>
public class WeatherResponse
{
    /// <summary>
    /// The zip code for which weather was requested.
    /// </summary>
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>
    /// Temperature in Fahrenheit.
    /// </summary>
    public decimal TemperatureFahrenheit { get; set; }

    /// <summary>
    /// Colorful, NSFW description of the weather.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Location name (city).
    /// </summary>
    public string Location { get; set; } = string.Empty;
}
