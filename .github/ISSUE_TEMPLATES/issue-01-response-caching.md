---
title: "Add Response Caching to Reduce API Calls"
labels: ["enhancement", "performance"]
assignees: []
---

## Description

Implement response caching to reduce the number of external API calls to OpenWeatherMap and improve performance.

## Problem Statement

Currently, every weather request makes a fresh API call to OpenWeatherMap, even if the same zip code was just queried. This leads to:
- Unnecessary API calls (OpenWeatherMap has 60 calls/minute limit on free tier)
- Higher latency for repeated queries
- Potential rate limit exhaustion
- Increased costs if using paid tier

## Proposed Solution

Add in-memory caching using `IMemoryCache` to cache weather responses per zip code for 10-15 minutes.

## Implementation Details

### 1. Add Memory Cache Service
```csharp
// In Program.cs
builder.Services.AddMemoryCache();
```

### 2. Update WeatherService
- Inject `IMemoryCache` into `WeatherService`
- Check cache before making external API call
- Store successful responses in cache with TTL
- Add cache key based on zip code

### 3. Configuration
Add to `appsettings.json`:
```json
{
  "CacheSettings": {
    "WeatherCacheDurationMinutes": 15,
    "EnableCaching": true
  }
}
```

### 4. Logging
- Log cache hits and misses
- Log cache expiration events

## Acceptance Criteria

- [ ] Weather data is cached per zip code
- [ ] Cache expiration is configurable via `appsettings.json`
- [ ] Subsequent requests within cache window return cached data without external API call
- [ ] Cache can be cleared/invalidated if needed
- [ ] Unit tests verify caching behavior:
  - First request hits external API
  - Second request returns cached data
  - Request after TTL hits external API again
- [ ] Cache hit/miss metrics are logged
- [ ] Documentation updated with caching details

## Testing Requirements

- Unit tests with mocked `IMemoryCache`
- Integration tests verifying cache behavior
- Performance tests showing latency improvement

## Additional Considerations

- Consider distributed cache (Redis) for multi-instance deployments (future enhancement)
- Add cache statistics endpoint for monitoring
- Consider cache warming strategies

## Priority

**High** - Performance improvement with immediate impact

## Estimated Effort

Medium (1-2 days)
