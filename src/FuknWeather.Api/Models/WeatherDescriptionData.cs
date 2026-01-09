namespace FuknWeather.Api.Models;

/// <summary>
/// Container for weather description data loaded from JSON.
/// </summary>
public class WeatherDescriptionData
{
    /// <summary>
    /// Dictionary mapping rating to temperature ranges to descriptions.
    /// Key: Rating name, Value: Dictionary of temperature range to list of descriptions.
    /// </summary>
    public Dictionary<string, Dictionary<string, List<string>>> Descriptions { get; set; } = new();
}

/// <summary>
/// Represents a single temperature range with descriptions.
/// </summary>
public class TemperatureRange
{
    /// <summary>
    /// Minimum temperature in Fahrenheit (inclusive).
    /// </summary>
    public int MinTemp { get; set; }
    
    /// <summary>
    /// Maximum temperature in Fahrenheit (exclusive).
    /// </summary>
    public int MaxTemp { get; set; }
    
    /// <summary>
    /// List of descriptions for this temperature range.
    /// </summary>
    public List<string> Descriptions { get; set; } = new();
}
