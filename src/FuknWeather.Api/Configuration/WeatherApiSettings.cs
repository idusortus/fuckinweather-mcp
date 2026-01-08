namespace FuknWeather.Api.Configuration;

/// <summary>
/// Configuration settings for the external weather API.
/// </summary>
public class WeatherApiSettings
{
    /// <summary>
    /// API key for authentication with the weather service.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the weather API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}
