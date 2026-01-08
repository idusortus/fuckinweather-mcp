# Azure Deployment Quick Start Guide

This is a condensed quick start guide for deploying fuckinweather-mcp to Azure. For comprehensive documentation, see [AZURE_DEPLOYMENT.md](AZURE_DEPLOYMENT.md).

## Prerequisites

1. [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) installed
2. [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) installed
3. [OpenWeatherMap API key](https://openweathermap.org/api)
4. Active Azure subscription

## Quick Deploy (5 minutes)

### Step 1: Clone and Setup

```bash
git clone https://github.com/idusortus/fuckinweather-mcp.git
cd fuckinweather-mcp
export WEATHER_API_KEY="your_openweathermap_api_key_here"
```

### Step 2: Login to Azure

```bash
az login
az account set --subscription "Your-Subscription-Name"
```

### Step 3: Deploy Using Script

```bash
# Deploy to development environment
./scripts/deploy-azure.sh dev

# Or deploy to production
./scripts/deploy-azure.sh prod rg-fukn-weather-prod eastus
```

The script will:
- Create Azure resource group
- Deploy infrastructure (App Service, Key Vault, Application Insights)
- Build and publish the .NET application
- Deploy to Azure App Service
- Test the deployment

### Step 4: Test Your Deployment

```bash
# The script will output your app URL, test it with:
curl https://your-app-name.azurewebsites.net/api/weather/10001
```

Expected response:
```json
{
  "zipCode": "10001",
  "temperatureFahrenheit": 45.2,
  "description": "Pretty damn chilly. Jacket up, buttercup!",
  "location": "New York"
}
```

## Alternative: Deploy Using Bicep Directly

```bash
# Create resource group
az group create --name rg-fukn-weather-dev --location eastus

# Deploy infrastructure
az deployment group create \
  --resource-group rg-fukn-weather-dev \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/parameters.json \
  --parameters weatherApiKey="$WEATHER_API_KEY"

# Get app service name
APP_NAME=$(az deployment group show \
  --resource-group rg-fukn-weather-dev \
  --name main \
  --query properties.outputs.appServiceName.value -o tsv)

# Build and deploy application
dotnet publish src/FuknWeather.Api/FuknWeather.Api.csproj -c Release -o ./publish
cd publish && zip -r ../deploy.zip . && cd ..

az webapp deploy \
  --resource-group rg-fukn-weather-dev \
  --name $APP_NAME \
  --src-path deploy.zip \
  --type zip
```

## Using GitHub Actions (CI/CD)

### Step 1: Setup Azure Service Principal

```bash
az ad sp create-for-rbac \
  --name "github-actions-fukn-weather" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/rg-fukn-weather-dev \
  --sdk-auth
```

Save the JSON output.

### Step 2: Configure GitHub Secrets

Add these secrets to your GitHub repository (Settings → Secrets → Actions):

- `AZURE_CREDENTIALS` - The JSON output from Step 1
- `WEATHER_API_KEY` - Your OpenWeatherMap API key

### Step 3: Push to Main Branch

```bash
git push origin main
```

GitHub Actions will automatically deploy your application!

## Deployment Options Comparison

| Method | Deployment Time | Complexity | Best For |
|--------|----------------|------------|----------|
| **Bash Script** | 5-7 minutes | Low | Quick deployments, local development |
| **PowerShell Script** | 5-7 minutes | Low | Windows users |
| **Bicep Template** | 5-10 minutes | Medium | Infrastructure versioning, repeatability |
| **GitHub Actions** | 8-12 minutes | Medium | Automated CI/CD, team environments |

## What Gets Deployed?

### Azure Resources Created

1. **App Service Plan** - Compute resources for your web app
2. **App Service (Web App)** - Hosts your .NET 10 API
3. **Key Vault** - Securely stores your API key
4. **Application Insights** - Monitors application performance
5. **Log Analytics Workspace** - Stores logs and telemetry

### Estimated Costs

| Environment | Monthly Cost |
|-------------|--------------|
| Development (B1 SKU) | ~$15-20 |
| Production (S1 SKU) | ~$80-90 |

## Post-Deployment

### View Your Application

```bash
# Open in browser (macOS)
open https://your-app-name.azurewebsites.net/api/weather/10001

# Open in browser (Windows)
start https://your-app-name.azurewebsites.net/api/weather/10001

# Open in browser (Linux)
xdg-open https://your-app-name.azurewebsites.net/api/weather/10001
```

### Monitor Your Application

```bash
# Stream live logs
az webapp log tail \
  --name your-app-name \
  --resource-group rg-fukn-weather-dev

# View Application Insights
az monitor app-insights component show \
  --app appi-fukn-weather-dev \
  --resource-group rg-fukn-weather-dev
```

### Update Your Deployment

```bash
# Make code changes, then redeploy
./scripts/deploy-azure.sh dev

# Or just redeploy application (skip infrastructure)
dotnet publish src/FuknWeather.Api/FuknWeather.Api.csproj -c Release -o ./publish
cd publish && zip -r ../deploy.zip . && cd ..
az webapp deploy \
  --resource-group rg-fukn-weather-dev \
  --name your-app-name \
  --src-path deploy.zip
```

## Client Integration

Once deployed, share your MCP service with external users:

### For Claude Desktop Users

Add to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "fukn-weather": {
      "url": "https://your-app-name.azurewebsites.net/api/weather/mcp"
    }
  }
}
```

### For API Consumers

```bash
# Get weather
curl https://your-app-name.azurewebsites.net/api/weather/90210

# List available MCP tools
curl -X POST https://your-app-name.azurewebsites.net/api/weather/mcp/tools

# Call MCP tool
curl -X POST https://your-app-name.azurewebsites.net/api/weather/mcp/call \
  -H "Content-Type: application/json" \
  -d '{"toolName": "get_fukn_weather", "arguments": {"zipCode": "90210"}}'
```

## Troubleshooting

### Deployment Script Fails

1. **Check prerequisites**: Ensure Azure CLI and .NET SDK are installed
2. **Verify login**: Run `az account show` to confirm you're logged in
3. **Check API key**: Ensure `WEATHER_API_KEY` environment variable is set
4. **Review logs**: Check the script output for specific error messages

### Application Not Responding

```bash
# Check app service status
az webapp show \
  --name your-app-name \
  --resource-group rg-fukn-weather-dev \
  --query state

# Restart app service
az webapp restart \
  --name your-app-name \
  --resource-group rg-fukn-weather-dev

# Download logs
az webapp log download \
  --name your-app-name \
  --resource-group rg-fukn-weather-dev
```

### 502 Bad Gateway Error

This usually means the app failed to start. Check:

1. **Environment variables**: Verify `WeatherApi__ApiKey` is set correctly
2. **Application logs**: Stream logs to see startup errors
3. **Key Vault access**: Ensure managed identity has Key Vault permissions

```bash
# Check app settings
az webapp config appsettings list \
  --name your-app-name \
  --resource-group rg-fukn-weather-dev
```

## Cleanup

To delete all deployed resources:

```bash
# Delete entire resource group (this removes all resources)
az group delete \
  --name rg-fukn-weather-dev \
  --yes --no-wait

# Verify deletion
az group exists --name rg-fukn-weather-dev
```

**Warning**: This permanently deletes all resources in the resource group!

## Next Steps

1. ✅ **Add Authentication** - Implement API key auth for external users
2. ✅ **Setup Custom Domain** - Use your own domain name
3. ✅ **Enable WAF** - Add Web Application Firewall for security
4. ✅ **Configure Alerts** - Get notified of issues
5. ✅ **Add Rate Limiting** - Prevent abuse

See [AZURE_DEPLOYMENT.md](AZURE_DEPLOYMENT.md) for detailed instructions on these advanced topics.

## Additional Resources

- [Full Azure Deployment Guide](AZURE_DEPLOYMENT.md)
- [Infrastructure README](infrastructure/README.md)
- [GitHub Actions Workflow](.github/workflows/azure-deploy.yml)
- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [MCP Protocol Specification](https://modelcontextprotocol.io/)

## Getting Help

- **Issues**: [GitHub Issues](https://github.com/idusortus/fuckinweather-mcp/issues)
- **Azure Support**: [Azure Portal](https://portal.azure.com)
- **Community**: Check existing issues and discussions

---

**Happy Deploying!** 🚀
