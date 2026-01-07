# fukn-weather MCP Server Development Instructions

## Project Overview

**fukn-weather** is a .NET 10 WebAPI application that implements a Model Context Protocol (MCP) server. It provides colorful, NSFW weather descriptions based on zip code input.

### Core Functionality
- Accepts a user's zip code as input
- Returns current temperature in Fahrenheit
- Provides "colorful" and NSFW weather descriptions based on temperature
- Integrates with external weather API for real-time data
- Implements MCP protocol for AI assistant integration

## Architecture

### Technology Stack
- **.NET 10** - WebAPI framework
- **MCP SDK** - Model Context Protocol implementation
- **HttpClient** - External API integration
- **Weather API** - OpenWeatherMap, WeatherAPI.com, or similar
- **JSON** - Data serialization

### Project Structure
```
fukn-weather/
├── src/
│   ├── FuknWeather.Api/           # Main WebAPI project
│   │   ├── Controllers/
│   │   │   └── WeatherController.cs
│   │   ├── Services/
│   │   │   ├── IWeatherService.cs
│   │   │   ├── WeatherService.cs
│   │   │   └── WeatherDescriptionService.cs
│   │   ├── Models/
│   │   │   ├── WeatherRequest.cs
│   │   │   ├── WeatherResponse.cs
│   │   │   └── ExternalWeatherData.cs
│   │   ├── MCP/
│   │   │   ├── McpServer.cs
│   │   │   └── WeatherTool.cs
│   │   ├── Configuration/
│   │   │   └── WeatherApiSettings.cs
│   │   ├── Program.cs
│   │   └── FuknWeather.Api.csproj
│   └── FuknWeather.Tests/          # Unit tests
│       ├── Services/
│       │   └── WeatherDescriptionServiceTests.cs
│       └── FuknWeather.Tests.csproj
├── .gitignore
├── README.md
├── instructions.md
└── fukn-weather.sln
```

## Step 1: Project Initialization

### Create Solution and Projects
```bash
# Create solution
dotnet new sln -n fukn-weather

# Create WebAPI project
dotnet new webapi -n FuknWeather.Api -o src/FuknWeather.Api --framework net10.0

# Create test project
dotnet new xunit -n FuknWeather.Tests -o src/FuknWeather.Tests --framework net10.0

# Add projects to solution
dotnet sln add src/FuknWeather.Api/FuknWeather.Api.csproj
dotnet sln add src/FuknWeather.Tests/FuknWeather.Tests.csproj

# Add test reference
dotnet add src/FuknWeather.Tests/FuknWeather.Tests.csproj reference src/FuknWeather.Api/FuknWeather.Api.csproj
```

### Install Required NuGet Packages
```bash
# Navigate to API project
cd src/FuknWeather.Api

# Add MCP SDK (when available) or implement custom
# Note: MCP SDK may need to be implemented or use ModelContextProtocol package
dotnet add package Microsoft.Extensions.Http
dotnet add package Newtonsoft.Json
dotnet add package System.Text.Json

# For testing
cd ../FuknWeather.Tests
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

## Step 2: MCP Protocol Implementation

### Define MCP Tool Specification

Create `src/FuknWeather.Api/MCP/WeatherTool.cs`:

```csharp
namespace FuknWeather.Api.MCP;

public class WeatherTool
{
    public string Name => "get_fukn_weather";
    
    public string Description => "Get the current weather with a colorful, NSFW description for a given zip code";
    
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
```

### MCP Server Implementation

Create `src/FuknWeather.Api/MCP/McpServer.cs`:

```csharp
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace FuknWeather.Api.MCP;

public class McpServer
{
    private readonly IWeatherService _weatherService;
    private readonly WeatherTool _weatherTool;

    public McpServer(IWeatherService weatherService)
    {
        _weatherService = weatherService;
        _weatherTool = new WeatherTool();
    }

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
```

## Step 3: Models Definition

### Weather Request Model
Create `src/FuknWeather.Api/Models/WeatherRequest.cs`:

```csharp
namespace FuknWeather.Api.Models;

public class WeatherRequest
{
    public string ZipCode { get; set; } = string.Empty;
}
```

### Weather Response Model
Create `src/FuknWeather.Api/Models/WeatherResponse.cs`:

```csharp
namespace FuknWeather.Api.Models;

public class WeatherResponse
{
    public string ZipCode { get; set; } = string.Empty;
    public decimal TemperatureFahrenheit { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}
```

### External Weather API Model
Create `src/FuknWeather.Api/Models/ExternalWeatherData.cs`:

```csharp
namespace FuknWeather.Api.Models;

// Example for OpenWeatherMap API
public class ExternalWeatherData
{
    public Main Main { get; set; } = new();
    public string Name { get; set; } = string.Empty;
}

public class Main
{
    public decimal Temp { get; set; }
    public decimal Feels_Like { get; set; }
    public int Humidity { get; set; }
}
```

## Step 4: Service Implementation

### Weather Service Interface
Create `src/FuknWeather.Api/Services/IWeatherService.cs`:

```csharp
using FuknWeather.Api.Models;

namespace FuknWeather.Api.Services;

public interface IWeatherService
{
    Task<WeatherResponse> GetWeatherAsync(string zipCode);
}
```

### Weather Service Implementation
Create `src/FuknWeather.Api/Services/WeatherService.cs`:

```csharp
using System.Text.Json;
using FuknWeather.Api.Models;
using FuknWeather.Api.Configuration;
using Microsoft.Extensions.Options;

namespace FuknWeather.Api.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly WeatherApiSettings _settings;
    private readonly WeatherDescriptionService _descriptionService;

    public WeatherService(
        HttpClient httpClient,
        IOptions<WeatherApiSettings> settings,
        WeatherDescriptionService descriptionService)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _descriptionService = descriptionService;
    }

    public async Task<WeatherResponse> GetWeatherAsync(string zipCode)
    {
        // Example using OpenWeatherMap API
        var url = $"{_settings.BaseUrl}/weather?zip={zipCode},US&appid={_settings.ApiKey}&units=imperial";
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var weatherData = JsonSerializer.Deserialize<ExternalWeatherData>(content);
        
        if (weatherData == null)
        {
            throw new Exception("Failed to parse weather data");
        }

        var temperature = weatherData.Main.Temp;
        var description = _descriptionService.GetColorfulDescription(temperature);

        return new WeatherResponse
        {
            ZipCode = zipCode,
            TemperatureFahrenheit = temperature,
            Description = description,
            Location = weatherData.Name
        };
    }
}
```

### Weather Description Service
Create `src/FuknWeather.Api/Services/WeatherDescriptionService.cs`:

```csharp
namespace FuknWeather.Api.Services;

public class WeatherDescriptionService
{
    public string GetColorfulDescription(decimal temperature)
    {
        return temperature switch
        {
            < 0 => "It's colder than a witch's tit in a brass bra! Fucking freezing out there!",
            < 20 => "It's ball-shriveling cold! Bundle the fuck up!",
            < 32 => "Freezing your ass off weather. Don't be a dumbass, wear a coat!",
            < 40 => "Cold as fuck! Winter can eat a dick.",
            < 50 => "Pretty damn chilly. Jacket up, buttercup!",
            < 60 => "Kinda cool, not too shabby. Tolerable as hell.",
            < 70 => "Actually pretty fucking nice out! Get your ass outside!",
            < 80 => "Beautiful as fuck! Perfect weather for not being a hermit!",
            < 85 => "Warm and wonderful! Mother Nature's not being a bitch today!",
            < 90 => "Getting hot as balls! Shorts and tank top weather!",
            < 95 => "Hot as fuck! Stay hydrated, you dehydrated bastard!",
            < 100 => "Hotter than Satan's asshole! AC is your best friend!",
            < 110 => "Ungodly fucking hot! Are you living in hell?",
            _ => "What in the actual fuck?! This temperature is apocalyptic! Stay inside or die!"
        };
    }
}
```

## Step 5: Configuration

### Weather API Settings
Create `src/FuknWeather.Api/Configuration/WeatherApiSettings.cs`:

```csharp
namespace FuknWeather.Api.Configuration;

public class WeatherApiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}
```

### appsettings.json Configuration
Update `src/FuknWeather.Api/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "WeatherApi": {
    "ApiKey": "YOUR_API_KEY_HERE",
    "BaseUrl": "https://api.openweathermap.org/data/2.5"
  }
}
```

### Environment Variables Setup
Create `.env.example`:

```bash
WEATHER_API_KEY=your_openweathermap_api_key
WEATHER_API_BASE_URL=https://api.openweathermap.org/data/2.5
```

## Step 6: Program.cs Setup

Update `src/FuknWeather.Api/Program.cs`:

```csharp
using FuknWeather.Api.Configuration;
using FuknWeather.Api.Services;
using FuknWeather.Api.MCP;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Weather API settings
builder.Services.Configure<WeatherApiSettings>(
    builder.Configuration.GetSection("WeatherApi"));

// Register services
builder.Services.AddHttpClient<IWeatherService, WeatherService>();
builder.Services.AddSingleton<WeatherDescriptionService>();
builder.Services.AddScoped<McpServer>();

// Add CORS for MCP clients
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## Step 7: Controller Implementation

Create `src/FuknWeather.Api/Controllers/WeatherController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using FuknWeather.Api.Services;
using FuknWeather.Api.Models;
using FuknWeather.Api.MCP;
using System.Text.Json;

namespace FuknWeather.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;
    private readonly McpServer _mcpServer;

    public WeatherController(IWeatherService weatherService, McpServer mcpServer)
    {
        _weatherService = weatherService;
        _mcpServer = mcpServer;
    }

    [HttpGet("{zipCode}")]
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

    [HttpPost("mcp/tools")]
    public ActionResult<object> ListMcpTools()
    {
        return Ok(_mcpServer.ListTools());
    }

    [HttpPost("mcp/call")]
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

public class McpToolCallRequest
{
    public string ToolName { get; set; } = string.Empty;
    public JsonElement Arguments { get; set; }
}
```

## Step 8: Testing

### Unit Test Example
Create `src/FuknWeather.Tests/Services/WeatherDescriptionServiceTests.cs`:

```csharp
using FuknWeather.Api.Services;
using FluentAssertions;
using Xunit;

namespace FuknWeather.Tests.Services;

public class WeatherDescriptionServiceTests
{
    private readonly WeatherDescriptionService _service;

    public WeatherDescriptionServiceTests()
    {
        _service = new WeatherDescriptionService();
    }

    [Theory]
    [InlineData(-10, "witch's tit")]
    [InlineData(15, "ball-shriveling")]
    [InlineData(30, "Freezing")]
    [InlineData(45, "damn chilly")]
    [InlineData(65, "fucking nice")]
    [InlineData(75, "Beautiful as fuck")]
    [InlineData(85, "hot as balls")]
    [InlineData(95, "Hot as fuck")]
    [InlineData(105, "Satan's asshole")]
    [InlineData(115, "apocalyptic")]
    public void GetColorfulDescription_ReturnsExpectedRange(decimal temp, string expectedPhrase)
    {
        // Act
        var result = _service.GetColorfulDescription(temp);

        // Assert
        result.Should().Contain(expectedPhrase, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetColorfulDescription_AlwaysContainsProfanity()
    {
        // Arrange
        var temperatures = new[] { -10m, 0m, 32m, 50m, 75m, 95m, 110m };

        // Act & Assert
        foreach (var temp in temperatures)
        {
            var result = _service.GetColorfulDescription(temp);
            result.Should().NotBeNullOrEmpty();
            // Verify it's colorful (contains profanity)
            result.Should().MatchRegex(@"(fuck|shit|ass|hell|damn|bitch)", 
                "description should be NSFW");
        }
    }
}
```

## Step 9: Building and Running

### Build Commands
```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run API
cd src/FuknWeather.Api
dotnet run
```

### Testing Endpoints

#### Get Weather Directly
```bash
curl -X GET "https://localhost:7000/api/weather/10001"
```

#### List MCP Tools
```bash
curl -X POST "https://localhost:7000/api/weather/mcp/tools"
```

#### Call MCP Tool
```bash
curl -X POST "https://localhost:7000/api/weather/mcp/call" \
  -H "Content-Type: application/json" \
  -d '{
    "toolName": "get_fukn_weather",
    "arguments": {
      "zipCode": "10001"
    }
  }'
```

## Step 10: Weather API Integration

### Recommended Weather APIs

1. **OpenWeatherMap** (Recommended)
   - Free tier: 60 calls/minute
   - Sign up: https://openweathermap.org/api
   - Documentation: https://openweathermap.org/current

2. **WeatherAPI.com**
   - Free tier: 1M calls/month
   - Sign up: https://www.weatherapi.com/
   - Documentation: https://www.weatherapi.com/docs/

3. **National Weather Service (NWS)**
   - Free, no API key required
   - US only
   - Documentation: https://www.weather.gov/documentation/services-web-api

### Getting an API Key (OpenWeatherMap)
1. Visit https://openweathermap.org/api
2. Sign up for a free account
3. Navigate to API keys section
4. Copy your API key
5. Add to `appsettings.json` or environment variable

## Step 11: Deployment Considerations

### Environment Configuration
- Store API keys in environment variables or Azure Key Vault
- Never commit API keys to source control
- Use different API keys for development/staging/production

### Docker Support
Create `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/FuknWeather.Api/FuknWeather.Api.csproj", "src/FuknWeather.Api/"]
RUN dotnet restore "src/FuknWeather.Api/FuknWeather.Api.csproj"
COPY . .
WORKDIR "/src/src/FuknWeather.Api"
RUN dotnet build "FuknWeather.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FuknWeather.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FuknWeather.Api.dll"]
```

### docker-compose.yml
```yaml
version: '3.8'
services:
  fuknweather-api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - WeatherApi__ApiKey=${WEATHER_API_KEY}
      - WeatherApi__BaseUrl=${WEATHER_API_BASE_URL}
```

## Step 12: MCP Server Integration with AI Assistants

### Claude Desktop Integration

Create `claude_mcp_config.json`:

```json
{
  "mcpServers": {
    "fukn-weather": {
      "url": "http://localhost:5000/api/weather/mcp",
      "tools": ["get_fukn_weather"]
    }
  }
}
```

### Usage Example

When integrated with Claude or other AI assistants:

**User**: "What's the weather like in New York City?"

**Assistant** (calls MCP tool):
```json
{
  "toolName": "get_fukn_weather",
  "arguments": {
    "zipCode": "10001"
  }
}
```

**Response**:
```json
{
  "zipCode": "10001",
  "temperatureFahrenheit": 45,
  "description": "Pretty damn chilly. Jacket up, buttercup!",
  "location": "New York"
}
```

## Step 13: Additional Features (Optional Enhancements)

### Extended Weather Data
- Add humidity, wind speed, conditions
- Multi-day forecast support
- Weather alerts and warnings

### Additional Tools
- `get_forecast` - 5-day forecast with colorful descriptions
- `compare_weather` - Compare weather between zip codes
- `weather_alert` - Get severe weather alerts

### Caching
- Implement response caching to reduce API calls
- Cache weather data for 10-15 minutes

### Rate Limiting
- Implement rate limiting to prevent abuse
- Use `AspNetCoreRateLimit` NuGet package

## Step 14: Best Practices

### Security
- ✅ Validate all zip code inputs
- ✅ Sanitize external API responses
- ✅ Use HTTPS in production
- ✅ Implement API key rotation
- ✅ Rate limit endpoints

### Error Handling
- Handle invalid zip codes gracefully
- Handle external API failures
- Provide meaningful error messages
- Log errors for debugging

### Performance
- Use async/await consistently
- Cache weather data appropriately
- Use HttpClient factory pattern
- Implement circuit breaker for external API calls

### Code Quality
- Follow C# naming conventions
- Write comprehensive unit tests
- Document public APIs with XML comments
- Use dependency injection properly

## Conclusion

This guide provides a complete roadmap for building the **fukn-weather** MCP server. The implementation prioritizes:

1. **MCP Protocol Compliance** - Proper tool definition and handling
2. **Clean Architecture** - Separation of concerns with services
3. **Extensibility** - Easy to add new features and weather sources
4. **Production Ready** - Error handling, configuration, and deployment support
5. **Testability** - Unit testable services and controllers

Follow these instructions step-by-step to create a fully functional, colorful, and NSFW weather MCP server!
