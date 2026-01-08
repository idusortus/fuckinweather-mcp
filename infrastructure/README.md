# Azure Infrastructure as Code

This directory contains Bicep templates for deploying the fuckinweather-mcp application to Microsoft Azure.

## Overview

The Bicep templates provide Infrastructure as Code (IaC) for deploying:

- Azure App Service (Web App)
- App Service Plan
- Azure Key Vault (for secrets)
- Application Insights (monitoring)
- Log Analytics Workspace
- Auto-scaling configuration

## Prerequisites

1. **Azure CLI** - [Install](https://docs.microsoft.com/cli/azure/install-azure-cli)
2. **Azure Subscription** - An active Azure subscription
3. **Bicep CLI** - Usually comes with Azure CLI, or [install separately](https://learn.microsoft.com/azure/azure-resource-manager/bicep/install)
4. **OpenWeatherMap API Key** - Get from [OpenWeatherMap](https://openweathermap.org/api)

## Files

- `main.bicep` - Main Bicep template defining all Azure resources
- `parameters.json` - Development environment parameters
- `parameters.prod.json` - Production environment parameters
- `README.md` - This file

## Quick Start

### 1. Login to Azure

```bash
az login
az account set --subscription "Your-Subscription-Name"
```

### 2. Create Resource Group

```bash
az group create \
  --name rg-fukn-weather-dev \
  --location eastus
```

### 3. Update Parameters

Edit `parameters.json` and replace `YOUR_OPENWEATHERMAP_API_KEY_HERE` with your actual API key:

```json
{
  "parameters": {
    "weatherApiKey": {
      "value": "your_actual_api_key_here"
    }
  }
}
```

**Important:** Do not commit your API key to source control. Use environment variables or Azure DevOps/GitHub secrets for production deployments.

### 4. Deploy Infrastructure

#### Development Environment

```bash
az deployment group create \
  --resource-group rg-fukn-weather-dev \
  --template-file main.bicep \
  --parameters parameters.json
```

#### Production Environment

```bash
# Create production resource group
az group create \
  --name rg-fukn-weather-prod \
  --location eastus

# Deploy with production parameters
az deployment group create \
  --resource-group rg-fukn-weather-prod \
  --template-file main.bicep \
  --parameters parameters.prod.json
```

### 5. Deploy Application Code

After infrastructure is deployed, deploy the application:

```bash
# Get the app service name from deployment output
APP_NAME=$(az deployment group show \
  --resource-group rg-fukn-weather-dev \
  --name main \
  --query properties.outputs.appServiceName.value \
  --output tsv)

# Build and publish
cd ../src/FuknWeather.Api
dotnet publish -c Release -o ../../publish

# Deploy
cd ../../publish
zip -r ../deploy.zip .
cd ..

az webapp deploy \
  --resource-group rg-fukn-weather-dev \
  --name $APP_NAME \
  --src-path deploy.zip \
  --type zip
```

## Parameters

### Required Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `weatherApiKey` | securestring | OpenWeatherMap API key |

### Optional Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `environment` | string | `dev` | Environment name (dev, staging, prod) |
| `location` | string | Resource group location | Azure region |
| `weatherApiBaseUrl` | string | `https://api.openweathermap.org/data/2.5` | Weather API base URL |
| `appServiceSku` | string | `B1` | App Service pricing tier |
| `enableApplicationInsights` | bool | `true` | Enable Application Insights monitoring |
| `enableKeyVault` | bool | `true` | Enable Azure Key Vault for secrets |
| `minInstances` | int | `1` | Minimum number of instances for auto-scale |
| `maxInstances` | int | `5` | Maximum number of instances for auto-scale |

## App Service SKU Options

| SKU | Description | Use Case | Approx. Monthly Cost |
|-----|-------------|----------|---------------------|
| B1 | Basic | Development/Testing | $13 |
| B2 | Basic | Small production | $26 |
| S1 | Standard | Production | $70 |
| S2 | Standard | Production with higher load | $140 |
| P1V2 | Premium | Production with advanced features | $85 |
| P2V2 | Premium | High-performance production | $170 |

## Outputs

The deployment provides these outputs:

| Output | Description |
|--------|-------------|
| `appServiceName` | Name of the created App Service |
| `appServiceUrl` | HTTPS URL of the deployed application |
| `appServicePrincipalId` | Managed Identity Principal ID |
| `keyVaultName` | Name of the Key Vault (if enabled) |
| `appInsightsName` | Name of Application Insights (if enabled) |
| `appInsightsInstrumentationKey` | Application Insights key |
| `appInsightsConnectionString` | Application Insights connection string |

### View Deployment Outputs

```bash
az deployment group show \
  --resource-group rg-fukn-weather-dev \
  --name main \
  --query properties.outputs
```

## Using with CI/CD

### Passing API Key Securely

Instead of storing the API key in parameters file:

```bash
az deployment group create \
  --resource-group rg-fukn-weather-dev \
  --template-file main.bicep \
  --parameters parameters.json \
  --parameters weatherApiKey="$WEATHER_API_KEY"
```

### GitHub Actions Example

```yaml
- name: Deploy Infrastructure
  run: |
    az deployment group create \
      --resource-group ${{ secrets.AZURE_RESOURCE_GROUP }} \
      --template-file infrastructure/main.bicep \
      --parameters infrastructure/parameters.json \
      --parameters weatherApiKey="${{ secrets.WEATHER_API_KEY }}"
```

### Azure DevOps Pipeline Example

```yaml
- task: AzureCLI@2
  inputs:
    azureSubscription: 'Azure-Connection'
    scriptType: 'bash'
    scriptLocation: 'inlineScript'
    inlineScript: |
      az deployment group create \
        --resource-group $(resourceGroup) \
        --template-file infrastructure/main.bicep \
        --parameters infrastructure/parameters.json \
        --parameters weatherApiKey="$(weatherApiKey)"
```

## Validation

### Validate Template Before Deployment

```bash
az deployment group validate \
  --resource-group rg-fukn-weather-dev \
  --template-file main.bicep \
  --parameters parameters.json \
  --parameters weatherApiKey="test-key"
```

### What-If Deployment

Preview changes before deploying:

```bash
az deployment group what-if \
  --resource-group rg-fukn-weather-dev \
  --template-file main.bicep \
  --parameters parameters.json \
  --parameters weatherApiKey="$WEATHER_API_KEY"
```

## Updating Infrastructure

To update existing infrastructure:

```bash
az deployment group create \
  --resource-group rg-fukn-weather-dev \
  --template-file main.bicep \
  --parameters parameters.json \
  --parameters weatherApiKey="$WEATHER_API_KEY" \
  --mode Incremental
```

## Cleanup

To delete all resources:

```bash
# Delete resource group (this deletes all resources within it)
az group delete \
  --name rg-fukn-weather-dev \
  --yes --no-wait
```

## Customization

### Adding a Custom Domain

1. Add your domain to the App Service:

```bash
az webapp config hostname add \
  --webapp-name $APP_NAME \
  --resource-group rg-fukn-weather-dev \
  --hostname yourdomain.com
```

2. Configure SSL:

```bash
az webapp config ssl bind \
  --name $APP_NAME \
  --resource-group rg-fukn-weather-dev \
  --certificate-thumbprint $THUMBPRINT \
  --ssl-type SNI
```

### Enabling Diagnostic Logs

Add to the `main.bicep` after the `appService` resource:

```bicep
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'diag-${appName}'
  scope: appService
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        category: 'AppServiceHTTPLogs'
        enabled: true
      }
      {
        category: 'AppServiceConsoleLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}
```

### Adding Application Gateway with WAF

For production environments requiring WAF protection, consider adding Application Gateway. See the [Azure documentation](https://learn.microsoft.com/azure/application-gateway/quick-create-bicep) for Application Gateway Bicep templates.

## Monitoring

After deployment, monitor your application:

1. **Application Insights Dashboard**:
   ```bash
   az monitor app-insights component show \
     --app appi-fukn-weather-dev \
     --resource-group rg-fukn-weather-dev
   ```

2. **View Logs**:
   ```bash
   az webapp log tail \
     --name $APP_NAME \
     --resource-group rg-fukn-weather-dev
   ```

3. **Check Health**:
   ```bash
   curl https://$APP_NAME.azurewebsites.net/api/weather/health
   ```

## Troubleshooting

### Deployment Fails

1. Check template validation:
   ```bash
   az deployment group validate \
     --resource-group rg-fukn-weather-dev \
     --template-file main.bicep \
     --parameters parameters.json
   ```

2. View deployment logs:
   ```bash
   az deployment operation group list \
     --resource-group rg-fukn-weather-dev \
     --name main
   ```

### App Service Not Starting

1. Check application logs:
   ```bash
   az webapp log download \
     --resource-group rg-fukn-weather-dev \
     --name $APP_NAME
   ```

2. Verify environment variables:
   ```bash
   az webapp config appsettings list \
     --name $APP_NAME \
     --resource-group rg-fukn-weather-dev
   ```

### Key Vault Access Issues

Check access policies:
```bash
az keyvault show \
  --name $KV_NAME \
  --resource-group rg-fukn-weather-dev \
  --query properties.accessPolicies
```

## Best Practices

1. **Never commit secrets** - Use environment variables or Azure DevOps/GitHub secrets
2. **Use separate resource groups** for different environments
3. **Enable soft delete** on Key Vault (enabled by default in template)
4. **Use managed identities** instead of connection strings when possible
5. **Tag resources** for cost tracking and organization
6. **Enable diagnostic logs** for production environments
7. **Use App Service slots** for zero-downtime deployments
8. **Implement backup strategy** for production data

## Additional Resources

- [Bicep Documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [Azure App Service Documentation](https://learn.microsoft.com/azure/app-service/)
- [Azure Key Vault Documentation](https://learn.microsoft.com/azure/key-vault/)
- [Application Insights Documentation](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)

## Support

For issues related to:
- **Infrastructure templates**: Open an issue in the GitHub repository
- **Azure services**: Use [Azure Support](https://azure.microsoft.com/support/)
- **Bicep**: See [Bicep GitHub](https://github.com/Azure/bicep)
