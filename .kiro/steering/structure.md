---
inclusion: always
---

# Project Structure & Architecture

## Clean Architecture Layers

This solution uses clean architecture with dependency flow: Core ← Infrastructure ← Functions/Web. Core has no external dependencies except Azure Table Storage base classes.

### KiroSpotiBot.Core
Domain layer containing entities and interfaces only.

**When to add code here:**
- New domain entities representing business concepts
- Shared interfaces for repositories or services
- Value objects and domain logic

**Entity conventions (MUST follow):**
- Inherit from `MyTableEntity` base class
- Naming: `{Domain}Entity.cs` (e.g., `TrackEntity`, `UserEntity`)
- Include parameterless constructor for Azure Table Storage deserialization
- Include parameterized constructor that sets PartitionKey and RowKey
- Set partition/row keys in constructor for optimal Table Storage queries
- Use nullable reference types (`?`) for optional properties

**Example entity structure:**
```csharp
public class ExampleEntity : MyTableEntity
{
    public ExampleEntity() { } // Required for deserialization
    
    public ExampleEntity(string partitionKey, string rowKey)
    {
        PartitionKey = partitionKey;
        RowKey = rowKey;
    }
    
    public string? OptionalProperty { get; set; }
    public string RequiredProperty { get; set; } = string.Empty;
}
```

### KiroSpotiBot.Infrastructure
Data access layer implementing repository pattern.

**When to add code here:**
- Repository implementations for data access
- Azure Table Storage client wrappers
- Data mapping and transformation logic

**Dependencies:** Core only

### KiroSpotiBot.Functions
Serverless webhook handlers and HTTP endpoints.

**When to add code here:**
- Telegram webhook handlers
- OAuth callback endpoints
- Scheduled background jobs

**Key patterns:**
- HTTP-triggered Azure Functions
- Stateless request processing
- Configuration via `local.settings.json` (never commit this file)

**Dependencies:** Core, Infrastructure

### KiroSpotiBot.Web
Blazor frontend for user-facing features.

**When to add code here:**
- UI components and pages
- Client-side state management
- API consumption logic

**Dependencies:** Core, Infrastructure

### KiroSpotiBot.Tests
Test suite using xUnit and property-based testing.

**When to add code here:**
- Unit tests for business logic
- Property-based tests for correctness properties
- Integration tests for repositories

**Test file naming:** `{ClassUnderTest}Tests.cs`

**Dependencies:** All projects

## Code Conventions (MUST follow)

### Comments
- All comments MUST end with a period at the end of sentences
- This applies to single-line comments (`//`), multi-line comments (`/* */`), and XML documentation comments (`///`)
- Example: `// This is a correct comment.`
- Example: `/// <summary>This is correct.</summary>`

### Namespaces
- Namespace MUST match folder structure exactly
- Example: File at `KiroSpotiBot.Core/Entities/TrackEntity.cs` → namespace `KiroSpotiBot.Core.Entities`

### Nullable Reference Types
- Enabled project-wide
- Use `?` for all nullable references
- Initialize non-nullable properties with default values or in constructor

### Implicit Usings
- Common namespaces are auto-imported (System, System.Collections.Generic, etc.)
- Do not add redundant using statements for implicit namespaces

### Configuration
- Sensitive settings go in `local.settings.json` (gitignored)
- Never hardcode secrets or connection strings
- Use environment variables for production configuration
- **ALWAYS use the Options Pattern for configuration** - NEVER inject `IConfiguration` directly into services
- Create strongly-typed options classes in `KiroSpotiBot.Infrastructure/Options/`
- Options class naming: `{Feature}Options.cs` (e.g., `SpotifyOptions`, `EncryptionOptions`)
- Include a `const string SectionName` property for the configuration section name
- **Add DataAnnotations validation attributes** to options properties (e.g., `[Required]`, `[Range]`, `[EmailAddress]`)
- Register options in `DependencyInjection.cs` using `services.Configure<TOptions>(configuration.GetSection(SectionName))`
- **Use `.AddOptionsWithValidateOnStart<TOptions>()` to validate configuration at startup** instead of at first use
- Inject options into services using `IOptions<TOptions>`

**Example options class with validation:**
```csharp
using System.ComponentModel.DataAnnotations;

public class SpotifyOptions
{
    public const string SectionName = "Spotify";
    
    [Required(ErrorMessage = "Spotify:ClientId is required.")]
    public string ClientId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Spotify:ClientSecret is required.")]
    public string ClientSecret { get; set; } = string.Empty;
}
```

**Example registration with validation:**
```csharp
services.Configure<SpotifyOptions>(configuration.GetSection(SpotifyOptions.SectionName))
    .AddOptionsWithValidateOnStart<SpotifyOptions>();
```

**Example service using options:**
```csharp
public class SpotifyService
{
    private readonly SpotifyOptions _options;
    
    public SpotifyService(IOptions<SpotifyOptions> options)
    {
        _options = options.Value;
    }
}
```

### Error Handling
- Sentry integration for production error tracking
- Log exceptions with context before rethrowing
- Use structured logging with meaningful messages

## File Placement Rules

**New entity:** → `KiroSpotiBot.Core/Entities/{Name}Entity.cs`

**New repository interface:** → `KiroSpotiBot.Core/Interfaces/I{Name}Repository.cs`

**New repository implementation:** → `KiroSpotiBot.Infrastructure/Repositories/{Name}Repository.cs`

**New Azure Function:** → `KiroSpotiBot.Functions/{FunctionName}.cs`

**New Blazor page:** → `KiroSpotiBot.Web/Pages/{PageName}.razor`

**New test file:** → `KiroSpotiBot.Tests/{ClassUnderTest}Tests.cs`

## Dependency Rules (MUST NOT violate)

- Core MUST NOT reference any other project
- Infrastructure MAY reference Core only
- Functions MAY reference Core and Infrastructure
- Web MAY reference Core and Infrastructure
- Tests MAY reference all projects

## Build Artifacts (gitignored)

- `bin/` - Compiled binaries
- `obj/` - Intermediate build files
- `.vs/` - Visual Studio cache
- `local.settings.json` - Local configuration with secrets
