namespace FuknWeather.Api.Models;

/// <summary>
/// External weather API response model (for OpenWeatherMap API).
/// </summary>
public class ExternalWeatherData
{
    /// <summary>
    /// Main weather data including temperature.
    /// </summary>
    public Main Main { get; set; } = new();

    /// <summary>
    /// Location name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Main weather data container.
/// </summary>
public class Main
{
    /// <summary>
    /// Current temperature.
    /// </summary>
    public decimal Temp { get; set; }

    /// <summary>
    /// Feels like temperature.
    /// </summary>
    public decimal Feels_Like { get; set; }

    /// <summary>
    /// Humidity percentage.
    /// </summary>
    public int Humidity { get; set; }
}
