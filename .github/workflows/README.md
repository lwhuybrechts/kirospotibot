# GitHub Actions Workflows

This directory contains CI/CD workflows for the KiroSpotiBot project.

## Workflows

### 1. Build and Test (`build-and-test.yml`)
Runs on every push and pull request to `main` and `develop` branches.

**Actions:**
- Builds the .NET solution
- Runs all unit and property tests with Azurite
- Generates code coverage reports
- Uploads coverage artifacts

### 2. Deploy Azure Functions (`deploy-functions.yml`)
Deploys the serverless webhook handlers to Azure Functions.

**Triggers:** Push to `main` branch or manual dispatch

**Requirements:**
- `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` secret
- `AZURE_RESOURCE_GROUP` secret
- `KEYVAULT_URI` secret
- `AZURE_CREDENTIALS` secret

### 3. Deploy Blazor Frontend (`deploy-blazor.yml`)
Deploys the Blazor WebAssembly application to Azure Static Web Apps (free tier).

**Triggers:** Push to `main` branch or manual dispatch

**Why Static Web Apps?**
- Free tier includes 100 GB bandwidth/month
- No server costs - pure static hosting
- Perfect for low-traffic applications
- Scales to zero when not in use

**Required Secrets:**
- `AZURE_STATIC_WEB_APPS_API_TOKEN` - Deployment token from Azure Static Web Apps

**Architecture:**
- Blazor WebAssembly frontend (client-side rendering)
- API endpoints in Azure Functions for data access
- Shared DTO models in `KiroSpotiBot.ApiModels` project
- HTTP client services for API communication

## Configuration

### Setting up Secrets

Navigate to your GitHub repository → Settings → Secrets and variables → Actions, then add:

**For Azure Functions:**
- `AZURE_FUNCTIONAPP_PUBLISH_PROFILE`
- `AZURE_RESOURCE_GROUP`
- `KEYVAULT_URI`
- `AZURE_CREDENTIALS`

**For Blazor Frontend:**
- `AZURE_STATIC_WEB_APPS_API_TOKEN`

### Setting up Variables

No variables are required for the simplified deployment.

### Azure Key Vault Configuration

The workflows retrieve sensitive configuration from Azure Key Vault. Ensure the following secrets exist in your Key Vault:

- `AzureStorageConnectionString` - Connection string for Azure Table Storage
- `TelegramBotToken` - Telegram bot API token
- `SpotifyClientId` - Spotify OAuth client ID
- `SpotifyClientSecret` - Spotify OAuth client secret
- `EncryptionKey` - Encryption key for sensitive data
- `SentryDsn` - Sentry error logging DSN (optional)

### Getting Azure Static Web Apps API Token

1. Go to Azure Portal
2. Navigate to your Static Web App (or create one)
3. Go to Overview → Manage deployment token
4. Copy the deployment token
5. Add as `AZURE_STATIC_WEB_APPS_API_TOKEN` secret in GitHub

**Creating a new Static Web App:**
```bash
az staticwebapp create \
  --name kirospotibot-web \
  --resource-group kirospotibot-rg \
  --location "East US 2" \
  --sku Free
```

## Manual Deployment

All workflows support manual triggering via the Actions tab:

1. Go to Actions tab in GitHub
2. Select the workflow
3. Click "Run workflow"
4. Choose the branch and click "Run workflow"

## Troubleshooting

### Build Failures
- Ensure .NET 10 SDK is properly configured
- Check that all project references are correct
- Verify NuGet package versions are compatible

### Deployment Failures
- Verify `AZURE_STATIC_WEB_APPS_API_TOKEN` secret is set correctly
- Check that the Static Web App exists in Azure
- Ensure the deployment token hasn't expired (regenerate if needed)
- Verify the publish output path is correct (`./publish/wwwroot`)

### Configuration Issues
- Static Web Apps configuration is handled through the Azure Functions API
- The frontend calls the Functions API endpoints for data access
- No direct Key Vault access needed from the frontend
- Ensure CORS is configured on Azure Functions to allow Static Web App domain

## Local Testing

Before deploying, test locally:

```bash
# Build
dotnet build --configuration Release

# Run tests
azurite --silent --location ./bin/azurite --debug ./bin/azurite/debug.log &
dotnet test --configuration Release

# Test Blazor app locally
cd KiroSpotiBot.Web
dotnet run
```
