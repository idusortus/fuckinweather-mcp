---
title: "Expand Test Coverage for Controllers and Services"
labels: ["testing", "quality"]
assignees: []
---

## Description

Increase test coverage from current minimal state to comprehensive coverage of all controllers, services, and MCP components.

## Problem Statement

Current test coverage is limited to `WeatherDescriptionService` only. There are no tests for:
- `WeatherService` (external API integration)
- `McpServer` (MCP protocol handling)
- `WeatherController` (API endpoints)
- Integration scenarios

This lack of coverage means:
- No safety net for refactoring
- Regressions can slip through
- Behavior is not documented via tests
- Lower confidence in production deployment

## Current Coverage

```
src/FuknWeather.Tests/
└── Services/
    └── WeatherDescriptionServiceTests.cs  ✓
```

## Proposed Solution

Add comprehensive unit and integration tests to achieve 80%+ code coverage.

## Implementation Details

### 1. WeatherService Tests
Create `src/FuknWeather.Tests/Services/WeatherServiceTests.cs`:

Test scenarios:
- Valid zip code returns weather data
- Invalid zip code throws exception
- External API HTTP errors are handled gracefully
- JSON deserialization handles malformed responses
- Temperature conversion is correct
- Location name is extracted properly

Use Moq to mock:
- `HttpClient` / `IHttpClientFactory`
- `IOptions<WeatherApiSettings>`
- `WeatherDescriptionService`

### 2. McpServer Tests
Create `src/FuknWeather.Tests/MCP/McpServerTests.cs`:

Test scenarios:
- `ListTools()` returns correct tool schema
- `HandleToolCall()` with valid tool name succeeds
- `HandleToolCall()` with invalid tool name throws
- `HandleToolCall()` with valid zip code calls weather service
- `HandleToolCall()` with invalid zip code throws
- JSON argument parsing works correctly

### 3. WeatherController Tests
Create `src/FuknWeather.Tests/Controllers/WeatherControllerTests.cs`:

Test scenarios:
- `GET /api/weather/{zipCode}` returns 200 with valid zip
- `GET /api/weather/{zipCode}` returns 400 with invalid zip
- `POST /api/weather/mcp/tools` returns tool list
- `POST /api/weather/mcp/call` succeeds with valid request
- `POST /api/weather/mcp/call` returns 400 with invalid request
- Error responses have proper format

### 4. Integration Tests
Create `src/FuknWeather.Tests/Integration/WeatherApiIntegrationTests.cs`:

Test scenarios:
- End-to-end API call with TestServer
- MCP tool call flow
- Error handling across layers
- Configuration loading

Use `Microsoft.AspNetCore.Mvc.Testing` for WebApplicationFactory.

### 5. Test Utilities
Create test helpers:
- Mock HTTP response builders
- Test data builders for weather responses
- Assertion helpers

## Test Structure Example

```csharp
public class WeatherServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly Mock<IOptions<WeatherApiSettings>> _mockSettings;
    private readonly Mock<WeatherDescriptionService> _mockDescriptionService;
    private readonly WeatherService _service;

    [Fact]
    public async Task GetWeatherAsync_WithValidZipCode_ReturnsWeatherResponse()
    {
        // Arrange
        var zipCode = "10001";
        // ... setup mocks

        // Act
        var result = await _service.GetWeatherAsync(zipCode);

        // Assert
        result.Should().NotBeNull();
        result.ZipCode.Should().Be(zipCode);
    }
}
```

## Acceptance Criteria

- [ ] Test coverage reaches 80% or higher (measured by code coverage tool)
- [ ] All public methods in services have corresponding tests
- [ ] All controller endpoints have unit tests
- [ ] MCP server functionality is fully tested
- [ ] Integration tests cover end-to-end scenarios
- [ ] Tests use proper mocking for external dependencies (HttpClient)
- [ ] Tests are fast (entire suite runs in < 10 seconds)
- [ ] Tests are reliable (no flaky tests)
- [ ] Test project follows xUnit best practices
- [ ] FluentAssertions used for readable assertions
- [ ] Tests are well-organized by component
- [ ] Documentation includes how to run tests with coverage

## Testing Best Practices to Follow

1. **Arrange-Act-Assert** pattern
2. **One assertion per test** (generally)
3. **Descriptive test names** (MethodName_Scenario_ExpectedBehavior)
4. **Mock external dependencies** (HTTP, database, etc.)
5. **Test both happy and sad paths**
6. **Use test data builders** for complex objects
7. **Avoid test interdependence**

## Required NuGet Packages

All already installed:
- ✓ xUnit
- ✓ Moq
- ✓ FluentAssertions
- ✓ Microsoft.AspNetCore.Mvc.Testing

## Code Coverage Tools

Add to test project:
```bash
dotnet add package coverlet.collector
```

Run with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Documentation Updates

- Update README.md with test running instructions
- Add section on writing new tests
- Document test organization structure

## Priority

**Critical** - Should be done first to establish quality baseline

## Estimated Effort

Medium-Large (3-4 days)

## Benefits

- Confidence in refactoring
- Documentation via tests
- Catch regressions early
- Better code quality
- Easier onboarding for contributors
