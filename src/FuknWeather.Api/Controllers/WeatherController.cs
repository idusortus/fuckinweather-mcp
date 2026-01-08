using Microsoft.AspNetCore.Mvc;
using FuknWeather.Api.Services;
using FuknWeather.Api.Models;
using FuknWeather.Api.MCP;
using System.Text.Json;

namespace FuknWeather.Api.Controllers;

/// <summary>
/// Controller for weather and MCP operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;
    private readonly McpServer _mcpServer;

    /// <summary>
    /// Initializes a new instance of the WeatherController.
    /// </summary>
    public WeatherController(IWeatherService weatherService, McpServer mcpServer)
    {
        _weatherService = weatherService;
        _mcpServer = mcpServer;
    }

    /// <summary>
    /// Gets weather information for a given zip code.
    /// </summary>
    /// <param name="zipCode">5-digit US zip code.</param>
    /// <returns>Weather response with colorful description.</returns>
    [HttpGet("{zipCode}")]
    [ProducesResponseType(typeof(WeatherResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WeatherResponse>> GetWeather(string zipCode)
    {
        try
        {
            var weather = await _weatherService.GetWeatherAsync(zipCode);
            return Ok(weather);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lists all available MCP tools.
    /// </summary>
    /// <returns>List of MCP tools.</returns>
    [HttpPost("mcp/tools")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> ListMcpTools()
    {
        return Ok(_mcpServer.ListTools());
    }

    /// <summary>
    /// Executes an MCP tool call.
    /// </summary>
    /// <param name="request">Tool call request containing tool name and arguments.</param>
    /// <returns>Result of the tool execution.</returns>
    [HttpPost("mcp/call")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> CallMcpTool([FromBody] McpToolCallRequest request)
    {
        try
        {
            var result = await _mcpServer.HandleToolCall(request.ToolName, request.Arguments);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>
/// Request model for MCP tool calls.
/// </summary>
public class McpToolCallRequest
{
    /// <summary>
    /// Name of the tool to call.
    /// </summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// Arguments to pass to the tool.
    /// </summary>
    public JsonElement Arguments { get; set; }
}
