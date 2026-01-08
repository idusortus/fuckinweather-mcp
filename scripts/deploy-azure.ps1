# Azure Deployment Script for fuckinweather-mcp
# PowerShell version for Windows users

param(
    [Parameter(Position=0)]
    [string]$Environment = "dev",
    
    [Parameter(Position=1)]
    [string]$ResourceGroup = "rg-fukn-weather-$Environment",
    
    [Parameter(Position=2)]
    [string]$Location = "eastus"
)

$ErrorActionPreference = "Stop"

# Function to write colored output
function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Green
}

function Write-Warn {
    param([string]$Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Check prerequisites
Write-Info "Checking prerequisites..."

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI is not installed. Please install it from https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error ".NET SDK is not installed. Please install .NET 10 SDK"
    exit 1
}

Write-Info "Deployment Configuration:"
Write-Host "  Environment: $Environment"
Write-Host "  Resource Group: $ResourceGroup"
Write-Host "  Location: $Location"
Write-Host ""

# Check if weather API key is provided
$WeatherApiKey = $env:WEATHER_API_KEY
if ([string]::IsNullOrEmpty($WeatherApiKey)) {
    Write-Error "WEATHER_API_KEY environment variable is not set"
    Write-Host "Please set it with: `$env:WEATHER_API_KEY='your_api_key_here'"
    exit 1
}

# Verify Azure CLI is logged in
Write-Info "Checking Azure CLI authentication..."
try {
    $account = az account show | ConvertFrom-Json
    Write-Info "Using Azure subscription: $($account.name)"
} catch {
    Write-Error "Not logged in to Azure. Please run 'az login' first"
    exit 1
}

# Create resource group if it doesn't exist
Write-Info "Ensuring resource group exists..."
$rgExists = az group exists --name $ResourceGroup
if ($rgExists -eq "false") {
    Write-Info "Creating resource group: $ResourceGroup"
    az group create --name $ResourceGroup --location $Location
} else {
    Write-Info "Resource group already exists: $ResourceGroup"
}

# Deploy infrastructure using Bicep
Write-Info "Deploying infrastructure..."
try {
    $deploymentOutput = az deployment group create `
        --resource-group $ResourceGroup `
        --template-file infrastructure/main.bicep `
        --parameters infrastructure/parameters.json `
        --parameters environment=$Environment `
        --parameters location=$Location `
        --parameters weatherApiKey=$WeatherApiKey `
        --query properties.outputs `
        --output json | ConvertFrom-Json
    
    Write-Info "Infrastructure deployed successfully"
} catch {
    Write-Error "Infrastructure deployment failed: $_"
    exit 1
}

# Extract outputs
$appServiceName = $deploymentOutput.appServiceName.value
$appServiceUrl = $deploymentOutput.appServiceUrl.value
$keyVaultName = $deploymentOutput.keyVaultName.value

Write-Info "Deployment outputs:"
Write-Host "  App Service Name: $appServiceName"
Write-Host "  App Service URL: $appServiceUrl"
Write-Host "  Key Vault Name: $keyVaultName"
Write-Host ""

# Get script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

# Build and publish application
Write-Info "Building application..."
Push-Location $rootDir
try {
    dotnet restore
    dotnet build -c Release
    
    Write-Info "Publishing application..."
    dotnet publish src/FuknWeather.Api/FuknWeather.Api.csproj -c Release -o ./publish
    
    if (-not (Test-Path "./publish")) {
        Write-Error "Publish directory not found"
        exit 1
    }
    
    # Create deployment package
    Write-Info "Creating deployment package..."
    $publishDir = Join-Path $rootDir "publish"
    $deployZip = Join-Path $rootDir "deploy.zip"
    
    if (Test-Path $deployZip) {
        Remove-Item $deployZip -Force
    }
    
    Compress-Archive -Path "$publishDir\*" -DestinationPath $deployZip
    
    if (-not (Test-Path $deployZip)) {
        Write-Error "Failed to create deployment package"
        exit 1
    }
    
    # Deploy to Azure App Service
    Write-Info "Deploying application to Azure App Service..."
    az webapp deploy `
        --resource-group $ResourceGroup `
        --name $appServiceName `
        --src-path $deployZip `
        --type zip `
        --async false
    
    # Cleanup
    Write-Info "Cleaning up temporary files..."
    if (Test-Path "./publish") {
        Remove-Item -Recurse -Force "./publish"
    }
    if (Test-Path $deployZip) {
        Remove-Item -Force $deployZip
    }
    
    # Wait for application to start
    Write-Info "Waiting for application to start..."
    Start-Sleep -Seconds 10
    
    # Test the deployment
    Write-Info "Testing deployment..."
    try {
        $response = Invoke-WebRequest -Uri "$appServiceUrl/api/weather/10001" -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            Write-Info "✅ Deployment successful! Application is responding"
        }
    } catch {
        Write-Warn "⚠️ Application deployed but test request failed: $_"
        Write-Warn "Check application logs for details"
    }
    
    # Print summary
    Write-Host ""
    Write-Info "=========================================="
    Write-Info "Deployment Summary"
    Write-Info "=========================================="
    Write-Host "Environment: $Environment"
    Write-Host "Resource Group: $ResourceGroup"
    Write-Host "App Service: $appServiceName"
    Write-Host "URL: $appServiceUrl"
    Write-Host ""
    Write-Info "Test your deployment:"
    Write-Host "  Invoke-WebRequest -Uri $appServiceUrl/api/weather/10001"
    Write-Host ""
    Write-Info "View logs:"
    Write-Host "  az webapp log tail --name $appServiceName --resource-group $ResourceGroup"
    Write-Host ""
    Write-Info "Open in browser:"
    Write-Host "  Start-Process $appServiceUrl/api/weather/10001"
    Write-Host ""
    Write-Info "=========================================="
    
} catch {
    Write-Error "Deployment failed: $_"
    exit 1
} finally {
    Pop-Location
}

exit 0
