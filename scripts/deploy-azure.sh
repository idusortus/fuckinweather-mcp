#!/bin/bash

# Azure Deployment Script for fuckinweather-mcp
# This script automates the deployment of the application to Azure App Service

set -e  # Exit on error

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check prerequisites
print_info "Checking prerequisites..."

if ! command_exists az; then
    print_error "Azure CLI is not installed. Please install it from https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
fi

if ! command_exists dotnet; then
    print_error ".NET SDK is not installed. Please install .NET 10 SDK"
    exit 1
fi

# Parse command line arguments
ENVIRONMENT=${1:-dev}
RESOURCE_GROUP=${2:-rg-fukn-weather-${ENVIRONMENT}}
LOCATION=${3:-eastus}

print_info "Deployment Configuration:"
echo "  Environment: $ENVIRONMENT"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Location: $LOCATION"
echo ""

# Check if weather API key is provided
if [ -z "$WEATHER_API_KEY" ]; then
    print_error "WEATHER_API_KEY environment variable is not set"
    echo "Please set it with: export WEATHER_API_KEY='your_api_key_here'"
    exit 1
fi

# Verify Azure CLI is logged in
print_info "Checking Azure CLI authentication..."
if ! az account show >/dev/null 2>&1; then
    print_error "Not logged in to Azure. Please run 'az login' first"
    exit 1
fi

SUBSCRIPTION_NAME=$(az account show --query name -o tsv)
print_info "Using Azure subscription: $SUBSCRIPTION_NAME"

# Create resource group if it doesn't exist
print_info "Ensuring resource group exists..."
if ! az group show --name "$RESOURCE_GROUP" >/dev/null 2>&1; then
    print_info "Creating resource group: $RESOURCE_GROUP"
    az group create --name "$RESOURCE_GROUP" --location "$LOCATION"
else
    print_info "Resource group already exists: $RESOURCE_GROUP"
fi

# Deploy infrastructure using Bicep
print_info "Deploying infrastructure..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --template-file infrastructure/main.bicep \
    --parameters "infrastructure/parameters.json" \
    --parameters environment="$ENVIRONMENT" \
    --parameters location="$LOCATION" \
    --parameters weatherApiKey="$WEATHER_API_KEY" \
    --query properties.outputs \
    --output json)

if [ $? -ne 0 ]; then
    print_error "Infrastructure deployment failed"
    exit 1
fi

print_info "Infrastructure deployed successfully"

# Extract outputs
APP_SERVICE_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.appServiceName.value')
APP_SERVICE_URL=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.appServiceUrl.value')
KEY_VAULT_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.keyVaultName.value')

print_info "Deployment outputs:"
echo "  App Service Name: $APP_SERVICE_NAME"
echo "  App Service URL: $APP_SERVICE_URL"
echo "  Key Vault Name: $KEY_VAULT_NAME"
echo ""

# Build and publish application
print_info "Building application..."
cd "$(dirname "$0")/.."
dotnet restore
dotnet build -c Release

print_info "Publishing application..."
dotnet publish src/FuknWeather.Api/FuknWeather.Api.csproj -c Release -o ./publish

if [ ! -d "./publish" ]; then
    print_error "Publish directory not found"
    exit 1
fi

# Create deployment package
print_info "Creating deployment package..."
cd publish
zip -r ../deploy.zip . >/dev/null
cd ..

if [ ! -f "deploy.zip" ]; then
    print_error "Failed to create deployment package"
    exit 1
fi

# Deploy to Azure App Service
print_info "Deploying application to Azure App Service..."
az webapp deploy \
    --resource-group "$RESOURCE_GROUP" \
    --name "$APP_SERVICE_NAME" \
    --src-path deploy.zip \
    --type zip \
    --async false

if [ $? -ne 0 ]; then
    print_error "Application deployment failed"
    exit 1
fi

# Cleanup
print_info "Cleaning up temporary files..."
rm -rf publish deploy.zip

# Wait for application to start
print_info "Waiting for application to start..."
sleep 10

# Test the deployment
print_info "Testing deployment..."
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "${APP_SERVICE_URL}/api/weather/10001")

if [ "$HTTP_STATUS" = "200" ]; then
    print_info "✅ Deployment successful! Application is responding"
else
    print_warn "⚠️  Application deployed but returned HTTP status: $HTTP_STATUS"
    print_warn "Check application logs for details"
fi

# Print summary
echo ""
print_info "=========================================="
print_info "Deployment Summary"
print_info "=========================================="
echo "Environment: $ENVIRONMENT"
echo "Resource Group: $RESOURCE_GROUP"
echo "App Service: $APP_SERVICE_NAME"
echo "URL: $APP_SERVICE_URL"
echo ""
print_info "Test your deployment:"
echo "  curl ${APP_SERVICE_URL}/api/weather/10001"
echo ""
print_info "View logs:"
echo "  az webapp log tail --name $APP_SERVICE_NAME --resource-group $RESOURCE_GROUP"
echo ""
print_info "Open in browser:"
echo "  open ${APP_SERVICE_URL}/api/weather/10001"
echo ""
print_info "=========================================="

exit 0
