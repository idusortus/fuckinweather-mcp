# Azure Deployment Documentation

This directory contains comprehensive guides for deploying and managing fuckinweather-mcp on Microsoft Azure.

## Quick Links

- **[Quick Start Guide](../AZURE_QUICKSTART.md)** - Get deployed in 5 minutes
- **[Full Deployment Guide](../AZURE_DEPLOYMENT.md)** - Comprehensive deployment documentation
- **[Infrastructure README](../infrastructure/README.md)** - Bicep templates and IaC

## Deployment Options

### 1. Quick Deploy with Script (Recommended for Getting Started)

```bash
export WEATHER_API_KEY="your_api_key"
./scripts/deploy-azure.sh dev
```

**Best for**: Quick deployments, testing, development

### 2. Infrastructure as Code with Bicep

```bash
az deployment group create \
  --resource-group rg-fukn-weather-dev \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/parameters.json \
  --parameters weatherApiKey="$WEATHER_API_KEY"
```

**Best for**: Production, version control, repeatability

### 3. CI/CD with GitHub Actions

Push to `main` branch and GitHub Actions automatically deploys.

**Best for**: Team environments, automated workflows

See [.github/workflows/azure-deploy.yml](../.github/workflows/azure-deploy.yml)

## Architecture Options

### Azure App Service (Recommended for Simplicity)
- **Deployment**: `infrastructure/main.bicep`
- **Cost**: ~$15-90/month
- **Best for**: Simple deployments, small to medium scale

### Azure Container Apps (Recommended for Scale)
- **Deployment**: `infrastructure/main.containerapp.bicep`
- **Cost**: Pay-per-use, ~$30-50/month
- **Best for**: Production workloads, auto-scaling, cost optimization

## Documentation Index

### Getting Started
1. [Azure Quick Start](../AZURE_QUICKSTART.md) - 5-minute deployment guide
2. [Azure Deployment Guide](../AZURE_DEPLOYMENT.md) - Comprehensive documentation
3. [Infrastructure README](../infrastructure/README.md) - Bicep templates

### Advanced Topics
1. [Adding API Key Authentication](examples/adding-api-key-authentication.md) - Secure your API
2. [Monitoring and Alerts](examples/monitoring-and-alerts.md) - Set up monitoring

### Reference
- [GitHub Actions Workflow](../.github/workflows/azure-deploy.yml) - CI/CD pipeline
- [Deployment Scripts](../scripts/) - Bash and PowerShell automation

## Deployment Scenarios

### Scenario 1: Personal/Development Use
- Use Free/Basic tier App Service
- Single region deployment
- Basic monitoring
- Estimated cost: $15-20/month

**Deploy with**:
```bash
./scripts/deploy-azure.sh dev
```

### Scenario 2: Shared Service for Small Team
- Standard tier App Service
- Application Insights monitoring
- API key authentication
- Estimated cost: $80-100/month

**Deploy with**:
```bash
# Deploy infrastructure
az deployment group create \
  --resource-group rg-fukn-weather-prod \
  --template-file infrastructure/main.bicep \
  --parameters infrastructure/parameters.prod.json \
  --parameters weatherApiKey="$WEATHER_API_KEY"

# Add authentication (see examples/adding-api-key-authentication.md)
```

### Scenario 3: Public MCP Service
- Container Apps for auto-scaling
- Azure API Management
- Web Application Firewall
- Multi-region deployment
- Estimated cost: $300-500/month

**Components**:
- Container Apps
- Azure API Management (Developer tier)
- Application Gateway with WAF
- Azure Front Door (optional)

See [Full Deployment Guide](../AZURE_DEPLOYMENT.md) for details.

## Security Considerations

### For Shared/Public Services

1. **API Key Authentication** - [Implementation Guide](examples/adding-api-key-authentication.md)
2. **Rate Limiting** - Prevent abuse
3. **CORS Configuration** - Restrict origins
4. **Key Vault Integration** - Secure secrets
5. **HTTPS Only** - Enforce TLS
6. **WAF Protection** - Web Application Firewall

### Compliance

- Store secrets in Azure Key Vault
- Enable audit logging
- Configure retention policies
- Implement RBAC
- Use managed identities

## Monitoring and Operations

### Essential Monitoring

1. **Application Insights** - Automatically configured
2. **Custom Metrics** - Track business events
3. **Alert Rules** - Get notified of issues
4. **Health Checks** - Monitor availability
5. **Log Analytics** - Query and analyze logs

See [Monitoring Guide](examples/monitoring-and-alerts.md) for implementation.

### Key Metrics to Track

- Request rate and response time
- Error rate (4xx, 5xx)
- External API success rate
- CPU and memory usage
- Availability percentage

## Cost Management

### Optimization Tips

1. **Use Container Apps** - Scale to zero when idle
2. **Right-size SKU** - Match resources to load
3. **Enable auto-scaling** - Scale based on demand
4. **Use consumption pricing** - Pay for what you use
5. **Set budget alerts** - Prevent overspending

### Cost Estimates

| Configuration | Monthly Cost |
|--------------|--------------|
| Dev (B1 App Service) | $15-20 |
| Small Prod (S1 App Service) | $80-100 |
| Medium Prod (Container Apps) | $150-250 |
| Enterprise (with APIM, WAF) | $300-500 |

See [Cost Calculator](https://azure.microsoft.com/pricing/calculator/)

## Troubleshooting

### Common Issues

#### Application Not Starting
```bash
# Check logs
az webapp log tail --name your-app --resource-group your-rg

# Check configuration
az webapp config appsettings list --name your-app --resource-group your-rg
```

#### API Key Not Working
- Verify Key Vault permissions
- Check managed identity configuration
- Validate Key Vault references

#### High Response Times
- Check Application Insights for bottlenecks
- Review external API performance
- Consider scaling up or out

See [Troubleshooting Section](../AZURE_DEPLOYMENT.md#troubleshooting) in full guide.

## Client Integration

Once deployed, clients can integrate with your MCP service:

### Claude Desktop
```json
{
  "mcpServers": {
    "fukn-weather": {
      "url": "https://your-app.azurewebsites.net/api/weather/mcp",
      "headers": {
        "X-API-Key": "your-api-key"
      }
    }
  }
}
```

### Direct HTTP
```bash
curl https://your-app.azurewebsites.net/api/weather/10001 \
  -H "X-API-Key: your-api-key"
```

## CI/CD Setup

### GitHub Actions Setup

1. Create Azure Service Principal:
```bash
az ad sp create-for-rbac \
  --name "github-actions-fukn-weather" \
  --role contributor \
  --scopes /subscriptions/{subscription-id} \
  --sdk-auth
```

2. Add GitHub Secrets:
   - `AZURE_CREDENTIALS` - Service principal JSON
   - `WEATHER_API_KEY` - OpenWeatherMap API key

3. Push to main branch - automatic deployment!

See [GitHub Actions Workflow](../.github/workflows/azure-deploy.yml)

## Best Practices

### Development
- Use separate resource groups per environment
- Use parameters files for configuration
- Test deployments in dev environment first
- Use managed identities over connection strings

### Production
- Enable Application Insights
- Configure auto-scaling
- Set up alert rules
- Use deployment slots for zero-downtime
- Implement health checks
- Enable diagnostic logs
- Use Azure Key Vault for all secrets

### Security
- Never commit secrets to source control
- Use HTTPS only
- Implement authentication
- Enable WAF for public services
- Configure CORS properly
- Use network security groups
- Implement rate limiting

## Getting Help

### Documentation
- [Azure App Service Docs](https://docs.microsoft.com/azure/app-service/)
- [Azure Container Apps Docs](https://docs.microsoft.com/azure/container-apps/)
- [Application Insights Docs](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)

### Support
- [GitHub Issues](https://github.com/idusortus/fuckinweather-mcp/issues)
- [Azure Support Portal](https://portal.azure.com)
- [Stack Overflow](https://stackoverflow.com/questions/tagged/azure)

## Contributing

Improvements to deployment documentation are welcome! Please:
1. Test your changes
2. Update relevant documentation
3. Submit a pull request

## License

This deployment documentation is part of the fuckinweather-mcp project and follows the same MIT License.

---

**Ready to deploy?** Start with the [Quick Start Guide](../AZURE_QUICKSTART.md)!
