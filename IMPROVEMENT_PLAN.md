# Future Improvements Plan

This document outlines four key improvements for the fuckinweather-mcp project.

## Quick Links

Detailed issue templates are available in `.github/ISSUE_TEMPLATES/`:
- [Issue 1: Response Caching](.github/ISSUE_TEMPLATES/issue-01-response-caching.md)
- [Issue 2: Test Coverage](.github/ISSUE_TEMPLATES/issue-02-test-coverage.md)
- [Issue 3: Multiple Location Formats](.github/ISSUE_TEMPLATES/issue-03-multiple-location-formats.md)
- [Issue 4: Rate Limiting & Security](.github/ISSUE_TEMPLATES/issue-04-rate-limiting-security.md)

See [.github/ISSUE_TEMPLATES/README.md](.github/ISSUE_TEMPLATES/README.md) for instructions on creating GitHub issues from these templates.

## Issue 1: Add Response Caching to Reduce API Calls

### Description
Implement response caching to reduce the number of external API calls to OpenWeatherMap and improve performance.

### Rationale
- Weather data doesn't change frequently (typically every 10-15 minutes)
- OpenWeatherMap has rate limits (60 calls/minute on free tier)
- Caching reduces latency and costs
- Improves reliability during API outages

### Implementation Details
- Add in-memory caching using `IMemoryCache`
- Cache weather responses per zip code for 10-15 minutes
- Add cache configuration options in `appsettings.json`
- Add cache hit/miss logging for monitoring

### Acceptance Criteria
- Weather data is cached per zip code
- Cache expiration is configurable
- Subsequent requests within cache window return cached data
- Cache can be cleared/invalidated if needed
- Unit tests verify caching behavior

---

## Issue 2: Expand Test Coverage for Controllers and Services

### Description
Increase test coverage from current minimal state to comprehensive coverage of all controllers, services, and MCP components.

### Rationale
- Current test coverage is limited to `WeatherDescriptionService`
- No tests for `WeatherService`, `McpServer`, or `WeatherController`
- Comprehensive tests ensure reliability and prevent regressions
- Tests serve as documentation for expected behavior

### Implementation Details
- Add unit tests for `WeatherService` (with mocked HttpClient)
- Add unit tests for `McpServer` tool handling
- Add unit tests for `WeatherController` endpoints
- Add integration tests for end-to-end API scenarios
- Target 80%+ code coverage

### Test Scenarios to Cover
- Valid and invalid zip codes
- External API failures and error handling
- MCP tool listing and calling
- Temperature edge cases
- HTTP response codes

### Acceptance Criteria
- Test coverage reaches 80% or higher
- All public methods have corresponding tests
- Tests use proper mocking for external dependencies
- Tests are fast and reliable

---

## Issue 3: Add Support for Multiple Location Formats

### Description
Extend the API to support multiple location input formats beyond US zip codes, including city names, coordinates, and international postal codes.

### Rationale
- Current limitation to US zip codes only is restrictive
- Users may want weather by city name (e.g., "Seattle", "New York")
- International users need postal code support
- Coordinates (lat/lon) enable precise location queries

### Implementation Details
- Create new endpoint: `GET /api/weather/city/{cityName}`
- Create new endpoint: `GET /api/weather/coordinates?lat={lat}&lon={lon}`
- Add location type detection in existing endpoint
- Update MCP tool schema to support multiple formats
- Add proper validation for each format

### API Examples
```
GET /api/weather/10001              # US zip code (existing)
GET /api/weather/city/Seattle       # City name
GET /api/weather/city/London,UK     # City with country
GET /api/weather/coordinates?lat=47.6062&lon=-122.3321  # Coordinates
```

### Acceptance Criteria
- API accepts city names and returns weather
- API accepts lat/lon coordinates and returns weather
- Existing zip code functionality remains unchanged
- MCP tool schema updated for new formats
- Comprehensive tests for all formats
- Documentation updated with examples

---

## Issue 4: Implement Rate Limiting and API Security

### Description
Add rate limiting, API key authentication, and security best practices to protect the API from abuse and ensure production readiness.

### Rationale
- Public APIs without rate limiting can be abused
- Protect against DDoS and excessive usage
- Enable different rate limits for different API key tiers
- Improve monitoring and usage tracking
- Production-ready security posture

### Implementation Details
- Implement rate limiting using `AspNetCoreRateLimit` package
- Add API key authentication middleware
- Configure rate limits per endpoint (e.g., 100 requests/hour)
- Add request throttling for excessive usage
- Implement proper CORS policies
- Add security headers (HSTS, CSP, etc.)
- Log rate limit violations

### Security Features
- API key authentication (optional tier)
- Rate limiting per IP address
- Rate limiting per API key
- Request throttling with 429 responses
- Security headers middleware
- Input sanitization enhancements

### Acceptance Criteria
- Rate limiting active on all endpoints
- Configurable rate limits per endpoint
- Proper HTTP 429 responses when limit exceeded
- API key authentication works (optional tier)
- Security headers present in responses
- Rate limit metrics logged
- Documentation updated with rate limit information

---

## Implementation Priority

1. **Issue 2** (Test Coverage) - Should be done first to establish quality baseline
2. **Issue 1** (Caching) - Performance improvement with immediate impact
3. **Issue 3** (Multiple Formats) - Feature expansion for broader usage
4. **Issue 4** (Security) - Production readiness and protection

## Notes

- Each issue should be implemented in a separate PR
- All issues should include updated documentation
- All issues should include comprehensive tests
- Follow existing code style and patterns
