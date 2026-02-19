# Technology Stack

## Framework & Runtime

- .NET 10 with C#
- Implicit usings enabled
- Nullable reference types enabled

## Core Libraries

- **Telegram.Bot** (v22.0.0) - Telegram Bot API SDK
- **SpotifyAPI.Web** (v7.2.1) - Spotify Web API SDK
- **Azure.Data.Tables** (v12.9.1) - Azure Table Storage client
- **Sentry** (v5.0.0+) - Error logging and monitoring
- **xUnit** - Unit testing framework

### Telegram.Bot Deserialization

**CRITICAL: When deserializing JSON to Telegram.Bot.Types models, ALWAYS use `Telegram.Bot.JsonBotAPI.Options`.**

The Telegram Bot API uses snake_case for JSON properties, but C# models use PascalCase. The `JsonBotAPI.Options` provides the correct serialization settings for proper conversion.

**Example:**
```csharp
using System.Text.Json;
using Telegram.Bot.Types;

// ✅ CORRECT: Use JsonBotAPI.Options
var update = JsonSerializer.Deserialize<Update>(json, Telegram.Bot.JsonBotAPI.Options);
var message = JsonSerializer.Deserialize<Message>(json, Telegram.Bot.JsonBotAPI.Options);

// ❌ WRONG: Without JsonBotAPI.Options, properties will be null
var update = JsonSerializer.Deserialize<Update>(json); // Properties will be null!
```

**When to use:**
- Deserializing webhook payloads in Azure Functions
- Creating test fixtures with Telegram.Bot types
- Any JSON deserialization involving `Telegram.Bot.Types` namespace

**Why it's needed:**
- Telegram API uses `snake_case` (e.g., `message_id`, `from`, `chat`)
- C# models use `PascalCase` (e.g., `MessageId`, `From`, `Chat`)
- Without proper options, JsonSerializer cannot map properties correctly
- Properties will remain null even if present in JSON

## Azure Services

- **Azure Functions V4** - Serverless webhook handlers
- **Azure Table Storage** - NoSQL data persistence
- **Application Insights** - Telemetry and monitoring

## Frontend

- **Blazor** (Server/WebAssembly) - Web UI framework
- **ASP.NET Core** - Web hosting

## Common Commands

### Build
```bash
dotnet build
```

### Run Tests

**CRITICAL: Azurite must be running before executing tests that interact with Azure Table Storage.**

Tests that require data persistence will hang indefinitely if Azurite is not running.

```bash
# Start Azurite (in a separate terminal or as a background process)
azurite --silent --location ./bin/azurite --debug ./bin/azurite/debug.log

# Then run tests
dotnet test
```

**Windows PowerShell (background process):**
```powershell
Start-Process azurite -ArgumentList "--silent", "--location", "./bin/azurite", "--debug", "./bin/azurite/debug.log" -WindowStyle Hidden
dotnet test
```

### Run Azure Functions Locally
```bash
cd KiroSpotiBot.Functions
func start
```

### Run Blazor Web Locally
```bash
cd KiroSpotiBot.Web
dotnet run
```

### Restore Dependencies
```bash
dotnet restore
```

## Local Development Requirements

- .NET 10 SDK
- Azure Storage Emulator (Windows) or Azurite (cross-platform)
- Azure Functions Core Tools (for local function execution)
- Configuration in `local.settings.json` (not checked into source control)

## Configuration Keys

Required environment variables/settings:
- `AZURE_STORAGE_CONNECTION_STRING`
- `TELEGRAM_BOT_TOKEN`
- `SPOTIFY_CLIENT_ID`
- `SPOTIFY_CLIENT_SECRET`
- `ENCRYPTION_KEY`
- `SENTRY_DSN` (optional)
