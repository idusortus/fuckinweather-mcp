namespace FuknWeather.Api.MCP;

/// <summary>
/// Defines the MCP tool for getting weather information.
/// </summary>
public class WeatherTool
{
    /// <summary>
    /// Gets the name of the tool.
    /// </summary>
    public string Name => "get_fukn_weather";
    
    /// <summary>
    /// Gets the description of what this tool does.
    /// </summary>
    public string Description => "Get the current weather with a colorful, NSFW description for a given zip code";
    
    /// <summary>
    /// Gets the input schema for this tool.
    /// </summary>
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            zipCode = new
            {
                type = "string",
                description = "5-digit US zip code",
                pattern = "^[0-9]{5}$"
            }
        },
        required = new[] { "zipCode" }
    };
}
