// Main Bicep template for fuckinweather-mcp Azure deployment
// This template deploys all required Azure resources for the MCP service

@description('Environment name (dev, staging, prod)')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environment string = 'dev'

@description('Location for all resources')
param location string = resourceGroup().location

@description('OpenWeatherMap API key')
@secure()
param weatherApiKey string

@description('Weather API base URL')
param weatherApiBaseUrl string = 'https://api.openweathermap.org/data/2.5'

@description('App Service SKU')
@allowed([
  'B1'
  'B2'
  'B3'
  'S1'
  'S2'
  'S3'
  'P1V2'
  'P2V2'
  'P3V2'
])
param appServiceSku string = 'B1'

@description('Enable Application Insights')
param enableApplicationInsights bool = true

@description('Enable Key Vault')
param enableKeyVault bool = true

@description('Minimum number of instances')
param minInstances int = 1

@description('Maximum number of instances')
param maxInstances int = 5

// Variables
var appName = 'fukn-weather-${environment}'
var appServicePlanName = 'plan-${appName}'
var appServiceName = 'app-${appName}-${uniqueString(resourceGroup().id)}'
var keyVaultName = 'kv-${appName}-${uniqueString(resourceGroup().id)}'
var appInsightsName = 'appi-${appName}'
var logAnalyticsName = 'log-${appName}'

// Log Analytics Workspace
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = if (enableApplicationInsights) {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = if (enableApplicationInsights) {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: enableApplicationInsights ? logAnalytics.id : null
  }
}

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = if (enableKeyVault) {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enableRbacAuthorization: false
    accessPolicies: []
  }
}

// Store Weather API Key in Key Vault
resource weatherApiKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (enableKeyVault) {
  parent: keyVault
  name: 'WeatherApiKey'
  properties: {
    value: weatherApiKey
  }
}

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: appServiceSku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// App Service (Web App)
resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: appServiceName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET|10.0'
      alwaysOn: appServiceSku != 'B1'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      http20Enabled: true
      healthCheckPath: '/api/weather/health'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environment == 'prod' ? 'Production' : 'Development'
        }
        {
          name: 'WeatherApi__BaseUrl'
          value: weatherApiBaseUrl
        }
        {
          name: 'WeatherApi__ApiKey'
          value: enableKeyVault ? '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=WeatherApiKey)' : weatherApiKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: enableApplicationInsights ? appInsights.properties.ConnectionString : ''
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'recommended'
        }
      ]
    }
  }
}

// Grant App Service access to Key Vault
resource keyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = if (enableKeyVault) {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: appService.identity.principalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
    ]
  }
}

// Auto-scaling configuration
resource autoScaleSettings 'Microsoft.Insights/autoscalesettings@2022-10-01' = {
  name: 'autoscale-${appName}'
  location: location
  properties: {
    enabled: true
    targetResourceUri: appServicePlan.id
    profiles: [
      {
        name: 'Auto scale condition'
        capacity: {
          minimum: string(minInstances)
          maximum: string(maxInstances)
          default: string(minInstances)
        }
        rules: [
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: appServicePlan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT5M'
              timeAggregation: 'Average'
              operator: 'GreaterThan'
              threshold: 70
            }
            scaleAction: {
              direction: 'Increase'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT5M'
            }
          }
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: appServicePlan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT5M'
              timeAggregation: 'Average'
              operator: 'LessThan'
              threshold: 30
            }
            scaleAction: {
              direction: 'Decrease'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT5M'
            }
          }
        ]
      }
    ]
  }
}

// Outputs
output appServiceName string = appService.name
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output appServicePrincipalId string = appService.identity.principalId
output keyVaultName string = enableKeyVault ? keyVault.name : ''
output appInsightsName string = enableApplicationInsights ? appInsights.name : ''
output appInsightsInstrumentationKey string = enableApplicationInsights ? appInsights.properties.InstrumentationKey : ''
output appInsightsConnectionString string = enableApplicationInsights ? appInsights.properties.ConnectionString : ''
