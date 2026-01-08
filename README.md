# fuckinweather-mcp

A .NET 10 MCP (Model Context Protocol) server that provides colorful, NSFW weather information. Get the damn weather with attitude!

## Overview

This MCP server delivers real-time weather data with hilariously profane descriptions based on temperature. Built with .NET 10, it integrates with AI assistants through the Model Context Protocol to provide weather information in a uniquely entertaining way.

### Features

- 🌡️ Real-time temperature data in Fahrenheit
- 💬 Colorful, NSFW weather descriptions
- 🔌 MCP protocol support for AI assistant integration
- 🌐 RESTful API endpoints
- ✅ Comprehensive unit tests
- 🚀 Built with .NET 10

## Technology Stack

- **.NET 10** - WebAPI framework
- **C#** - Primary programming language
- **OpenWeatherMap API** - Weather data provider
- **xUnit** - Testing framework
- **FluentAssertions** - Test assertions
- **Moq** - Mocking framework

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- OpenWeatherMap API key (get one [here](https://openweathermap.org/api))

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/idusortus/fuckinweather-mcp.git
cd fuckinweather-mcp
```

### 2. Get an API Key

1. Visit [OpenWeatherMap](https://openweathermap.org/api)
2. Sign up for a free account
3. Navigate to the API keys section
4. Copy your API key

### 3. Configure the Application

#### Option A: Using appsettings.json (Development)

Edit `src/FuknWeather.Api/appsettings.json`:

```json
{
  "WeatherApi": {
    "ApiKey": "your_actual_api_key_here",
    "BaseUrl": "https://api.openweathermap.org/data/2.5"
  }
}
```

#### Option B: Using Environment Variables (Recommended for Production)

```bash
export WeatherApi__ApiKey="your_api_key_here"
export WeatherApi__BaseUrl="https://api.openweathermap.org/data/2.5"
```

### 4. Build and Run

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the API
cd src/FuknWeather.Api
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:7012`
- HTTP: `http://localhost:5263`

## API Endpoints

### Get Weather by Zip Code

```http
GET /api/weather/{zipCode}
```

**Example:**
```bash
curl https://localhost:7012/api/weather/10001
```

**Response:**
```json
{
  "zipCode": "10001",
  "temperatureFahrenheit": 45.2,
  "description": "Pretty damn chilly. Jacket up, buttercup!",
  "location": "New York"
}
```

### List MCP Tools

```http
POST /api/weather/mcp/tools
```

**Example:**
```bash
curl -X POST https://localhost:7012/api/weather/mcp/tools
```

**Response:**
```json
{
  "tools": [
    {
      "name": "get_fukn_weather",
      "description": "Get the current weather with a colorful, NSFW description for a given zip code",
      "inputSchema": {
        "type": "object",
        "properties": {
          "zipCode": {
            "type": "string",
            "description": "5-digit US zip code",
            "pattern": "^[0-9]{5}$"
          }
        },
        "required": ["zipCode"]
      }
    }
  ]
}
```

### Call MCP Tool

```http
POST /api/weather/mcp/call
```

**Example:**
```bash
curl -X POST https://localhost:7012/api/weather/mcp/call \
  -H "Content-Type: application/json" \
  -d '{
    "toolName": "get_fukn_weather",
    "arguments": {
      "zipCode": "90210"
    }
  }'
```

## Weather Descriptions

The service provides temperature-based descriptions:

| Temperature (°F) | Description Style |
|------------------|-------------------|
| < 0 | Arctic hellscape |
| 0-20 | Painfully cold |
| 20-32 | Freezing |
| 32-40 | Cold as fuck |
| 40-50 | Chilly |
| 50-60 | Cool but tolerable |
| 60-70 | Nice weather |
| 70-80 | Beautiful |
| 80-85 | Warm and wonderful |
| 85-90 | Getting hot |
| 90-95 | Hot as fuck |
| 95-100 | Dangerously hot |
| 100-110 | Hellish |
| 110+ | Apocalyptic |

## MCP Integration

This server implements the Model Context Protocol for integration with AI assistants like Claude.

### Claude Desktop Integration Example

Add to your Claude Desktop MCP configuration:

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

## Development

### Project Structure

```
fukn-weather/
├── src/
│   ├── FuknWeather.Api/           # Main WebAPI project
│   │   ├── Configuration/         # Configuration models
│   │   ├── Controllers/           # API controllers
│   │   ├── MCP/                   # MCP protocol implementation
│   │   ├── Models/                # Data models
│   │   ├── Services/              # Business logic
│   │   └── Program.cs             # Application entry point
│   └── FuknWeather.Tests/         # Unit tests
├── .gitignore
├── fukn-weather.sln               # Solution file
├── instructions.md                # Detailed development guide
└── README.md                      # This file
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with verbosity
dotnet test --verbosity normal

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Building for Production

```bash
dotnet publish -c Release -o ./publish
```

## Docker Support

### Build Docker Image

```bash
docker build -t fukn-weather:latest .
```

### Run with Docker Compose

```bash
docker-compose up
```

## Azure Deployment

Deploy this MCP service to Azure as a shared service for external users. See the comprehensive [Azure Deployment Guide](AZURE_DEPLOYMENT.md) for:

- **Step-by-step deployment instructions** for Azure App Service and Container Apps
- **Infrastructure as Code (IaC)** with Bicep templates
- **Automated deployment scripts** for bash and PowerShell
- **CI/CD pipeline** with GitHub Actions
- **Security best practices** including Key Vault integration
- **Monitoring and scaling** configurations
- **Cost estimates** for different deployment scenarios

### Quick Deploy to Azure

```bash
# Set your API key
export WEATHER_API_KEY="your_openweathermap_api_key"

# Run deployment script
./scripts/deploy-azure.sh dev
```

Or use the Bicep template directly:

```bash
az deployment group create \
  --resource-group rg-fukn-weather-dev \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/parameters.json \
  --parameters weatherApiKey="$WEATHER_API_KEY"
```

For detailed instructions, see [AZURE_DEPLOYMENT.md](AZURE_DEPLOYMENT.md).

## Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "WeatherApi": {
    "ApiKey": "YOUR_API_KEY_HERE",
    "BaseUrl": "https://api.openweathermap.org/data/2.5"
  }
}
```

### Environment Variables

- `WeatherApi__ApiKey` - OpenWeatherMap API key
- `WeatherApi__BaseUrl` - Weather API base URL
- `ASPNETCORE_ENVIRONMENT` - Environment (Development/Production)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is open source and available under the MIT License.

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/idusortus/fuckinweather-mcp).

---

**Note:** This service uses explicit language for entertainment purposes. It may not be suitable for all audiences or professional environments.