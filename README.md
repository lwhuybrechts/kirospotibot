# Telegram Spotify Bot

A .NET 10 Telegram bot that automatically detects Spotify track URLs in group chats and adds them to configured playlists. Features collaborative voting, automatic track removal, and a Blazor web frontend for browsing playlist history.

## Project Structure

- **KiroSpotiBot.Core** - Core domain models, entities, and interfaces
- **KiroSpotiBot.Functions** - Azure Functions for Telegram webhooks and OAuth handlers
- **KiroSpotiBot.Infrastructure** - Data access layer with Azure Table Storage repositories
- **KiroSpotiBot.Web** - Blazor web frontend for browsing playlists and analytics
- **KiroSpotiBot.Tests** - Unit and property-based tests using xUnit and FsCheck

## Technologies

- .NET 10
- Azure Functions (serverless webhooks)
- Azure Table Storage (ultra-low-cost NoSQL storage)
- Telegram.Bot SDK
- SpotifyAPI.Web SDK
- Blazor Server/WebAssembly
- Sentry (error logging and monitoring)
- FsCheck (property-based testing)

## Getting Started

### Prerequisites

- .NET 10 SDK
- Azure Storage Emulator or Azure Storage Account
- Telegram Bot Token (from @BotFather)
- Spotify Developer Account (for OAuth credentials)

### Configuration

1. Copy `KiroSpotiBot.Functions/local.settings.json` and fill in the required values:
   - `AZURE_STORAGE_CONNECTION_STRING` - Azure Storage connection string
   - `TELEGRAM_BOT_TOKEN` - Your Telegram bot token
   - `SPOTIFY_CLIENT_ID` - Spotify OAuth client ID
   - `SPOTIFY_CLIENT_SECRET` - Spotify OAuth client secret
   - `ENCRYPTION_KEY` - Key for encrypting Spotify credentials
   - `SENTRY_DSN` - Sentry project DSN (optional)

2. Run Azure Storage Emulator for local development:
   ```bash
   # Windows
   AzureStorageEmulator.exe start
   ```

### Running Locally

```bash
# Build the solution
dotnet build

# Run Azure Functions
cd KiroSpotiBot.Functions
func start

# Run Blazor Web (in separate terminal)
cd KiroSpotiBot.Web
dotnet run
```

### Running Tests

```bash
dotnet test
```

## Architecture

The application follows a stateless, webhook-driven architecture:

1. Telegram sends webhook requests to Azure Functions
2. Functions process messages and detect Spotify URLs
3. Tracks are added to playlists using administrator credentials
4. All state is persisted in Azure Table Storage
5. Blazor frontend provides web interface for browsing

## Features

- ✅ Automatic Spotify URL detection in group chats
- ✅ Collaborative playlist building
- ✅ Voting system (upvotes/downvotes)
- ✅ Automatic track removal based on downvotes
- ✅ OAuth authentication for Spotify
- ✅ Auto-queue tracks to user's Spotify queue
- ✅ Chat history synchronization
- ✅ Web frontend for browsing playlists
- ✅ User and genre filtering
- ✅ Property-based testing for correctness

## Deployment

See `.github/workflows` for CI/CD pipeline configuration using GitHub Actions.

## License

MIT
