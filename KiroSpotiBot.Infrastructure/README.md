# KiroSpotiBot.Infrastructure

This project contains the data access layer and infrastructure services for the Telegram Spotify Bot.

## Features

- **Azure Table Storage Repositories**: CRUD operations for all entities
- **AES-256 Encryption**: Secure storage of Spotify credentials
- **Options Pattern**: Configuration using strongly-typed options classes

## Configuration

### Using Options Pattern

The infrastructure layer uses the options pattern for configuration. Add the following to your `appsettings.json`:

```json
{
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
  },
  "Encryption": {
    "Key": "your-encryption-key-here"
  }
}
```

### Environment Variables (Azure Functions)

For Azure Functions, you can also use environment variables:

- `AzureStorage__ConnectionString`: Azure Storage connection string
- `Encryption__Key`: Encryption key for sensitive data

**Important**: Store the encryption key in Azure Key Vault and reference it in your Function App settings.

## Service Registration

The infrastructure services are already registered in the Azure Functions `Program.cs`:

```csharp
using KiroSpotiBot.Infrastructure;

// Register infrastructure services (repositories, encryption, Azure Table Storage)
builder.Services.AddInfrastructure(builder.Configuration);
```

This single call registers:
- `TableServiceClient` (singleton)
- `IEncryptionService` (singleton)
- `IGroupChatRepository` (scoped)
- `IUserRepository` (scoped)
- `ITrackRecordRepository` (scoped)
- `IVoteRepository` (scoped)

For other projects (like Blazor Web), add the same line to your service configuration.

## Repositories

### IGroupChatRepository

Manages group chat configurations:
- Get/Create/Update group chats
- Check if playlist is already linked to another group

### IUserRepository

Manages user data and Spotify credentials:
- Get/Create/Update users
- **UpdateSpotifyCredentialsAsync**: Store encrypted Spotify tokens
- **GetDecryptedSpotifyAccessTokenAsync**: Retrieve decrypted access token
- **GetDecryptedSpotifyRefreshTokenAsync**: Retrieve decrypted refresh token

### ITrackRecordRepository

Manages track sharing history:
- Get track records with pagination
- Check if track exists or is deleted
- Mark tracks as deleted

### IVoteRepository

Manages voting on tracks:
- Upsert/Delete votes
- Get vote counts (upvotes, downvotes)
- Get all votes for a track

## Encryption

The `AesEncryptionService` uses AES-256-CBC encryption for sensitive data:
- Spotify access tokens
- Spotify refresh tokens

The encryption key is derived from the configured key using SHA-256 hashing.

## Azure Table Storage Schema

### Tables

- **GroupChats**: Group chat configurations
- **Users**: User profiles and encrypted Spotify credentials
- **TrackRecords**: Track sharing events
- **Votes**: User votes on tracks

### Partition Keys

- **GroupChats**: `"GROUPCHAT"`
- **Users**: `"USER"`
- **TrackRecords**: `TelegramChatId` (enables efficient queries per group)
- **Votes**: `TrackRecordId` (enables efficient queries per track)

## Security Best Practices

1. **Never commit encryption keys**: Use Azure Key Vault or environment variables
2. **Rotate keys regularly**: Implement key rotation for encryption keys
3. **Use managed identities**: When possible, use Azure Managed Identity for storage access
4. **Audit access**: Enable Azure Storage logging for audit trails
