---
title: "Add Support for Multiple Location Formats"
labels: ["enhancement", "feature"]
assignees: []
---

## Description

Extend the API to support multiple location input formats beyond US zip codes, including city names, coordinates, and international postal codes.

## Problem Statement

Currently, the API only accepts US 5-digit zip codes. This creates limitations:
- International users cannot use the service
- Users who don't know zip codes cannot query by city
- Cannot get weather for specific coordinates (lat/lon)
- Limited use cases and user adoption

## Current Functionality

```
GET /api/weather/10001  ✓ (US Zip only)
```

## Proposed Solution

Add support for multiple location input formats:
1. **City names** (with optional country code)
2. **Geographic coordinates** (latitude/longitude)
3. **International postal codes** (future consideration)

## Implementation Details

### 1. New API Endpoints

```csharp
// City name endpoint
GET /api/weather/city/{cityName}
GET /api/weather/city/Seattle
GET /api/weather/city/London,UK

// Coordinates endpoint  
GET /api/weather/coordinates?lat={lat}&lon={lon}
GET /api/weather/coordinates?lat=47.6062&lon=-122.3321
```

### 2. Update WeatherController

```csharp
[HttpGet("city/{cityName}")]
public async Task<ActionResult<WeatherResponse>> GetWeatherByCity(string cityName)
{
    // Implementation
}

[HttpGet("coordinates")]
public async Task<ActionResult<WeatherResponse>> GetWeatherByCoordinates(
    [FromQuery] double lat,
    [FromQuery] double lon)
{
    // Implementation
}
```

### 3. Extend WeatherService Interface

```csharp
public interface IWeatherService
{
    Task<WeatherResponse> GetWeatherAsync(string zipCode);
    Task<WeatherResponse> GetWeatherByCityAsync(string cityName);
    Task<WeatherResponse> GetWeatherByCoordinatesAsync(double latitude, double longitude);
}
```

### 4. OpenWeatherMap API Support

The OpenWeatherMap API already supports these formats:
```
# By city name
https://api.openweathermap.org/data/2.5/weather?q={city}&appid={key}

# By coordinates
https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={key}
```

### 5. Update MCP Tool Schema

Extend `WeatherTool` to support multiple input formats:

```csharp
public object InputSchema => new
{
    type = "object",
    properties = new
    {
        // Existing
        zipCode = new
        {
            type = "string",
            description = "5-digit US zip code",
            pattern = "^[0-9]{5}$"
        },
        // New
        city = new
        {
            type = "string",
            description = "City name, optionally with country code (e.g., 'Seattle' or 'London,UK')"
        },
        latitude = new
        {
            type = "number",
            description = "Latitude coordinate (-90 to 90)"
        },
        longitude = new
        {
            type = "number", 
            description = "Longitude coordinate (-180 to 180)"
        }
    },
    oneOf = new[]
    {
        new { required = new[] { "zipCode" } },
        new { required = new[] { "city" } },
        new { required = new[] { "latitude", "longitude" } }
    }
};
```

### 6. Input Validation

Create validation helpers:
```csharp
// ValidationHelper extensions
public static bool IsValidCityName(string? cityName, out string? errorMessage)
public static bool IsValidCoordinates(double lat, double lon, out string? errorMessage)
```

Validation rules:
- City name: 1-100 characters, alphanumeric plus spaces and commas
- Latitude: -90 to 90
- Longitude: -180 to 180

### 7. Update Models

Extend `WeatherRequest` to support multiple formats:
```csharp
public class WeatherRequest
{
    public string? ZipCode { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
```

## API Examples

### By City Name
```bash
curl https://localhost:7012/api/weather/city/Seattle

Response:
{
  "city": "Seattle",
  "temperatureFahrenheit": 52.3,
  "description": "Pretty fucking nice out! Get your ass outside!",
  "location": "Seattle, US"
}
```

### By Coordinates
```bash
curl "https://localhost:7012/api/weather/coordinates?lat=47.6062&lon=-122.3321"

Response:
{
  "latitude": 47.6062,
  "longitude": -122.3321,
  "temperatureFahrenheit": 52.3,
  "description": "Pretty fucking nice out! Get your ass outside!",
  "location": "Seattle"
}
```

### MCP Tool Call
```json
{
  "toolName": "get_fukn_weather",
  "arguments": {
    "city": "New York"
  }
}
```

## Acceptance Criteria

- [ ] API accepts city names and returns weather successfully
- [ ] API accepts lat/lon coordinates and returns weather successfully
- [ ] City names with country codes work (e.g., "London,UK")
- [ ] Existing zip code functionality remains unchanged (backward compatible)
- [ ] Proper validation for all input formats
- [ ] Meaningful error messages for invalid inputs
- [ ] MCP tool schema updated to support all formats
- [ ] MCP server can handle all location types
- [ ] Comprehensive unit tests for all formats
- [ ] Integration tests for new endpoints
- [ ] API documentation updated (README.md)
- [ ] OpenAPI/Swagger documentation updated
- [ ] Example requests in documentation

## Testing Requirements

### Unit Tests
- Valid city names return weather
- Invalid city names return errors
- Valid coordinates return weather
- Invalid coordinates (out of range) return errors
- City with country code parsing works
- Edge cases (empty strings, null values)

### Integration Tests
- End-to-end city weather retrieval
- End-to-end coordinate weather retrieval
- MCP tool calls with different formats

## Error Handling

- City not found: Return 404 with helpful message
- Invalid coordinates: Return 400 with validation error
- External API errors: Return appropriate status with message

## Documentation Updates

Update README.md with new endpoint examples:
```markdown
### Get Weather by City

GET /api/weather/city/{cityName}

### Get Weather by Coordinates

GET /api/weather/coordinates?lat={lat}&lon={lon}
```

## Future Enhancements

- International postal code support
- Multiple cities in one request
- Batch coordinate queries
- Search autocomplete for city names

## Priority

**Medium** - Feature expansion for broader usage

## Estimated Effort

Medium (2-3 days)

## Dependencies

- OpenWeatherMap API supports these features (already available)
- No new external dependencies required

## Breaking Changes

**None** - This is additive only. Existing zip code API remains unchanged.
