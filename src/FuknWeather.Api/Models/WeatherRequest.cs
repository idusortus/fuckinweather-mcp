namespace FuknWeather.Api.Models;

/// <summary>
/// Request model for weather queries.
/// </summary>
public class WeatherRequest
{
    /// <summary>
    /// 5-digit US zip code.
    /// </summary>
    public string ZipCode { get; set; } = string.Empty;
}
