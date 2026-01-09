namespace FuknWeather.Api.Models;

/// <summary>
/// Response model for random weather endpoint containing descriptions for all ratings.
/// </summary>
public class RandomWeatherResponse
{
    /// <summary>
    /// Randomly generated temperature in Fahrenheit.
    /// </summary>
    public decimal TemperatureFahrenheit { get; set; }
    
    /// <summary>
    /// Weather descriptions for each rating.
    /// </summary>
    public Dictionary<string, string> DescriptionsByRating { get; set; } = new();
}
