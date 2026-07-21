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

### RingBufferLogProvider

Stores entries in a bounded, thread-safe in-memory buffer that supports querying. This is the provider to reach for when a tool (an editor console, an MCP server, a debug session capture) needs to browse log history rather than just watch a stream:

```csharp
using KeenEyes.Logging.Providers;

var ringBuffer = new RingBufferLogProvider(capacity: 5000); // default is RingBufferLogProvider.DefaultCapacity (10,000)
logManager.AddProvider(ringBuffer);
```

When the buffer fills up, the oldest entries are evicted to make room for new ones. See [Querying Log History](#querying-log-history) below for how to read entries back out.

## Querying Log History

Providers that store entries in memory can implement `ILogQueryable` to expose retrieval and filtering. `RingBufferLogProvider` implements both `ILogProvider` and `ILogQueryable`, so it can be added to a `LogManager` for live logging and queried independently for history:

```csharp
var ringBuffer = new RingBufferLogProvider();
logManager.AddProvider(ringBuffer);

// ... application runs and logs ...

// Get everything currently stored
IReadOnlyList<LogEntry> all = ringBuffer.GetEntries();

// Filter with a LogQuery
var errors = ringBuffer.Query(new LogQuery
{
    MinLevel = LogLevel.Error,
    CategoryPattern = "ECS.*",
    MessageContains = "failed",
    After = DateTime.Now.AddMinutes(-10),
    MaxResults = 50,
    NewestFirst = true
});
```

`LogQuery` properties are all optional filters — leaving one unset means "no filter" for that dimension:

| Property | Description |
|----------|--------------|
| `MinLevel` / `MaxLevel` | Inclusive level range |
| `CategoryPattern` | Wildcard pattern (`*` and `?`) matched against `LogEntry.Category` |
| `MessageContains` | Case-insensitive substring match against `LogEntry.Message` |
| `After` / `Before` | Inclusive timestamp range |
| `MaxResults` | Maximum entries returned (default 1000) |
| `Skip` | Entries to skip, for pagination with `MaxResults` |
| `NewestFirst` | Reverse-chronological order when true (default); chronological when false |

Each result is a `LogEntry` record (`Timestamp`, `Level`, `Category`, `Message`, `Properties`).

Call `GetStats()` for a summary without pulling every entry:

```csharp
LogStats stats = ringBuffer.GetStats();

Console.WriteLine($"{stats.TotalCount} entries ({stats.ErrorCount} errors, {stats.FatalCount} fatal)");
Console.WriteLine($"Buffer capacity: {stats.Capacity}");
Console.WriteLine($"Oldest: {stats.OldestTimestamp}, Newest: {stats.NewestTimestamp}");

// Per-level counts by LogLevel value
int warnings = stats.GetCountForLevel(LogLevel.Warning);
```

`ILogQueryable` also exposes `EntryCount`, `Clear()`, and the `LogAdded` / `LogsCleared` events, so a console UI or MCP resource can subscribe for live updates instead of polling.

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

## ECS-Specific Logging

`LogManager` and its providers are ECS-agnostic — they know nothing about worlds, entities, or systems. `EcsLoggingPlugin` bridges the gap: it's a `IWorldPlugin` that hooks into world events and turns them into structured log messages via an internal `EcsLogger`.

### Installing the Plugin

```csharp
using KeenEyes.Logging;

var logManager = new LogManager();
logManager.AddProvider(new ConsoleLogProvider());

var world = new World();
world.InstallPlugin(new EcsLoggingPlugin(logManager));
```

Once installed, the plugin automatically logs:
- **System execution** — start/complete timing (via the system hook capability, when available) and enable/disable changes
- **Entity lifecycle** — creation and destruction

Messages are written under the `ECS.System`, `ECS.Entity`, `ECS.Component`, and `ECS.Query` categories.

### Per-Category Verbosity

Each category in `EcsLogCategory` (`System`, `Entity`, `Component`, `Query`) can have its own minimum level, independent of `LogManager.MinimumLevel`. Access the logger through the plugin's `Logger` property:

```csharp
var plugin = new EcsLoggingPlugin(logManager);

plugin.Logger.SetCategoryLevel(EcsLogCategory.System, LogLevel.Debug);
plugin.Logger.SetCategoryLevel(EcsLogCategory.Component, LogLevel.Warning); // quiet down noisy component churn
plugin.Logger.IsEnabled = true; // master on/off switch for all ECS logging

world.InstallPlugin(plugin);
```

`EcsLogger.IsLevelEnabled(category, level)` combines the master switch, the per-category level, and the underlying `LogManager`'s level check, so callers can guard expensive work the same way they would with `LogManager.IsLevelEnabled`.

### Component Logging

Component add/remove/change logging is opt-in per type, since subscribing requires compile-time type information and unconditionally logging every component would be expensive in busy scenes:

```csharp
// After the plugin is installed:
plugin.EnableComponentLogging<Position>();
plugin.EnableComponentLogging<Velocity>();
```

This subscribes to `IWorld.OnComponentAdded<T>`, `OnComponentRemoved<T>`, and `OnComponentChanged<T>` for the given component type and logs each event under `ECS.Component` at `LogLevel.Trace`. Calling `EnableComponentLogging<T>()` before the plugin is installed throws `InvalidOperationException`.

### Query Cache Statistics

`EcsLoggingPlugin.LogQueryStats(cachedQueries, cacheHits, cacheMisses, hitRate)` logs a summary line under `ECS.Query` at `LogLevel.Info`. It doesn't hook into the query system automatically — call it yourself (e.g., from your own system or a periodic diagnostic) with numbers from wherever your query cache tracks them.

### Accessing the Logger via Extension

`EcsLoggingPlugin` registers the `EcsLogger` as a world extension on install, so other plugins or systems can retrieve it without holding a reference to the plugin itself:

```csharp
var ecsLogger = world.GetExtension<EcsLogger>();
ecsLogger.LogEntityParentChanged(childId: 5, parentId: 2);
```

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
