# Adding API Key Authentication for External Users

When deploying fuckinweather-mcp as a shared MCP service, you'll want to add authentication to control access and track usage. This guide shows how to implement simple API key authentication.

## Overview

This implementation adds:
- API key validation middleware
- Header-based authentication (`X-API-Key` header)
- Easy key management
- Request logging per API key

## Implementation

### 1. Create API Key Authentication Handler

Create `src/FuknWeather.Api/Authentication/ApiKeyAuthenticationHandler.cs`:

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace FuknWeather.Api.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string HeaderName { get; set; } = "X-API-Key";
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
        _logger = logger.CreateLogger<ApiKeyAuthenticationHandler>();
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if API key header exists
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var apiKeyHeaderValues))
        {
            _logger.LogWarning("API key header '{HeaderName}' not found", Options.HeaderName);
            return Task.FromResult(AuthenticateResult.Fail("API key header not found"));
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            _logger.LogWarning("API key header '{HeaderName}' is empty", Options.HeaderName);
            return Task.FromResult(AuthenticateResult.Fail("API key is empty"));
        }

        // Get valid API keys from configuration
        var validApiKeys = _configuration.GetSection("ApiKeys").Get<Dictionary<string, string>>();
        if (validApiKeys == null || !validApiKeys.Any())
        {
            _logger.LogError("No valid API keys configured");
            return Task.FromResult(AuthenticateResult.Fail("API keys not configured"));
        }

        // Validate the provided API key
        var matchingKey = validApiKeys.FirstOrDefault(k => k.Value == providedApiKey);
        if (matchingKey.Key == null)
        {
            _logger.LogWarning("Invalid API key provided");
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        // Create claims for the authenticated user
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, matchingKey.Key),
            new Claim("ApiKey", providedApiKey)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        _logger.LogInformation("API key authenticated for client: {ClientName}", matchingKey.Key);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers.Append("WWW-Authenticate", $"{Options.HeaderName} realm=\"API Key\"");
        return Task.CompletedTask;
    }
}
```

### 2. Update Program.cs

Add authentication configuration to `src/FuknWeather.Api/Program.cs`:

```csharp
using FuknWeather.Api.Authentication;
using FuknWeather.Api.Configuration;
using FuknWeather.Api.Services;
using FuknWeather.Api.MCP;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure Weather API settings
builder.Services.Configure<WeatherApiSettings>(
    builder.Configuration.GetSection("WeatherApi"));

// Register services
builder.Services.AddHttpClient<IWeatherService, WeatherService>();
builder.Services.AddSingleton<WeatherDescriptionService>();
builder.Services.AddScoped<McpServer>();

// Add API Key Authentication
builder.Services.AddAuthentication(ApiKeyAuthenticationOptions.DefaultScheme)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.DefaultScheme, null);

builder.Services.AddAuthorization();

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
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();  // Add this
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 3. Protect Controllers with [Authorize]

Update your controllers to require authentication:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuknWeather.Api.Controllers;

[ApiController]
[Route("api/weather")]
[Authorize]  // Add this attribute
public class WeatherController : ControllerBase
{
    // Your existing controller code...
    
    // You can also add endpoints without auth if needed
    [AllowAnonymous]
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy" });
    }
}
```

### 4. Configure API Keys in appsettings.json

Add API keys to `src/FuknWeather.Api/appsettings.json`:

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
  },
  "ApiKeys": {
    "client1": "ak_live_abc123def456ghi789jkl012",
    "client2": "ak_live_mno345pqr678stu901vwx234",
    "test-client": "ak_test_xyz789abc012def345ghi678"
  }
}
```

**Important**: For production, store API keys in Azure Key Vault:

```bash
# Store each API key in Key Vault
az keyvault secret set \
  --vault-name kv-fukn-weather-prod \
  --name ApiKey-Client1 \
  --value "ak_live_abc123def456ghi789jkl012"

az keyvault secret set \
  --vault-name kv-fukn-weather-prod \
  --name ApiKey-Client2 \
  --value "ak_live_mno345pqr678stu901vwx234"
```

Then reference them in Azure App Service:

```bash
az webapp config appsettings set \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --settings \
    ApiKeys__Client1="@Microsoft.KeyVault(VaultName=kv-fukn-weather-prod;SecretName=ApiKey-Client1)" \
    ApiKeys__Client2="@Microsoft.KeyVault(VaultName=kv-fukn-weather-prod;SecretName=ApiKey-Client2)"
```

### 5. Test Authentication

```bash
# Without API key - Should return 401
curl -i https://your-app.azurewebsites.net/api/weather/10001

# With valid API key - Should return 200
curl -i https://your-app.azurewebsites.net/api/weather/10001 \
  -H "X-API-Key: ak_live_abc123def456ghi789jkl012"

# Health check without auth - Should return 200
curl -i https://your-app.azurewebsites.net/api/weather/health
```

## Usage for External Users

### Documentation for Your Users

Share this with external users consuming your MCP service:

```markdown
# fuckinweather-mcp API Documentation

## Authentication

All API requests require an API key to be included in the `X-API-Key` header.

### Example Request

```bash
curl https://fukn-weather-api.azurewebsites.net/api/weather/10001 \
  -H "X-API-Key: your_api_key_here"
```

### Response

```json
{
  "zipCode": "10001",
  "temperatureFahrenheit": 45.2,
  "description": "Pretty damn chilly. Jacket up, buttercup!",
  "location": "New York"
}
```

### Error Responses

- `401 Unauthorized` - Missing or invalid API key
- `400 Bad Request` - Invalid zip code format
- `404 Not Found` - Zip code not found
- `500 Internal Server Error` - Server error

## MCP Integration

For Claude Desktop, add to your configuration:

```json
{
  "mcpServers": {
    "fukn-weather": {
      "url": "https://fukn-weather-api.azurewebsites.net/api/weather/mcp",
      "headers": {
        "X-API-Key": "your_api_key_here"
      }
    }
  }
}
```
```

## Advanced: Rate Limiting

To add rate limiting per API key, install the `AspNetCoreRateLimit` package:

```bash
dotnet add package AspNetCoreRateLimit
```

Configure in `Program.cs`:

```csharp
// Add rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1m",
            Limit = 60
        }
    };
});
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// In the middleware pipeline
app.UseIpRateLimiting();
```

## API Key Generation Script

Use this simple script to generate secure API keys:

```bash
#!/bin/bash
# generate-api-key.sh

# Generate a secure random API key
CLIENT_NAME=$1
if [ -z "$CLIENT_NAME" ]; then
    echo "Usage: ./generate-api-key.sh <client-name>"
    exit 1
fi

# Generate random key
API_KEY="ak_live_$(openssl rand -hex 16)"

echo "Generated API key for $CLIENT_NAME:"
echo "$API_KEY"
echo ""
echo "Store in Key Vault:"
echo "az keyvault secret set \\"
echo "  --vault-name kv-fukn-weather-prod \\"
echo "  --name ApiKey-$CLIENT_NAME \\"
echo "  --value \"$API_KEY\""
```

## Monitoring API Key Usage

Add custom telemetry to track usage per API key:

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

public class WeatherController : ControllerBase
{
    private readonly TelemetryClient _telemetryClient;
    
    public WeatherController(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }
    
    [HttpGet("{zipCode}")]
    public async Task<IActionResult> GetWeather(string zipCode)
    {
        // Track custom event with API key
        var clientName = User.Identity?.Name ?? "unknown";
        _telemetryClient.TrackEvent("WeatherRequest", new Dictionary<string, string>
        {
            { "Client", clientName },
            { "ZipCode", zipCode }
        });
        
        // Your existing code...
    }
}
```

View usage in Application Insights:

```kusto
customEvents
| where name == "WeatherRequest"
| summarize RequestCount = count() by Client = tostring(customDimensions.Client)
| order by RequestCount desc
```

## Security Best Practices

1. **Use HTTPS Only**: Enforce HTTPS in production
2. **Rotate Keys Regularly**: Change API keys every 90 days
3. **Monitor for Abuse**: Set up alerts for unusual patterns
4. **Implement Rate Limiting**: Prevent API abuse
5. **Use Key Vault**: Never store keys in code or configuration files
6. **Add Logging**: Track all authentication attempts
7. **Implement IP Whitelisting**: Restrict access by IP if possible

## Troubleshooting

### Authentication Not Working

1. Check if authentication middleware is added before authorization
2. Verify API keys are correctly configured
3. Check Application Insights for authentication failures
4. Test with curl to isolate issues

### Key Vault Access Issues

```bash
# Verify managed identity has access
az keyvault show \
  --name kv-fukn-weather-prod \
  --query properties.accessPolicies
```

## Alternative: Azure API Management

For enterprise scenarios, consider Azure API Management:

```bash
# Create APIM instance
az apim create \
  --name fukn-weather-apim \
  --resource-group rg-fukn-weather-prod \
  --publisher-email admin@example.com \
  --publisher-name "Your Company" \
  --sku-name Developer

# Import your API
az apim api import \
  --resource-group rg-fukn-weather-prod \
  --service-name fukn-weather-apim \
  --path weather \
  --specification-url https://your-app.azurewebsites.net/swagger/v1/swagger.json
```

APIM provides:
- Built-in API key management
- Rate limiting and quotas
- Developer portal
- Analytics and monitoring
- OAuth integration
- IP filtering

## Summary

You now have:
- ✅ API key authentication
- ✅ Secure key storage in Key Vault
- ✅ Request logging per client
- ✅ Documentation for external users
- ✅ Rate limiting (optional)
- ✅ Usage monitoring

For more information, see:
- [Azure App Service Authentication](https://docs.microsoft.com/azure/app-service/overview-authentication-authorization)
- [Azure Key Vault](https://docs.microsoft.com/azure/key-vault/)
- [ASP.NET Core Authentication](https://docs.microsoft.com/aspnet/core/security/authentication/)
