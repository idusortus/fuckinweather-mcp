# Setting Up Monitoring and Alerts for Azure Deployment

This guide shows how to configure comprehensive monitoring and alerting for your deployed fuckinweather-mcp service to ensure reliability and performance.

## Overview

We'll set up:
- Application Insights for performance monitoring
- Custom metrics and dashboards
- Alert rules for critical events
- Log queries for troubleshooting
- Automated health checks

## 1. Application Insights Configuration

Application Insights is automatically configured during deployment, but let's enhance it with custom telemetry.

### Add Application Insights NuGet Package

```bash
cd src/FuknWeather.Api
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

### Configure in Program.cs

```csharp
using Microsoft.ApplicationInsights.Extensibility;

var builder = WebApplication.CreateBuilder(args);

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    options.EnableAdaptiveSampling = true;
    options.EnableQuickPulseMetricStream = true;
});

// Configure telemetry
builder.Services.Configure<TelemetryConfiguration>(config =>
{
    config.TelemetryInitializers.Add(new CloudRoleNameInitializer());
});

// Rest of your configuration...
```

### Create Custom Telemetry Initializer

Create `src/FuknWeather.Api/Telemetry/CloudRoleNameInitializer.cs`:

```csharp
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace FuknWeather.Api.Telemetry;

public class CloudRoleNameInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = "FuknWeather.Api";
        telemetry.Context.Cloud.RoleInstance = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") 
            ?? Environment.MachineName;
    }
}
```

## 2. Custom Metrics

Track custom metrics for business insights.

### Create Metrics Service

Create `src/FuknWeather.Api/Services/MetricsService.cs`:

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace FuknWeather.Api.Services;

public interface IMetricsService
{
    void TrackWeatherRequest(string zipCode, bool success, double responseTimeMs);
    void TrackMcpToolCall(string toolName, bool success);
    void TrackExternalApiCall(string apiName, bool success, double responseTimeMs);
}

public class MetricsService : IMetricsService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<MetricsService> _logger;

    public MetricsService(TelemetryClient telemetryClient, ILogger<MetricsService> logger)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    public void TrackWeatherRequest(string zipCode, bool success, double responseTimeMs)
    {
        var properties = new Dictionary<string, string>
        {
            { "ZipCode", zipCode },
            { "Success", success.ToString() }
        };

        var metrics = new Dictionary<string, double>
        {
            { "ResponseTimeMs", responseTimeMs }
        };

        _telemetryClient.TrackEvent("WeatherRequest", properties, metrics);
        
        _telemetryClient.GetMetric("WeatherRequest.ResponseTime").TrackValue(responseTimeMs);
        _telemetryClient.GetMetric("WeatherRequest.Count", "Success").TrackValue(1, success.ToString());
    }

    public void TrackMcpToolCall(string toolName, bool success)
    {
        var properties = new Dictionary<string, string>
        {
            { "ToolName", toolName },
            { "Success", success.ToString() }
        };

        _telemetryClient.TrackEvent("McpToolCall", properties);
        _telemetryClient.GetMetric("McpToolCall.Count", "ToolName", "Success")
            .TrackValue(1, toolName, success.ToString());
    }

    public void TrackExternalApiCall(string apiName, bool success, double responseTimeMs)
    {
        var dependency = new DependencyTelemetry
        {
            Name = apiName,
            Type = "HTTP",
            Success = success,
            Duration = TimeSpan.FromMilliseconds(responseTimeMs)
        };

        _telemetryClient.TrackDependency(dependency);
    }
}
```

### Register in Program.cs

```csharp
builder.Services.AddSingleton<IMetricsService, MetricsService>();
```

### Use in Controller

```csharp
[ApiController]
[Route("api/weather")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;
    private readonly IMetricsService _metricsService;

    public WeatherController(IWeatherService weatherService, IMetricsService metricsService)
    {
        _weatherService = weatherService;
        _metricsService = metricsService;
    }

    [HttpGet("{zipCode}")]
    public async Task<IActionResult> GetWeather(string zipCode)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var weather = await _weatherService.GetWeatherByZipCodeAsync(zipCode);
            stopwatch.Stop();
            
            _metricsService.TrackWeatherRequest(zipCode, true, stopwatch.ElapsedMilliseconds);
            
            return Ok(weather);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metricsService.TrackWeatherRequest(zipCode, false, stopwatch.ElapsedMilliseconds);
            
            return StatusCode(500, new { error = "Failed to fetch weather data" });
        }
    }
}
```

## 3. Azure Monitor Alert Rules

### Create Alerts Using Azure CLI

```bash
# Get resource IDs
RESOURCE_GROUP="rg-fukn-weather-prod"
APP_NAME="fukn-weather-api"
APP_INSIGHTS_NAME="appi-fukn-weather-prod"

# Get App Service resource ID
APP_SERVICE_ID=$(az webapp show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query id --output tsv)

# Get Application Insights resource ID
APP_INSIGHTS_ID=$(az monitor app-insights component show \
  --app $APP_INSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query id --output tsv)

# Create action group for notifications
az monitor action-group create \
  --name "fukn-weather-alerts" \
  --resource-group $RESOURCE_GROUP \
  --short-name "fwAlerts" \
  --email-receiver name="Admin" email="admin@example.com"

# Get action group ID
ACTION_GROUP_ID=$(az monitor action-group show \
  --name "fukn-weather-alerts" \
  --resource-group $RESOURCE_GROUP \
  --query id --output tsv)

# Alert: High error rate (5xx errors)
az monitor metrics alert create \
  --name "High Error Rate" \
  --resource-group $RESOURCE_GROUP \
  --scopes $APP_SERVICE_ID \
  --condition "avg Http5xx > 10" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action $ACTION_GROUP_ID \
  --description "Alert when 5xx errors exceed 10 per 5 minutes"

# Alert: High response time
az monitor metrics alert create \
  --name "High Response Time" \
  --resource-group $RESOURCE_GROUP \
  --scopes $APP_SERVICE_ID \
  --condition "avg AverageResponseTime > 2000" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action $ACTION_GROUP_ID \
  --description "Alert when average response time exceeds 2 seconds"

# Alert: High CPU usage
az monitor metrics alert create \
  --name "High CPU Usage" \
  --resource-group $RESOURCE_GROUP \
  --scopes $APP_SERVICE_ID \
  --condition "avg CpuPercentage > 80" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action $ACTION_GROUP_ID \
  --description "Alert when CPU usage exceeds 80%"

# Alert: High memory usage
az monitor metrics alert create \
  --name "High Memory Usage" \
  --resource-group $RESOURCE_GROUP \
  --scopes $APP_SERVICE_ID \
  --condition "avg MemoryPercentage > 85" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action $ACTION_GROUP_ID \
  --description "Alert when memory usage exceeds 85%"

# Alert: App is down
az monitor metrics alert create \
  --name "App Service Down" \
  --resource-group $RESOURCE_GROUP \
  --scopes $APP_SERVICE_ID \
  --condition "avg Http2xx == 0" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action $ACTION_GROUP_ID \
  --severity 0 \
  --description "Critical: App Service is not responding"
```

### Create Custom Log Alerts

```bash
# Alert: Frequent Weather API failures
az monitor scheduled-query create \
  --name "Weather API Failures" \
  --resource-group $RESOURCE_GROUP \
  --scopes $APP_INSIGHTS_ID \
  --condition "count 'dependencies | where name contains \"openweathermap\" and success == false' > 10" \
  --window-size 5m \
  --evaluation-frequency 5m \
  --action $ACTION_GROUP_ID \
  --description "Alert when external Weather API calls fail frequently"

# Alert: Unusual number of requests
az monitor scheduled-query create \
  --name "High Request Volume" \
  --resource-group $RESOURCE_GROUP \
  --scopes $APP_INSIGHTS_ID \
  --condition "count 'requests' > 1000" \
  --window-size 5m \
  --evaluation-frequency 5m \
  --action $ACTION_GROUP_ID \
  --description "Alert on unusually high request volume"
```

## 4. Useful Log Queries

### View in Application Insights

Navigate to Application Insights → Logs and run these Kusto queries:

#### Performance Overview
```kusto
requests
| where timestamp > ago(1h)
| summarize 
    RequestCount = count(),
    AvgDuration = avg(duration),
    P95Duration = percentile(duration, 95),
    SuccessRate = 100.0 * countif(success) / count()
by bin(timestamp, 5m)
| render timechart
```

#### Error Analysis
```kusto
exceptions
| where timestamp > ago(24h)
| summarize Count = count() by outerMessage, type
| order by Count desc
| take 10
```

#### External API Dependencies
```kusto
dependencies
| where timestamp > ago(1h)
| where name contains "openweathermap"
| summarize 
    CallCount = count(),
    AvgDuration = avg(duration),
    SuccessRate = 100.0 * countif(success) / count()
by bin(timestamp, 5m)
| render timechart
```

#### Top ZIP Codes Requested
```kusto
customEvents
| where name == "WeatherRequest"
| where timestamp > ago(24h)
| extend ZipCode = tostring(customDimensions.ZipCode)
| summarize Count = count() by ZipCode
| order by Count desc
| take 20
```

#### Failed Requests by Status Code
```kusto
requests
| where timestamp > ago(24h)
| where success == false
| summarize Count = count() by resultCode
| render piechart
```

## 5. Create Custom Dashboard

Save this as `dashboard.json`:

```json
{
  "properties": {
    "lenses": [
      {
        "order": 0,
        "parts": [
          {
            "position": { "x": 0, "y": 0, "colSpan": 6, "rowSpan": 4 },
            "metadata": {
              "type": "Extension/AppInsightsExtension/PartType/MetricsChartPart",
              "settings": {
                "content": {
                  "metrics": [
                    {
                      "resourceId": "/subscriptions/{subscription-id}/resourceGroups/rg-fukn-weather-prod/providers/Microsoft.Web/sites/fukn-weather-api",
                      "name": "Requests"
                    }
                  ]
                }
              }
            }
          }
        ]
      }
    ]
  }
}
```

Create dashboard using Azure CLI:

```bash
az portal dashboard create \
  --resource-group $RESOURCE_GROUP \
  --name "fukn-weather-dashboard" \
  --input-path dashboard.json \
  --location $LOCATION
```

## 6. Health Checks

### Add Health Check Endpoint

Create `src/FuknWeather.Api/HealthChecks/WeatherApiHealthCheck.cs`:

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FuknWeather.Api.HealthChecks;

public class WeatherApiHealthCheck : IHealthCheck
{
    private readonly IWeatherService _weatherService;
    private readonly ILogger<WeatherApiHealthCheck> _logger;

    public WeatherApiHealthCheck(IWeatherService weatherService, ILogger<WeatherApiHealthCheck> logger)
    {
        _weatherService = weatherService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test with a known valid zip code
            await _weatherService.GetWeatherByZipCodeAsync("10001");
            return HealthCheckResult.Healthy("Weather API is responding");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Weather API health check failed");
            return HealthCheckResult.Unhealthy("Weather API is not responding", ex);
        }
    }
}
```

### Register in Program.cs

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<WeatherApiHealthCheck>("weather-api");

// Add health check UI (optional)
builder.Services.AddHealthChecksUI()
    .AddInMemoryStorage();

// In middleware pipeline
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecksUI(options => options.UIPath = "/health-ui");
```

### Configure in Azure

```bash
az webapp config set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --health-check-path "/health"
```

## 7. Availability Tests

Create an availability test to monitor your service from multiple regions:

```bash
# Create availability test
az monitor app-insights web-test create \
  --resource-group $RESOURCE_GROUP \
  --name "fukn-weather-availability" \
  --location eastus \
  --kind ping \
  --web-test-name "Weather API Availability" \
  --frequency 300 \
  --timeout 120 \
  --enabled true \
  --locations "us-east-1,us-west-1,eu-west-1" \
  --synthetic-monitor-id "fukn-weather-avail" \
  --web-test "<WebTest><Items><Request Url=\"https://$APP_NAME.azurewebsites.net/health\" /></Items></WebTest>"
```

## 8. Automated Response

### Auto-restart on Failures

Create an Azure Automation runbook:

```powershell
# auto-restart-app.ps1
param(
    [string]$ResourceGroupName,
    [string]$WebAppName
)

# Connect using managed identity
Connect-AzAccount -Identity

# Restart the app
Restart-AzWebApp -ResourceGroupName $ResourceGroupName -Name $WebAppName

Write-Output "Restarted $WebAppName at $(Get-Date)"
```

### Configure Auto-healing

```bash
az webapp config set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --auto-heal-enabled true
```

## 9. Monitoring Checklist

After deployment, verify:

- [ ] Application Insights is receiving telemetry
- [ ] Custom metrics are being tracked
- [ ] Alert rules are configured
- [ ] Action groups receive notifications
- [ ] Health checks are passing
- [ ] Availability tests are running
- [ ] Dashboard displays metrics
- [ ] Log queries return data

## 10. Useful Commands

### Stream Live Metrics

```bash
# View live metrics in Application Insights
az monitor app-insights component show \
  --app $APP_INSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "properties.InstrumentationKey"

# Open Live Metrics in browser
# https://portal.azure.com → Application Insights → Live Metrics
```

### Export Logs

```bash
# Export logs for the last hour
az monitor app-insights query \
  --app $APP_INSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP \
  --analytics-query "requests | where timestamp > ago(1h)" \
  --output json > requests.json
```

### Check Alert Status

```bash
# List all alerts
az monitor metrics alert list \
  --resource-group $RESOURCE_GROUP

# Get alert details
az monitor metrics alert show \
  --name "High Error Rate" \
  --resource-group $RESOURCE_GROUP
```

## Summary

You now have comprehensive monitoring with:

- ✅ Application Insights telemetry
- ✅ Custom metrics and events
- ✅ Alert rules for critical issues
- ✅ Log queries for troubleshooting
- ✅ Health checks and availability tests
- ✅ Custom dashboard
- ✅ Automated notifications

## Next Steps

1. Set up a dedicated operations dashboard
2. Configure alert notification channels (email, SMS, Teams, Slack)
3. Implement distributed tracing for complex scenarios
4. Set up log archival for compliance
5. Create runbooks for common issues

## Resources

- [Application Insights Documentation](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Azure Monitor Alerts](https://docs.microsoft.com/azure/azure-monitor/alerts/alerts-overview)
- [Kusto Query Language](https://docs.microsoft.com/azure/data-explorer/kusto/query/)
- [Health Checks in ASP.NET Core](https://docs.microsoft.com/aspnet/core/host-and-deploy/health-checks)
