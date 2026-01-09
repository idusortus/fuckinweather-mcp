using System.Text.Json;
using FuknWeather.Api.Models;

namespace FuknWeather.Api.Services;

/// <summary>
/// Service that generates colorful, NSFW weather descriptions based on temperature and rating.
/// </summary>
public class WeatherDescriptionService
{
    private readonly WeatherDescriptionData _descriptionData;
    private static readonly Random _sharedRandom = new Random();
    private readonly object _lock = new object();

    /// <summary>
    /// Initializes a new instance of the WeatherDescriptionService.
    /// </summary>
    public WeatherDescriptionService()
    {
        _descriptionData = LoadDescriptions();
    }

    /// <summary>
    /// Loads weather descriptions from the JSON file.
    /// </summary>
    private WeatherDescriptionData LoadDescriptions()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "weather-descriptions.json");
        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<WeatherDescriptionData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        return data ?? new WeatherDescriptionData();
    }

    /// <summary>
    /// Gets a colorful, NSFW description based on the given temperature (defaults to X rating for backward compatibility).
    /// </summary>
    /// <param name="temperature">Temperature in Fahrenheit.</param>
    /// <returns>A colorful weather description.</returns>
    public string GetColorfulDescription(decimal temperature)
    {
        return GetDescription(temperature, Rating.X);
    }

    /// <summary>
    /// Gets a weather description based on temperature and rating.
    /// </summary>
    /// <param name="temperature">Temperature in Fahrenheit.</param>
    /// <param name="rating">Content rating for the description.</param>
    /// <returns>A weather description appropriate for the rating.</returns>
    public string GetDescription(decimal temperature, Rating rating)
    {
        var tempRange = GetTemperatureRange((int)Math.Round(temperature));
        var ratingKey = rating == Rating.PG13 ? "PG-13" : rating.ToString();
        
        if (_descriptionData.Descriptions.TryGetValue(ratingKey, out var ranges))
        {
            if (ranges.TryGetValue(tempRange, out var descriptions) && descriptions.Count > 0)
            {
                int index;
                lock (_lock)
                {
                    index = _sharedRandom.Next(descriptions.Count);
                }
                return descriptions[index];
            }
        }
        
        // Fallback for missing data
        return $"Temperature: {temperature}°F";
    }

    /// <summary>
    /// Gets the temperature range key for a given temperature.
    /// </summary>
    /// <param name="temp">Temperature in Fahrenheit.</param>
    /// <returns>Temperature range key (e.g., "25_29").</returns>
    private string GetTemperatureRange(int temp)
    {
        // Clamp temperature to our range
        temp = Math.Clamp(temp, -50, 140);
        
        // Find the range start (rounds down to nearest 5)
        var rangeStart = (int)(Math.Floor(temp / 5.0) * 5);
        var rangeEnd = rangeStart + 4;
        
        return $"{rangeStart}_{rangeEnd}";
    }
}
