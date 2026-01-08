---
title: "Implement Rate Limiting and API Security"
labels: ["security", "enhancement"]
assignees: []
---

## Description

Add rate limiting, API key authentication, and security best practices to protect the API from abuse and ensure production readiness.

## Problem Statement

The current API has no protection mechanisms:
- No rate limiting → can be abused/DDoS'd
- No authentication → anyone can use unlimited requests
- No request throttling → resource exhaustion possible
- Missing security headers
- No usage tracking or monitoring

This creates risks:
- Service abuse and excessive costs
- Potential denial of service
- No accountability for API usage
- Not production-ready

## Proposed Solution

Implement comprehensive security measures:
1. Rate limiting per IP address and per API key
2. Optional API key authentication
3. Security headers middleware
4. Request throttling with proper HTTP responses
5. Logging and monitoring for security events

## Implementation Details

### 1. Add Rate Limiting Package

```bash
dotnet add package AspNetCoreRateLimit
```

### 2. Configure Rate Limiting

Create `src/FuknWeather.Api/Configuration/RateLimitSettings.cs`:
```csharp
public class RateLimitSettings
{
    public bool EnableRateLimiting { get; set; } = true;
    public int PerIpLimit { get; set; } = 100;
    public int PeriodInMinutes { get; set; } = 60;
    public int PerEndpointLimit { get; set; } = 50;
}
```

Add to `appsettings.json`:
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1h",
        "Limit": 100
      },
      {
        "Endpoint": "*:/api/weather/*",
        "Period": "1m",
        "Limit": 20
      }
    ]
  }
}
```

### 3. Register Rate Limiting in Program.cs

```csharp
// Add rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// In middleware pipeline
app.UseIpRateLimiting();
```

### 4. API Key Authentication (Optional Tier)

Create `src/FuknWeather.Api/Middleware/ApiKeyAuthenticationMiddleware.cs`:

```csharp
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public async Task InvokeAsync(HttpContext context)
    {
        // Check for X-API-Key header
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            // Anonymous access allowed with lower rate limits
            context.Items["RateLimit"] = 10; // requests per minute
        }
        else
        {
            // Validate API key and set higher limits
            if (IsValidApiKey(apiKey))
            {
                context.Items["RateLimit"] = 100;
                context.Items["ApiKeyTier"] = "Premium";
            }
            else
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid API key");
                return;
            }
        }

        await _next(context);
    }

    private bool IsValidApiKey(string apiKey)
    {
        // Validate against database or configuration
        return true; // Implement actual validation
    }
}
```

### 5. Security Headers Middleware

Create `src/FuknWeather.Api/Middleware/SecurityHeadersMiddleware.cs`:

```csharp
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "no-referrer");
        context.Response.Headers.Add("Content-Security-Policy", 
            "default-src 'self'; script-src 'self'; style-src 'self'");
        
        // HSTS (Strict-Transport-Security) for HTTPS
        if (context.Request.IsHttps)
        {
            context.Response.Headers.Add("Strict-Transport-Security", 
                "max-age=31536000; includeSubDomains");
        }

        await _next(context);
    }
}
```

### 6. Request Throttling

Implement throttling to prevent request flooding:
```csharp
// Return 429 Too Many Requests with Retry-After header
context.Response.StatusCode = 429;
context.Response.Headers.Add("Retry-After", "60");
await context.Response.WriteAsJsonAsync(new
{
    error = "Rate limit exceeded",
    message = "Too many requests. Please try again later.",
    retryAfter = 60
});
```

### 7. Enhanced CORS Configuration

Update CORS to be more restrictive:
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "https://claude.ai",
            "https://chat.openai.com",
            // Add trusted MCP client origins
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .WithExposedHeaders("X-Rate-Limit-Remaining", "X-Rate-Limit-Reset");
    });
});
```

### 8. Logging and Monitoring

Add logging for security events:
```csharp
public class SecurityEventLogger
{
    private readonly ILogger<SecurityEventLogger> _logger;

    public void LogRateLimitExceeded(string ipAddress, string endpoint)
    {
        _logger.LogWarning(
            "Rate limit exceeded for IP {IpAddress} on endpoint {Endpoint}",
            ipAddress, endpoint);
    }

    public void LogInvalidApiKey(string ipAddress)
    {
        _logger.LogWarning(
            "Invalid API key attempt from IP {IpAddress}",
            ipAddress);
    }

    public void LogSuspiciousActivity(string ipAddress, string reason)
    {
        _logger.LogWarning(
            "Suspicious activity from IP {IpAddress}: {Reason}",
            ipAddress, reason);
    }
}
```

### 9. Rate Limit Response Headers

Add informative headers to responses:
```csharp
X-Rate-Limit-Limit: 100
X-Rate-Limit-Remaining: 73
X-Rate-Limit-Reset: 1704672000
```

## Configuration Example

Complete `appsettings.json` security section:
```json
{
  "Security": {
    "EnableApiKeyAuth": false,
    "EnableRateLimiting": true,
    "EnableSecurityHeaders": true
  },
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1h",
        "Limit": 100
      },
      {
        "Endpoint": "*:/api/weather/*",
        "Period": "1m",
        "Limit": 20
      }
    ]
  },
  "ApiKeys": {
    "ValidKeys": [
      "key1_for_premium_tier",
      "key2_for_standard_tier"
    ]
  }
}
```

## Acceptance Criteria

- [ ] Rate limiting is active on all endpoints
- [ ] Rate limits are configurable per endpoint via config
- [ ] HTTP 429 responses returned when rate limit exceeded
- [ ] 429 responses include `Retry-After` header
- [ ] Rate limit headers included in all responses (X-Rate-Limit-*)
- [ ] API key authentication works (optional feature, can be disabled)
- [ ] Invalid API keys return 401 Unauthorized
- [ ] Security headers present in all responses:
  - X-Content-Type-Options: nosniff
  - X-Frame-Options: DENY
  - X-XSS-Protection: 1; mode=block
  - Strict-Transport-Security (HTTPS only)
  - Content-Security-Policy
- [ ] CORS policy is restrictive and configurable
- [ ] Rate limit violations are logged
- [ ] Invalid API key attempts are logged
- [ ] Unit tests for rate limiting middleware
- [ ] Unit tests for API key authentication
- [ ] Integration tests for rate limit scenarios
- [ ] Documentation updated with:
  - Rate limit information
  - API key usage (if enabled)
  - Security best practices

## Testing Requirements

### Unit Tests
- Rate limiting middleware behavior
- API key validation logic
- Security headers middleware
- Throttling logic

### Integration Tests
- Exceeding rate limits returns 429
- Rate limit resets after period
- Valid API keys are accepted
- Invalid API keys are rejected
- Security headers present in responses

### Load Tests
- Rate limiting under high load
- Performance impact of middleware

## Security Best Practices

1. **Input Validation** - Already implemented in ValidationHelper
2. **Output Encoding** - JSON responses are safe
3. **HTTPS Only** - Enforce in production
4. **Security Headers** - Implemented
5. **Rate Limiting** - Implemented
6. **Logging** - Security events logged
7. **API Keys** - Optional authentication
8. **CORS** - Restrictive policy

## Documentation Updates

Update README.md with:
```markdown
## Rate Limiting

This API implements rate limiting to ensure fair usage:
- **Anonymous users**: 100 requests per hour
- **Weather endpoints**: 20 requests per minute
- **429 responses** include Retry-After header

### Response Headers
- `X-Rate-Limit-Limit`: Maximum requests allowed
- `X-Rate-Limit-Remaining`: Requests remaining in current period
- `X-Rate-Limit-Reset`: Unix timestamp when limit resets

### API Key Authentication (Optional)

To get higher rate limits, include your API key:
```
curl -H "X-API-Key: your_api_key" https://api.example.com/api/weather/10001
```

### Error Response
```json
{
  "error": "Rate limit exceeded",
  "message": "Too many requests. Please try again later.",
  "retryAfter": 60
}
```
```

## Environment Variables

```bash
ENABLE_RATE_LIMITING=true
ENABLE_API_KEY_AUTH=false
RATE_LIMIT_PER_HOUR=100
RATE_LIMIT_PER_MINUTE=20
```

## Priority

**High** - Production readiness and protection

## Estimated Effort

Medium-Large (3-4 days)

## Dependencies

- AspNetCoreRateLimit (new NuGet package)
- IMemoryCache (already available)

## Breaking Changes

**Minor** - Clients may need to handle 429 responses and rate limit headers.

## Future Enhancements

- Redis-based distributed rate limiting for multi-instance deployments
- API key management dashboard
- Per-user rate limiting (requires user accounts)
- Advanced threat detection
- IP whitelisting/blacklisting
