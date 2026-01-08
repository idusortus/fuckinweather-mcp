using System.Text.Json;
using FuknWeather.Api.Services;

namespace FuknWeather.Api.MCP;

/// <summary>
/// MCP server implementation for handling tool calls.
/// </summary>
public class McpServer
{
    private readonly IWeatherService _weatherService;
    private readonly WeatherTool _weatherTool;

    /// <summary>
    /// Initializes a new instance of the McpServer.
    /// </summary>
    public McpServer(IWeatherService weatherService)
    {
        _weatherService = weatherService;
        _weatherTool = new WeatherTool();
    }

    /// <summary>
    /// Handles a tool call request.
    /// </summary>
    /// <param name="toolName">Name of the tool to invoke.</param>
    /// <param name="arguments">Arguments for the tool.</param>
    /// <returns>Result of the tool execution.</returns>
    public async Task<object> HandleToolCall(string toolName, JsonElement arguments)
    {
        if (toolName != _weatherTool.Name)
        {
            throw new ArgumentException($"Unknown tool: {toolName}");
        }

        var zipCode = arguments.GetProperty("zipCode").GetString();
        
        if (string.IsNullOrEmpty(zipCode) || zipCode.Length != 5)
        {
            throw new ArgumentException("Invalid zip code format. Must be 5 digits.");
        }

        var weather = await _weatherService.GetWeatherAsync(zipCode);
        return weather;
    }

    /// <summary>
    /// Lists all available tools.
    /// </summary>
    /// <returns>List of available tools with their schemas.</returns>
    public object ListTools()
    {
        return new
        {
            tools = new[]
            {
                new
                {
                    name = _weatherTool.Name,
                    description = _weatherTool.Description,
                    inputSchema = _weatherTool.InputSchema
                }
            }
        };
    }
}
