# Azure Deployment Guide for fuckinweather-mcp

This guide provides a comprehensive overview of deploying the fuckinweather-mcp application to Microsoft Azure as a shared MCP (Model Context Protocol) service for external users.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Azure Services Required](#azure-services-required)
3. [Deployment Options](#deployment-options)
4. [Step-by-Step Deployment](#step-by-step-deployment)
5. [Security Considerations](#security-considerations)
6. [Cost Estimation](#cost-estimation)
7. [Monitoring and Maintenance](#monitoring-and-maintenance)
8. [Scaling Considerations](#scaling-considerations)

## Architecture Overview

The recommended Azure architecture for this MCP service includes:

```
┌─────────────────────────────────────────────────────────────┐
│                     Azure Front Door (Optional)              │
│                  (Global CDN + WAF + SSL)                    │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│                   Azure Application Gateway                  │
│                  (Regional Load Balancer + WAF)              │
└─────────────────────────┬───────────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────────┐
│                  Azure App Service / Container Apps          │
│               (Hosts the .NET 10 MCP API)                    │
│                                                               │
│  ┌─────────────────────────────────────────────────┐        │
│  │  FuknWeather.Api (.NET 10)                      │        │
│  │  - RESTful API Endpoints                        │        │
│  │  - MCP Protocol Implementation                  │        │
│  │  - Weather Service Integration                  │        │
│  └─────────────────────────────────────────────────┘        │
└───────────────────────────┬─────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
┌───────▼────────┐  ┌──────▼──────┐  ┌────────▼────────┐
│  Azure Key     │  │  Application │  │  OpenWeatherMap │
│  Vault         │  │  Insights    │  │  API            │
│  (Secrets)     │  │  (Monitoring)│  │  (External)     │
└────────────────┘  └──────────────┘  └─────────────────┘
```

## Azure Services Required

### Core Services

1. **Azure App Service** (Recommended for simplicity) or **Azure Container Apps** (Recommended for scalability)
   - Hosts the .NET 10 Web API
   - Built-in HTTPS support
   - Easy scaling options
   - Native .NET support

2. **Azure Key Vault**
   - Secure storage for OpenWeatherMap API key
   - Manages application secrets
   - Provides access control and auditing

3. **Azure Application Insights**
   - Application performance monitoring
   - Request tracking and diagnostics
   - Custom telemetry for MCP calls

### Optional Services (for Production/Enterprise)

4. **Azure Front Door** or **Application Gateway**
   - Global load balancing (Front Door)
   - Web Application Firewall (WAF)
   - DDoS protection
   - SSL/TLS termination
   - Rate limiting

5. **Azure API Management**
   - API Gateway functionality
   - Authentication and authorization
   - Rate limiting per client
   - API versioning and documentation
   - Developer portal for external users

6. **Azure CDN**
   - Edge caching for static content
   - Reduces latency globally

7. **Azure Container Registry** (if using containers)
   - Private Docker image storage
   - Security scanning
   - Geo-replication

## Deployment Options

### Option 1: Azure App Service (Recommended for Getting Started)

**Pros:**
- Simplest deployment model
- Managed platform (PaaS)
- Built-in SSL/TLS
- Easy scaling
- Native .NET support
- Free tier available

**Cons:**
- Less control over infrastructure
- Regional availability only

**Best for:** Quick deployment, small to medium scale, cost-effective solution

### Option 2: Azure Container Apps (Recommended for Production)

**Pros:**
- Built on Kubernetes
- Automatic scaling to zero
- Pay-per-use pricing
- Microservices-ready
- Better resource utilization

**Cons:**
- More complex setup
- Requires Docker knowledge

**Best for:** Production workloads, cost optimization, scalability

### Option 3: Azure Kubernetes Service (AKS)

**Pros:**
- Full Kubernetes capabilities
- Maximum control
- Advanced networking
- Multi-region deployments

**Cons:**
- Most complex
- Higher management overhead
- Higher cost

**Best for:** Enterprise deployments, complex requirements, multi-service architectures

## Step-by-Step Deployment

### Prerequisites

Before you begin, ensure you have:

1. **Azure Account** - [Sign up](https://azure.microsoft.com/free/) if you don't have one
2. **Azure CLI** - [Install Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
3. **OpenWeatherMap API Key** - [Get it here](https://openweathermap.org/api)
4. **.NET 10 SDK** - For local testing

### Method 1: Deploy to Azure App Service (Recommended)

#### Step 1: Login to Azure

```bash
az login
az account set --subscription "Your-Subscription-Name"
```

#### Step 2: Create Resource Group

```bash
# Create a resource group
az group create \
  --name rg-fukn-weather-prod \
  --location eastus

# Verify resource group
az group show --name rg-fukn-weather-prod
```

#### Step 3: Create Azure Key Vault and Store Secrets

```bash
# Create Key Vault
az keyvault create \
  --name kv-fukn-weather-prod \
  --resource-group rg-fukn-weather-prod \
  --location eastus

# Store OpenWeatherMap API key
az keyvault secret set \
  --vault-name kv-fukn-weather-prod \
  --name WeatherApiKey \
  --value "your_openweathermap_api_key_here"

# Verify secret was stored
az keyvault secret show \
  --vault-name kv-fukn-weather-prod \
  --name WeatherApiKey
```

#### Step 4: Create App Service Plan

```bash
# Create App Service Plan (Linux, .NET)
az appservice plan create \
  --name plan-fukn-weather-prod \
  --resource-group rg-fukn-weather-prod \
  --location eastus \
  --is-linux \
  --sku B1

# For production, consider Standard or Premium:
# --sku S1  (Standard)
# --sku P1V2 (Premium)
```

#### Step 5: Create Web App

```bash
# Create Web App with .NET 10 runtime
az webapp create \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --plan plan-fukn-weather-prod \
  --runtime "DOTNET|10.0"

# Enable HTTPS only
az webapp update \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --https-only true
```

#### Step 6: Configure App Settings

```bash
# Configure application settings
az webapp config appsettings set \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    WeatherApi__BaseUrl="https://api.openweathermap.org/data/2.5"

# Configure Key Vault reference for API key
# First, enable managed identity
az webapp identity assign \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod

# Get the managed identity principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --query principalId --output tsv)

# Grant Key Vault access to the managed identity
az keyvault set-policy \
  --name kv-fukn-weather-prod \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list

# Configure app to use Key Vault reference
az webapp config appsettings set \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --settings \
    WeatherApi__ApiKey="@Microsoft.KeyVault(VaultName=kv-fukn-weather-prod;SecretName=WeatherApiKey)"
```

#### Step 7: Deploy Application

**Option A: Deploy from Local Build**

```bash
# Navigate to project root
cd /path/to/fuckinweather-mcp

# Publish the application
dotnet publish src/FuknWeather.Api/FuknWeather.Api.csproj \
  -c Release \
  -o ./publish

# Create deployment package
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy to Azure
az webapp deploy \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --src-path deploy.zip \
  --type zip
```

**Option B: Deploy from GitHub (CI/CD)**

```bash
# Configure GitHub deployment
az webapp deployment source config \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --repo-url https://github.com/idusortus/fuckinweather-mcp \
  --branch main \
  --manual-integration
```

#### Step 8: Configure Application Insights

```bash
# Create Application Insights
az monitor app-insights component create \
  --app fukn-weather-insights \
  --location eastus \
  --resource-group rg-fukn-weather-prod \
  --application-type web

# Get instrumentation key
INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app fukn-weather-insights \
  --resource-group rg-fukn-weather-prod \
  --query instrumentationKey --output tsv)

# Configure web app to use Application Insights
az webapp config appsettings set \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --settings \
    APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$INSTRUMENTATION_KEY"
```

#### Step 9: Verify Deployment

```bash
# Get the web app URL
az webapp show \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --query defaultHostName --output tsv

# Test the endpoint
curl https://fukn-weather-api.azurewebsites.net/api/weather/10001
```

### Method 2: Deploy to Azure Container Apps

#### Step 1-3: Same as Method 1 (Login, Resource Group, Key Vault)

#### Step 4: Create Container Apps Environment

```bash
# Install Container Apps extension
az extension add --name containerapp --upgrade

# Create Container Apps environment
az containerapp env create \
  --name fukn-weather-env \
  --resource-group rg-fukn-weather-prod \
  --location eastus
```

#### Step 5: Create Container Registry

```bash
# Create Azure Container Registry
az acr create \
  --name acrfuknweather \
  --resource-group rg-fukn-weather-prod \
  --location eastus \
  --sku Basic \
  --admin-enabled true

# Get ACR credentials
ACR_USERNAME=$(az acr credential show \
  --name acrfuknweather \
  --query username --output tsv)
  
ACR_PASSWORD=$(az acr credential show \
  --name acrfuknweather \
  --query passwords[0].value --output tsv)
```

#### Step 6: Build and Push Docker Image

```bash
# Login to ACR
az acr login --name acrfuknweather

# Build and push image
az acr build \
  --registry acrfuknweather \
  --image fukn-weather-api:latest \
  --file Dockerfile \
  .
```

#### Step 7: Deploy Container App

```bash
# Deploy container app
az containerapp create \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --environment fukn-weather-env \
  --image acrfuknweather.azurecr.io/fukn-weather-api:latest \
  --registry-server acrfuknweather.azurecr.io \
  --registry-username $ACR_USERNAME \
  --registry-password $ACR_PASSWORD \
  --target-port 80 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 10 \
  --cpu 0.5 \
  --memory 1.0Gi \
  --env-vars \
    ASPNETCORE_ENVIRONMENT="Production" \
    WeatherApi__BaseUrl="https://api.openweathermap.org/data/2.5" \
    WeatherApi__ApiKey="your_api_key_here"

# Get the container app URL
az containerapp show \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --query properties.configuration.ingress.fqdn --output tsv
```

### Method 3: Using Infrastructure as Code (Bicep)

We provide Bicep templates in the `/infrastructure` directory for automated deployment:

```bash
# Deploy using Bicep template
az deployment group create \
  --resource-group rg-fukn-weather-prod \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/parameters.json \
  --parameters weatherApiKey="your_api_key_here"
```

See the [infrastructure/README.md](infrastructure/README.md) for detailed instructions.

## Security Considerations

### 1. API Key Management

- **Never** commit API keys to source control
- Use Azure Key Vault for all secrets
- Use Managed Identity for Key Vault access
- Rotate API keys regularly

### 2. Authentication and Authorization

For a shared MCP service, implement authentication:

**Option A: API Key Authentication**

```csharp
// Add to Program.cs
builder.Services.AddAuthentication("ApiKey")
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("ApiKey", null);
```

**Option B: Azure AD/Entra ID**

```bash
az webapp auth update \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --enabled true \
  --action LoginWithAzureActiveDirectory
```

**Option C: Use Azure API Management**

- Provides subscription keys
- Rate limiting per subscription
- OAuth 2.0 integration
- JWT validation

### 3. Network Security

```bash
# Restrict network access (if using App Service)
az webapp config access-restriction add \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --rule-name "AllowCloudflare" \
  --action Allow \
  --ip-address 173.245.48.0/20 \
  --priority 100
```

### 4. Enable Web Application Firewall (WAF)

Deploy Azure Application Gateway or Front Door with WAF enabled to protect against:
- SQL injection
- Cross-site scripting (XSS)
- DDoS attacks
- Common web vulnerabilities

### 5. HTTPS/TLS Configuration

```bash
# App Service automatically provides SSL
# For custom domain:
az webapp config ssl bind \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --certificate-thumbprint {thumbprint} \
  --ssl-type SNI
```

### 6. CORS Configuration

Update the CORS policy in `Program.cs` to restrict origins:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://trusted-domain.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

## Cost Estimation

### Development/Testing Environment

| Service | SKU | Monthly Cost (Approx) |
|---------|-----|----------------------|
| App Service | B1 (Basic) | $13 |
| Key Vault | Standard | $0.03 per 10K ops |
| Application Insights | Pay-as-you-go | $2-5 |
| **Total** | | **~$15-20/month** |

### Production Environment (Small Scale)

| Service | SKU | Monthly Cost (Approx) |
|---------|-----|----------------------|
| App Service | S1 (Standard) | $70 |
| Key Vault | Standard | $0.50 |
| Application Insights | 5GB included | $10-20 |
| **Total** | | **~$80-90/month** |

### Production Environment (High Scale)

| Service | SKU | Monthly Cost (Approx) |
|---------|-----|----------------------|
| Container Apps | 1M requests | $30-50 |
| API Management | Developer | $50 |
| Application Gateway + WAF | WAF V2 | $150 |
| Key Vault | Standard | $1 |
| Application Insights | 10GB | $50 |
| Container Registry | Basic | $5 |
| **Total** | | **~$285-305/month** |

**Note:** Costs vary by region and actual usage. Use the [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/) for accurate estimates.

## Monitoring and Maintenance

### Application Insights Dashboards

1. **Request Performance**
   - Track API response times
   - Monitor MCP tool call latency
   - Identify slow endpoints

2. **Error Tracking**
   - Failed requests
   - Exceptions
   - External API failures

3. **Custom Metrics**
   - Weather API call success rate
   - MCP protocol usage
   - Zip code request patterns

### Setting Up Alerts

```bash
# Create alert for high error rate
az monitor metrics alert create \
  --name "High Error Rate" \
  --resource-group rg-fukn-weather-prod \
  --scopes "/subscriptions/{subscription-id}/resourceGroups/rg-fukn-weather-prod/providers/Microsoft.Web/sites/fukn-weather-api" \
  --condition "avg Http5xx > 10" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action-group-id {action-group-id}
```

### Health Checks

Add health check endpoint:

```csharp
// In Program.cs
builder.Services.AddHealthChecks();
app.MapHealthChecks("/health");
```

Configure App Service health check:

```bash
az webapp config set \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --health-check-path "/health"
```

### Log Management

```bash
# Enable application logging
az webapp log config \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --application-logging filesystem \
  --level information

# Stream logs
az webapp log tail \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod
```

## Scaling Considerations

### Horizontal Scaling (App Service)

```bash
# Configure autoscale rules
az monitor autoscale create \
  --resource-group rg-fukn-weather-prod \
  --resource fukn-weather-api \
  --resource-type Microsoft.Web/sites \
  --name autoscale-fukn-weather \
  --min-count 1 \
  --max-count 5 \
  --count 1

# Add scale-out rule (CPU > 70%)
az monitor autoscale rule create \
  --resource-group rg-fukn-weather-prod \
  --autoscale-name autoscale-fukn-weather \
  --condition "Percentage CPU > 70 avg 5m" \
  --scale out 1

# Add scale-in rule (CPU < 30%)
az monitor autoscale rule create \
  --resource-group rg-fukn-weather-prod \
  --autoscale-name autoscale-fukn-weather \
  --condition "Percentage CPU < 30 avg 5m" \
  --scale in 1
```

### Container Apps Auto-scaling

Container Apps automatically scale based on HTTP requests, CPU, and memory:

```bash
# Update scaling rules
az containerapp update \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --min-replicas 1 \
  --max-replicas 20 \
  --scale-rule-name http-rule \
  --scale-rule-type http \
  --scale-rule-http-concurrency 50
```

### Performance Optimization

1. **Caching**: Implement response caching for frequently requested zip codes
2. **Connection Pooling**: Already configured with `HttpClient` in .NET
3. **Compression**: Enable gzip compression
4. **CDN**: Use Azure CDN for static content

```bash
# Enable compression
az webapp config set \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod \
  --http-logging-enabled true \
  --min-tls-version 1.2
```

## Multi-Region Deployment

For global availability, deploy to multiple regions:

```bash
# Deploy to multiple regions
REGIONS=("eastus" "westeurope" "southeastasia")

for region in "${REGIONS[@]}"; do
  az webapp create \
    --name fukn-weather-api-$region \
    --resource-group rg-fukn-weather-prod \
    --plan plan-fukn-weather-$region \
    --runtime "DOTNET|10.0"
done

# Configure Azure Front Door for global load balancing
az afd profile create \
  --profile-name fukn-weather-afd \
  --resource-group rg-fukn-weather-prod \
  --sku Premium_AzureFrontDoor
```

## Troubleshooting

### Common Issues

1. **502 Bad Gateway**
   - Check App Service logs
   - Verify application starts correctly
   - Check environment variables

2. **Weather API Key Errors**
   - Verify Key Vault permissions
   - Check managed identity configuration
   - Validate API key in Key Vault

3. **CORS Errors**
   - Update CORS policy in `Program.cs`
   - Verify allowed origins

### Diagnostic Commands

```bash
# Check app service status
az webapp show \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod

# View recent logs
az webapp log tail \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod

# SSH into container
az webapp ssh \
  --name fukn-weather-api \
  --resource-group rg-fukn-weather-prod
```

## Client Integration

Once deployed, external users can integrate with your MCP service:

### Example: Claude Desktop Configuration

```json
{
  "mcpServers": {
    "fukn-weather": {
      "url": "https://fukn-weather-api.azurewebsites.net/api/weather/mcp",
      "headers": {
        "X-API-Key": "your-client-api-key"
      }
    }
  }
}
```

### Example: Direct HTTP Calls

```bash
# Get weather
curl https://fukn-weather-api.azurewebsites.net/api/weather/10001 \
  -H "X-API-Key: your-client-api-key"

# List MCP tools
curl -X POST https://fukn-weather-api.azurewebsites.net/api/weather/mcp/tools \
  -H "X-API-Key: your-client-api-key"
```

## Next Steps

1. **Deploy to Development**: Start with App Service Basic tier
2. **Test Integration**: Verify MCP protocol functionality
3. **Implement Authentication**: Add API key or Azure AD auth
4. **Enable Monitoring**: Configure Application Insights
5. **Set Up CI/CD**: Automate deployments with GitHub Actions
6. **Scale to Production**: Upgrade to Standard/Premium tier
7. **Add WAF**: Deploy Application Gateway for security
8. **Go Global**: Add additional regions as needed

## Support and Resources

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Azure Container Apps Documentation](https://docs.microsoft.com/azure/container-apps/)
- [.NET on Azure Documentation](https://docs.microsoft.com/dotnet/azure/)
- [Model Context Protocol Specification](https://modelcontextprotocol.io/)

## Additional Resources

- See [infrastructure/README.md](infrastructure/README.md) for Bicep templates
- See [.github/workflows/azure-deploy.yml](.github/workflows/azure-deploy.yml) for CI/CD pipeline
- See [scripts/deploy-azure.sh](scripts/deploy-azure.sh) for deployment automation

---

For questions or issues with deployment, please open an issue on the [GitHub repository](https://github.com/idusortus/fuckinweather-mcp).
