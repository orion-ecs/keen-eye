# Logging

KeenEyes provides a pluggable logging system through the `KeenEyes.Logging` library. The system supports multiple log providers, structured logging with properties, scoped contexts, and level-based filtering.

## Basic Usage

### Setting Up Logging

```csharp
using KeenEyes.Logging;
using KeenEyes.Logging.Providers;

// Create a log manager
var logManager = new LogManager();

// Add providers
logManager.AddProvider(new ConsoleLogProvider());
logManager.AddProvider(new FileLogProvider("logs/app.log"));

// Set minimum log level
logManager.MinimumLevel = LogLevel.Debug;
```

### Logging Messages

```csharp
// Log at different levels
logManager.Trace("MySystem", "Entering method ProcessEntities");
logManager.Debug("MySystem", "Processing 100 entities");
logManager.Info("MySystem", "System initialized successfully");
logManager.Warning("MySystem", "Entity pool nearing capacity");
logManager.Error("MySystem", "Failed to load component data");
logManager.Fatal("MySystem", "Unrecoverable error - shutting down");

// Generic log method
logManager.Log(LogLevel.Info, "MySystem", "Custom message");
```

## Log Levels

Log levels are ordered by severity from lowest to highest:

| Level | Code | Description |
|-------|------|-------------|
| Trace | `TRC` | Fine-grained diagnostic information |
| Debug | `DBG` | Detailed information for troubleshooting |
| Info | `INF` | General progress information |
| Warning | `WRN` | Potential problems or unusual situations |
| Error | `ERR` | Operation failures |
| Fatal | `FTL` | Critical errors causing shutdown |

Messages below the configured minimum level are ignored.

## Structured Logging

Include structured properties with log messages for better analysis:

```csharp
// Using a dictionary for properties
logManager.Info("EntitySystem", "Entity spawned", new Dictionary<string, object?>
{
    ["EntityId"] = 42,
    ["Position"] = "(100, 200)",
    ["ComponentCount"] = 5
});

// Output: [12:34:56.789] INF [EntitySystem] Entity spawned {EntityId=42, Position=(100, 200), ComponentCount=5}
```

## Log Scopes

Scopes add contextual properties to all log messages within a block:

```csharp
using (logManager.BeginScope("EntityProcessing", new Dictionary<string, object?>
{
    ["BatchId"] = Guid.NewGuid(),
    ["StartTime"] = DateTime.UtcNow
}))
{
    logManager.Debug("System", "Processing started");
    // All messages here include BatchId and StartTime

    foreach (var entity in entities)
    {
        using (logManager.BeginScope("Entity", new Dictionary<string, object?>
        {
            ["EntityId"] = entity.Id
        }))
        {
            logManager.Trace("System", "Processing entity");
            // Messages here include BatchId, StartTime, and EntityId
        }
    }

    logManager.Debug("System", "Processing complete");
}
```

Scopes can be nested, and child scope properties take precedence over parent scope properties for duplicate keys.

## Built-in Providers

### ConsoleLogProvider

Writes color-coded log messages to the console:

```csharp
var console = new ConsoleLogProvider
{
    MinimumLevel = LogLevel.Debug,
    UseColors = true,                    // Enable color-coded output
    TimestampFormat = "HH:mm:ss.fff",    // Compact time format
    IncludeProperties = true             // Include structured properties
};

logManager.AddProvider(console);
```

Color coding:
- **Trace**: Gray
- **Debug**: Cyan
- **Info**: White
- **Warning**: Yellow
- **Error**: Red
- **Fatal**: Dark Red

### FileLogProvider

Writes log messages to a file with optional rotation:

```csharp
var file = new FileLogProvider("logs/app.log")
{
    MinimumLevel = LogLevel.Info,
    TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff",
    MaxFileSizeBytes = 10 * 1024 * 1024,  // 10 MB rotation
    IncludeProperties = true
};

logManager.AddProvider(file);
```

Features:
- Automatic directory creation
- Size-based file rotation with timestamps
- Thread-safe file access
- Efficient buffered writes

### DebugLogProvider

Writes to `System.Diagnostics.Debug` output (visible in IDE debugger):

```csharp
var debug = new DebugLogProvider
{
    MinimumLevel = LogLevel.Trace
};

logManager.AddProvider(debug);
```

### NullLogProvider

A no-op provider useful for testing or disabling logging:

```csharp
logManager.AddProvider(new NullLogProvider());
```

## Creating Custom Providers

Implement `ILogProvider` to create custom log destinations:

```csharp
public class CustomLogProvider : ILogProvider
{
    public string Name => "Custom";
    public LogLevel MinimumLevel { get; set; } = LogLevel.Info;

    public void Log(
        LogLevel level,
        string category,
        string message,
        IReadOnlyDictionary<string, object?>? properties)
    {
        if (level < MinimumLevel) return;

        // Write to custom destination (database, network, etc.)
        SendToExternalService(level, category, message, properties);
    }

    public void Flush()
    {
        // Ensure all buffered messages are written
    }

    public void Dispose()
    {
        // Clean up resources
    }
}
```

Requirements:
- Thread-safe `Log` method (may be called from multiple threads)
- Swallow exceptions internally (logging must not crash the application)
- Unique `Name` property for each provider instance

## Provider Management

### Adding Providers

```csharp
logManager.AddProvider(new ConsoleLogProvider());
logManager.AddProvider(new FileLogProvider("logs/app.log"));
```

Each provider must have a unique name. Adding a provider with a duplicate name throws `InvalidOperationException`.

### Removing Providers

```csharp
bool removed = logManager.RemoveProvider("Console");
```

### Getting Providers

```csharp
var fileProvider = logManager.GetProvider("File") as FileLogProvider;
if (fileProvider != null)
{
    fileProvider.MinimumLevel = LogLevel.Warning;
}
```

### Checking Status

```csharp
// Check if any providers are registered
if (logManager.IsEnabled)
{
    logManager.Info("System", "Logging is active");
}

// Get provider count
int count = logManager.ProviderCount;

// Check if a specific level is enabled
if (logManager.IsLevelEnabled(LogLevel.Debug))
{
    // Perform expensive string formatting only if debug is enabled
    logManager.Debug("System", $"Complex data: {ExpensiveToString(data)}");
}
```

## Performance Considerations

### Early Exit Checks

Use level checks to avoid expensive operations when logging is disabled:

```csharp
// Good - avoids string formatting if debug is disabled
if (logManager.IsLevelEnabled(LogLevel.Debug))
{
    logManager.Debug("System", $"Entity {entity.Id} at position {position}");
}

// Also good - providers check internally, but formatting still happens
logManager.Debug("System", $"Entity {entity.Id} at position {position}");
```

### Flushing

Call `Flush()` before shutdown to ensure buffered messages are written:

```csharp
// Before application exit
logManager.Flush();
logManager.Dispose();
```

### Thread Safety

- `LogManager` is thread-safe
- All built-in providers are thread-safe
- Scopes use `AsyncLocal` for proper async context flow

## Integration Example

```csharp
public class GameApplication : IDisposable
{
    private readonly World world;
    private readonly LogManager logManager;

    public GameApplication()
    {
        // Set up logging
        logManager = new LogManager();
        logManager.AddProvider(new ConsoleLogProvider { MinimumLevel = LogLevel.Debug });
        logManager.AddProvider(new FileLogProvider("logs/game.log")
        {
            MinimumLevel = LogLevel.Info,
            MaxFileSizeBytes = 50 * 1024 * 1024
        });

        logManager.Info("Game", "Application starting");

        // Create world
        world = new World();
        logManager.Info("Game", "World created");
    }

    public void Update(float deltaTime)
    {
        using (logManager.BeginScope("Frame", new Dictionary<string, object?>
        {
            ["DeltaTime"] = deltaTime
        }))
        {
            logManager.Trace("Game", "Frame update started");
            world.Update(deltaTime);
            logManager.Trace("Game", "Frame update completed");
        }
    }

    public void Dispose()
    {
        logManager.Info("Game", "Application shutting down");
        world.Dispose();
        logManager.Flush();
        logManager.Dispose();
    }
}
```
