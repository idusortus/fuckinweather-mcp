// Bicep template for Azure Container Apps deployment
// Alternative to App Service for better scalability and cost optimization

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

@description('Container image')
param containerImage string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

@description('Container registry server (optional)')
param containerRegistryServer string = ''

@description('Container registry username (optional)')
param containerRegistryUsername string = ''

@description('Container registry password (optional)')
@secure()
param containerRegistryPassword string = ''

@description('Minimum number of replicas')
param minReplicas int = 1

@description('Maximum number of replicas')
param maxReplicas int = 10

@description('CPU cores per replica')
param cpuCores string = '0.5'

@description('Memory per replica in Gi')
param memorySize string = '1.0Gi'

// Variables
var appName = 'fukn-weather-${environment}'
var containerAppName = 'ca-${appName}-${uniqueString(resourceGroup().id)}'
var containerAppEnvName = 'cae-${appName}'
var keyVaultName = 'kv-${appName}-${uniqueString(resourceGroup().id)}'
var appInsightsName = 'appi-${appName}'
var logAnalyticsName = 'log-${appName}'
var containerRegistryName = 'acr${replace(appName, '-', '')}${uniqueString(resourceGroup().id)}'

// Log Analytics Workspace
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
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
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
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
resource weatherApiKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'WeatherApiKey'
  properties: {
    value: weatherApiKey
  }
}

// Container Registry (optional - create if needed)
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = if (empty(containerRegistryServer)) {
  name: containerRegistryName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

// Container Apps Environment
resource containerAppEnv 'Microsoft.App/managedEnvironments@2023-11-02-preview' = {
  name: containerAppEnvName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// Container App
resource containerApp 'Microsoft.App/containerApps@2023-11-02-preview' = {
  name: containerAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 80
        transport: 'auto'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: empty(containerRegistryServer) ? [
        {
          server: containerRegistry.properties.loginServer
          username: containerRegistry.listCredentials().username
          passwordSecretRef: 'registry-password'
        }
      ] : [
        {
          server: containerRegistryServer
          username: containerRegistryUsername
          passwordSecretRef: 'registry-password'
        }
      ]
      secrets: concat([
        {
          name: 'weather-api-key'
          value: weatherApiKey
        }
        {
          name: 'app-insights-key'
          value: appInsights.properties.InstrumentationKey
        }
      ], empty(containerRegistryServer) ? [
        {
          name: 'registry-password'
          value: containerRegistry.listCredentials().passwords[0].value
        }
      ] : [
        {
          name: 'registry-password'
          value: containerRegistryPassword
        }
      ])
    }
    template: {
      containers: [
        {
          name: 'fukn-weather-api'
          image: empty(containerRegistryServer) ? containerImage : '${containerRegistryServer}/${containerImage}'
          resources: {
            cpu: json(cpuCores)
            memory: memorySize
          }
          env: [
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
              secretRef: 'weather-api-key'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsights.properties.ConnectionString
            }
            {
              name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
              value: '~3'
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

// Grant Container App access to Key Vault
resource keyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: containerApp.identity.principalId
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

// Outputs
output containerAppName string = containerApp.name
output containerAppFqdn string = containerApp.properties.configuration.ingress.fqdn
output containerAppUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output containerAppPrincipalId string = containerApp.identity.principalId
output keyVaultName string = keyVault.name
output appInsightsName string = appInsights.name
output containerRegistryName string = empty(containerRegistryServer) ? containerRegistry.name : containerRegistryServer
output containerRegistryLoginServer string = empty(containerRegistryServer) ? containerRegistry.properties.loginServer : containerRegistryServer
